using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Character;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.Building;
using CivilizationEvolution.Tech;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Core
{
 /// GameWorld.Terrain —— 地形生成与气候水文计算（含脏标记重算）（partial class，与 GameWorld.cs 共享字段与子系统）
    public partial class GameWorld
    {

 /// <summary>生成随机地形（球形行星生成器：3D球面Simplex噪声+域扭曲+温度降水模拟+55群系）</summary>
        public void GenerateTerrain(int seed = 42)
        {
            randomSeed = seed;

 // 球形行星地形生成器：3D球面Simplex噪声+域扭曲+山脊叠加+温度降水+55群系+肥力
            var planetGen = new PlanetTerrainGenerator(seed);
            planetGen.Generate(tiles, mapWidth, mapHeight);

 // 标记所有地块为脏，触发渲染刷新
            for (int i = 0; i < tiles.Length; i++)
                _terrainDirtyTiles.Add(i);

 // 河流追踪（须在 isLand 判定完成后，复用旧TerrainGenerator的河流算法）
            var terrainGen = new TerrainGenerator(seed);
            terrainGen.TrackRivers(tiles);

 // 地形生成后再初始化政权（修复：地形生成前isLand全为false）
            InitializeDefaultRealms();

 // 省份生成（沃罗诺伊+Lloyd 松弛——地图结构层）
            GenerateProvinces(seed);
            GenerateBurgs(seed);

            Debug.Log($"[GameWorld] 球形地形生成完成，陆地{GetLandTileCount()}地块，海洋{GetSeaTileCount()}地块，省份{provinces.Count}个");
        }


        
 /// <summary>使用GenConfig生成地形（编辑器面板调用）</summary>
        public void GenerateTerrainWithConfig()
        {
            if (GenConfig == null) GenConfig = new MapGenerationConfig();

            int seed = GenConfig.GetActualSeed();
            randomSeed = seed;

 // 应用地图尺寸
            var (w, h) = GenConfig.GetMapDimensions();
            if (w != mapWidth || h != mapHeight)
            {
                mapWidth = w; mapHeight = h;
                tiles = new TileData[mapWidth * mapHeight];
                for (int i = 0; i < tiles.Length; i++)
                {
                    tiles[i] = new TileData { tileIndex = i, provinceId = -1, ownerRealmId = -1, occupyingRealmId = -1, exists = false, elevation01 = 0f, fertility = 0.5f, development = 0.1f, stability = 50f, order = 50f, populationBlocks = new List<PopulationBlock>(), buildingLevels = new int[6] };
                }
                InitializeSubsystems();
            }

 // 创建并配置地形生成器
            _planetTerrainGenerator = new PlanetTerrainGenerator(seed);

 // 先应用地形模板预设（基础参数），再应用 GenConfig 滑块（玩家微调）
            var templatePreset = GenConfig.TerrainScale == TerrainScale.World
                ? TerrainTemplateSystem.GetWorldPreset(GenConfig.WorldTemplate)
                : TerrainTemplateSystem.GetRegionalPreset(GenConfig.RegionalTemplate);
            TerrainTemplateSystem.ApplyPreset(templatePreset, _planetTerrainGenerator);

            GenConfig.ApplyToGenerator(_planetTerrainGenerator);
            _planetTerrainGenerator.Generate(tiles, mapWidth, mapHeight);

 // 河流追踪
            var terrainGen = new TerrainGenerator(seed);
            terrainGen.TrackRivers(tiles);

 // 标记脏
            for (int i = 0; i < tiles.Length; i++) _terrainDirtyTiles.Add(i);

 // 生成省份和Burg
            GenerateProvinces(seed);
            GenerateBurgs(seed);
            InitializeDefaultRealms();

            Debug.Log($"[GameWorld] 配置化地形生成完成：陆地{GetLandTileCount()}，海洋{GetSeaTileCount()}，省份{provinces.Count}");
        }


 /// <summary>计算气候（大气环流GCM：温度/降水/风/气压）</summary>
        public void CalculateClimate()
        {
            if (_atmosphericCirculation == null)
                _atmosphericCirculation = new AtmosphericCirculation(mapWidth, mapHeight);

 // 从tiles提取高程、海陆、温度数组
            float[] elevation = new float[tiles.Length];
            bool[] isLand = new bool[tiles.Length];
            float[] temperature = new float[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                elevation[i] = tiles[i].elevation01;
                isLand[i] = tiles[i].isLand;
                temperature[i] = tiles[i].annualTemp;
            }

 // 应用GenConfig气候参数
            GenConfig.ApplyToGCM(_atmosphericCirculation);

 // 运行大气环流模拟
            _atmosphericCirculation.Run(elevation, isLand, temperature);

 // 写回tiles
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i].annualPrecipMm = _atmosphericCirculation.Precipitation[i];
                tiles[i].airHumidityPct = Mathf.Clamp(_atmosphericCirculation.SpecificHumidity[i] * 10000f, 0f, 100f);
                _climateDirtyTiles.Add(i);
            }

 // 用Holdridge分类器更新生物群系（静态类，直接调用）
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].isLand)
                {
                    float latAbs = Mathf.Abs((float)(TileGrid.ToY(i, mapWidth) - mapHeight * 0.5) / mapHeight * 180f);
                    tiles[i].biome = HoldridgeBiomeClassifier.Classify(
                        tiles[i].annualTemp, tiles[i].annualPrecipMm, tiles[i].elevation01,
                        tiles[i].isLand, tiles[i].isCoast, tiles[i].isRiver,
                        tiles[i].slopeDegree, latAbs);
                }
            }

            Debug.Log($"[GameWorld] 气候计算完成：GCM已运行，降水/湿度/生物群系已更新");
        }


 /// <summary>重算水文（水力侵蚀+河网）</summary>
        public void RecalculateHydrology()
        {
            if (_hydraulicErosion == null)
                _hydraulicErosion = new HydraulicErosion(mapWidth, mapHeight, randomSeed);

 // 从tiles提取高程和海陆
            float[] elevation = new float[tiles.Length];
            bool[] isLand = new bool[tiles.Length];
            for (int i = 0; i < tiles.Length; i++)
            {
                elevation[i] = tiles[i].elevation01;
                isLand[i] = tiles[i].isLand;
            }

 // 应用GenConfig水文参数
            GenConfig.ApplyToErosion(_hydraulicErosion, tiles.Length);
            _hydraulicErosion.WrapX = config.wrapX;

 // 运行水力侵蚀
            _hydraulicErosion.Run(elevation, isLand);
            _hydraulicErosion.ApplyToTiles(tiles, elevation);

 // 重新追踪河流
            var terrainGen = new TerrainGenerator(randomSeed);
            terrainGen.TrackRivers(tiles);

 // 标记脏
            for (int i = 0; i < tiles.Length; i++) _terrainDirtyTiles.Add(i);

            Debug.Log($"[GameWorld] 水文重算完成：水力侵蚀+河网追踪已执行");
        }


 /// <summary>全部生成（地形+气候+水文，编辑器一键生成）</summary>
        public void GenerateAll()
        {
            GenerateTerrainWithConfig();
            CalculateClimate();
            RecalculateHydrology();
            Debug.Log("[GameWorld] 全部生成完成");
        }

        private void GenerateProvinces(int seed)
        {
 // 省份密度随地图尺寸动态调整：大地图每省地块更多，避免省份数量爆炸
 // 公式：Max(48, sqrt(总地块) * 0.3)
 // 128×64(8192)→48地块/省≈170省；512×256(131072)→109≈1200省；1920×1080(2073600)→432≈4800省（对齐参考项目5226省）
            int totalTiles = mapWidth * mapHeight;
            int cellsPerProvince = Mathf.Max(48, (int)(Mathf.Sqrt(totalTiles) * 0.3));

            var generator = new ProvinceGenerator(tiles, mapWidth, mapHeight, config.wrapX);
            provinces = generator.Generate(seed + 777, cellsPerProvince,
                ProvinceGenerator.DefaultLloydIterations);
            Debug.Log($"[GameWorld] 省份生成：{cellsPerProvince}地块/省 → {provinces.Count}省");
        }



 /// <summary>生成子地块/Burg（省份内定居点：城市/港口/要塞/村庄）</summary>
        private void GenerateBurgs(int seed)
        {
            var generator = new BurgGenerator(tiles, mapWidth, mapHeight, provinces, seed);
            burgs = generator.Generate();
            int cityCount=0, portCount=0, fortCount=0, villageCount=0;
            foreach (var b in burgs.Values) {
                switch (b.type) { case BurgType.City: cityCount++; break; case BurgType.Port: portCount++; break; case BurgType.Fortress: fortCount++; break; default: villageCount++; break; }
            }
            Debug.Log($"[GameWorld] 子地块生成：{burgs.Count}个（城{cityCount}/港{portCount}/寨{fortCount}/村{villageCount}）");
        }

 /// <summary>计算地块基础肥力</summary>
        private float CalculateBaseFertility(int index)
        {
            ref TileData tile = ref tiles[index];
            float climateScore = Mathf.Clamp(tile.annualPrecipMm / 1000f, 0f, 1f) * 0.5f
                + Mathf.Clamp((tile.annualTemp + 10f) / 35f, 0f, 1f) * 0.3f;
            float terrainScore = (1f - tile.elevation01) * 0.2f;
            float soilScore = tile.soilHumidityPct / 100f * 0.2f;
            return Mathf.Clamp(climateScore + terrainScore + soilScore, 0.05f, 1f);
        }


 /// <summary>全量重算</summary>
        public void RecalculateAll()
        {
            _seaLandGenerator.RecalculateAll();
            _climateSimulator.RecalculateAll();
            _terrainDirtyTiles.Clear();
            _climateDirtyTiles.Clear();
            _configDirty = false;
        }


 /// <summary>脏区增量重算</summary>
        public void RecalculateDirty()
        {
            if (_configDirty)
            {
                RecalculateAll();
                return;
            }

            if (_terrainDirtyTiles.Count > 0)
            {
                _seaLandGenerator.RecalculateDirty(_terrainDirtyTiles);
                foreach (int idx in _terrainDirtyTiles)
                    _climateDirtyTiles.Add(idx);
                _terrainDirtyTiles.Clear();
            }

            if (_climateDirtyTiles.Count > 0)
            {
                _climateSimulator.RecalculateDirty(_climateDirtyTiles);
                _climateDirtyTiles.Clear();
            }
        }


 /// <summary>画笔修改地形</summary>
        public void PaintTerrain(int tileIndex, float newElevation)
        {
            if (tileIndex < 0 || tileIndex >= tiles.Length) return;
            tiles[tileIndex].elevation01 = newElevation;
            _terrainDirtyTiles.Add(tileIndex);
            MarkNeighboursDirty(tileIndex);
        }


 /// <summary>修改世界配置参数</summary>
        public void UpdateConfig(System.Action<WorldConfig> configUpdater)
        {
            configUpdater?.Invoke(config);
            _configDirty = true;
        }


        private void MarkNeighboursDirty(int centerIndex)
        {
            foreach (int n in _seaLandGenerator.GetNeighbourIndices(centerIndex))
                _terrainDirtyTiles.Add(n);
        }

    }
}
