using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;

namespace CivilizationEvolution.Climate
{
    /// <summary>
    /// 行星气候模拟器
    /// 成因层参数驱动，消除"全局温度/全局降水"滑块
    /// 支持季节变化、迎风坡/背风坡地形雨、脏标记增量重算
    /// </summary>
    public class PlanetClimateSimulator
    {
        private readonly WorldConfig _config;
        private readonly TileData[] _tiles;
        private readonly int _width;
        private readonly int _height;
        private readonly SeaLandGenerator _seaLand;

        // 内部派生常量
        private float _obliquity;
        private float _polarCircleLat;
        private float _tropicLat;
        private float _effectiveSolarConstant;

        // 季节缓存：0=春,1=夏,2=秋,3=冬（北半球视角）
        private float[] _seasonTempMod = new float[4];
        private float[] _seasonPrecipMod = new float[4];

        public PlanetClimateSimulator(WorldConfig config, TileData[] tiles, int width, int height, SeaLandGenerator seaLand)
        {
            _config = config;
            _tiles = tiles;
            _width = width;
            _height = height;
            _seaLand = seaLand;
            RecalculateDerivedConstants();
            CalculateSeasonModifiers();
        }

        /// <summary>重算内部派生常量</summary>
        private void RecalculateDerivedConstants()
        {
            _obliquity = Mathf.Lerp(0f, 45f, _config.seasonIntensity);
            _tropicLat = _obliquity;
            _polarCircleLat = 90f - _obliquity;
            _effectiveSolarConstant = _config.stellarIrradiance * (1f - _config.albedo) / 4f;
        }

        /// <summary>计算季节修正因子</summary>
        private void CalculateSeasonModifiers()
        {
            // 季节温差由黄赤交角决定
            float seasonalAmplitude = _obliquity / 23.5f * 15f;
            _seasonTempMod[0] = 0f;           // 春：年均温
            _seasonTempMod[1] = seasonalAmplitude;  // 夏：升温
            _seasonTempMod[2] = 0f;           // 秋：年均温
            _seasonTempMod[3] = -seasonalAmplitude; // 冬：降温

            // 降水季节变化：夏季季风区降水多，冬季地中海气候区降水多
            _seasonPrecipMod[0] = 1.0f;
            _seasonPrecipMod[1] = 1.3f;
            _seasonPrecipMod[2] = 1.0f;
            _seasonPrecipMod[3] = 0.7f;
        }

        /// <summary>全量重算所有地块气候</summary>
        public void RecalculateAll()
        {
            RecalculateDerivedConstants();
            CalculateSeasonModifiers();

            // 先计算盛行风向场（用于迎风坡判断）
            var windField = CalculateWindField();

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand) continue;
                CalculateTileClimate(i, windField, 0); // 默认春季
            }
        }

        /// <summary>脏区局部重算</summary>
        public void RecalculateDirty(HashSet<int> dirtyIndices)
        {
            RecalculateDerivedConstants();
            CalculateSeasonModifiers();
            var windField = CalculateWindField();

            foreach (int idx in dirtyIndices)
            {
                if (!_tiles[idx].exists || !_tiles[idx].isLand) continue;
                CalculateTileClimate(idx, windField, 0);
            }
        }

        /// <summary>按季节更新气候（用于游戏内季节切换）</summary>
        public void UpdateForSeason(int season)
        {
            var windField = CalculateWindField();
            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists || !_tiles[i].isLand) continue;
                CalculateTileClimate(i, windField, season);
            }
        }

        /// <summary>
        /// 计算盛行风向场
        /// 低纬(0-30°)：信风（北半球东北风，南半球东南风）
        /// 中纬(30-60°)：西风（北半球西南风，南半球西北风）
        /// 高纬(60-90°)：极地东风
        /// </summary>
        private Vector2[] CalculateWindField()
        {
            var windField = new Vector2[_tiles.Length];

            for (int i = 0; i < _tiles.Length; i++)
            {
                if (!_tiles[i].exists)
                {
                    windField[i] = Vector2.zero;
                    continue;
                }

                float lat = GetTileLatitude(i);
                float absLat = Mathf.Abs(lat);
                bool isNorthern = lat > 0;

                Vector2 windDir;
                if (absLat < 30f)
                {
                    // 信风带：北半球东北风(→↓)，南半球东南风(→↑)
                    windDir = isNorthern ? new Vector2(0.7f, -0.7f) : new Vector2(0.7f, 0.7f);
                }
                else if (absLat < 60f)
                {
                    // 西风带：北半球西南风(←↑)，南半球西北风(←↓)
                    windDir = isNorthern ? new Vector2(-0.7f, 0.7f) : new Vector2(-0.7f, -0.7f);
                }
                else
                {
                    // 极地东风
                    windDir = isNorthern ? new Vector2(0.8f, -0.6f) : new Vector2(0.8f, 0.6f);
                }

                // 季风区风向反转（夏季）
                if (_config.monsoonStrength > 0.3f && absLat < 35f)
                {
                    float monsoonFactor = _config.monsoonStrength * _tiles[i].waterAdjacentWeight;
                    windDir = Vector2.Lerp(windDir, -windDir, monsoonFactor * 0.5f);
                }

                windField[i] = windDir.normalized;
            }

            return windField;
        }

        /// <summary>
        /// 单地块气候计算
        /// </summary>
        private void CalculateTileClimate(int index, Vector2[] windField, int season)
        {
            ref TileData tile = ref _tiles[index];
            float lat = GetTileLatitude(index);
            float absLat = Mathf.Abs(lat);

            // ===== 1. 温度计算 =====
            float equatorBaseTemp = CalculateEquatorBaseTemp();
            float latDecay = Mathf.Pow(absLat / 90f, 1.5f) * (1.6f - _config.heatTransport * 0.3f);
            float latTemp = equatorBaseTemp * (1f - latDecay);
            float elevationTemp = -tile.elevation01 * 4000f / 1000f * _config.lapseRate;
            float maritimeEffect = tile.waterAdjacentWeight * 3f;
            float thermalOffset = Mathf.Abs(lat - _config.thermalEquatorLat) - absLat;
            float thermalEffect = -thermalOffset * 0.15f;
            float seasonMod = _seasonTempMod[season] * (absLat / 90f); // 高纬季节变化大

            tile.annualTemp = Mathf.Clamp(latTemp + elevationTemp + maritimeEffect + thermalEffect + seasonMod, -55f, 35f);
            tile.diurnalTempRange = (1f - tile.waterAdjacentWeight) * 12f + tile.elevation01 * 4f + 4f;

            // ===== 2. 降水计算 =====
            float itczRain = CalculateITCZPrecipitation(lat);
            float westerlyRain = CalculateWesterlyPrecipitation(lat);
            float subtropicalDry = CalculateSubtropicalDry(lat);
            float monsoonRain = CalculateMonsoonPrecipitation(lat, tile.waterAdjacentWeight, season);
            float orographicRain = CalculateOrographicPrecipitation(index, windField);
            float continentalFactor = 1f - tile.waterAdjacentWeight * 0.5f;
            float seasonPrecipMod = _seasonPrecipMod[season];

            float totalRainMm = (itczRain + westerlyRain + monsoonRain + orographicRain) * continentalFactor * seasonPrecipMod - subtropicalDry;
            tile.annualPrecipMm = Mathf.Clamp(totalRainMm, 0f, 4000f);

            // ===== 3. 湿度计算 =====
            tile.airHumidityPct = Mathf.Clamp(
                Mathf.Lerp(20f, 95f, tile.annualPrecipMm / 2000f) + tile.waterAdjacentWeight * 10f,
                5f, 100f);
            tile.soilHumidityPct = Mathf.Clamp(
                tile.airHumidityPct * 0.7f + (tile.annualPrecipMm / 1000f) * 30f,
                0f, 100f);

            // ===== 4. 积温与无霜期 =====
            tile.accumulatedTemp = Mathf.Max(0, tile.annualTemp - 10f) * 365f;
            tile.frostFreeDays = Mathf.Clamp((tile.annualTemp + 5f) / 30f * 365f, 0f, 365f);

            // ===== 5. 温度带判定 =====
            tile.climateZone = DetermineClimateZone(tile.annualTemp, absLat, tile.elevation01, tile.annualPrecipMm);

            // ===== 6. 群系匹配 =====
            tile.biome = DetermineBiome(tile.climateZone, tile.annualTemp, tile.annualPrecipMm, tile.elevation01, tile.soilHumidityPct);
        }

        private float CalculateEquatorBaseTemp()
        {
            float ratio = _effectiveSolarConstant / (1361f * 0.7f / 4f);
            return 27f * ratio + _config.greenhouseFactor * 0.3f;
        }

        /// <summary>赤道辐合带降水：环流模式决定降水带数量</summary>
        private float CalculateITCZPrecipitation(float lat)
        {
            float rain = 0f;
            int cellCount = _config.circulationMode switch
            {
                GameEnums.CirculationMode.SingleCell => 1,
                GameEnums.CirculationMode.DoubleCell => 2,
                GameEnums.CirculationMode.TripleCell => 3,
                _ => 3
            };

            for (int i = 0; i < cellCount; i++)
            {
                float bandLat = _config.thermalEquatorLat + (i - cellCount / 2f) * 15f;
                float dist = Mathf.Abs(lat - bandLat);
                rain += Mathf.Exp(-dist * dist / 100f) * 1500f;
            }
            return rain;
        }

        private float CalculateWesterlyPrecipitation(float lat)
        {
            float absLat = Mathf.Abs(lat);
            if (absLat > 35f && absLat < 65f)
            {
                float peak = 50f - Mathf.Abs(absLat - 50f);
                return Mathf.Max(0, peak) * 15f;
            }
            return 0f;
        }

        private float CalculateSubtropicalDry(float lat)
        {
            float absLat = Mathf.Abs(lat);
            if (absLat > 18f && absLat < 38f)
            {
                float peak = 10f - Mathf.Abs(absLat - 28f);
                return Mathf.Max(0, peak) * 80f;
            }
            return 0f;
        }

        private float CalculateMonsoonPrecipitation(float lat, float waterWeight, int season)
        {
            float absLat = Mathf.Abs(lat);
            if (absLat < 35f && waterWeight > 0.2f)
            {
                // 夏季季风降水强，冬季弱
                float seasonFactor = season == 1 ? 1.5f : (season == 3 ? 0.3f : 1f);
                return _config.monsoonStrength * waterWeight * _config.seasonIntensity * 800f * seasonFactor;
            }
            return 0f;
        }

        /// <summary>
        /// 地形雨：迎风坡降水增加，背风坡雨影效应
        /// 沿盛行风向追踪上游地块，计算高程抬升导致的降水增加
        /// </summary>
        private float CalculateOrographicPrecipitation(int index, Vector2[] windField)
        {
            ref TileData tile = ref _tiles[index];
            if (tile.elevation01 <= 0.05f) return 0f;

            Vector2 wind = windField[index];
            int x = index % _width;
            int y = index / _width;

            // 沿盛行风向回溯3格，计算上游高程
            float upstreamElevation = 0f;
            int upstreamCount = 0;
            for (int step = 1; step <= 3; step++)
            {
                int ux = x - Mathf.RoundToInt(wind.x * step);
                int uy = y - Mathf.RoundToInt(wind.y * step);
                if (ux >= 0 && ux < _width && uy >= 0 && uy < _height)
                {
                    int uidx = uy * _width + ux;
                    upstreamElevation += _tiles[uidx].elevation01;
                    upstreamCount++;
                }
            }

            if (upstreamCount == 0) return tile.elevation01 * 200f;

            upstreamElevation /= upstreamCount;
            float elevationGain = tile.elevation01 - upstreamElevation;

            if (elevationGain > 0f)
            {
                // 迎风坡：高程抬升 → 降水增加
                return elevationGain * 800f + tile.elevation01 * 100f;
            }
            else
            {
                // 背风坡：雨影效应 → 降水减少（返回负值，在总降水中扣除）
                return elevationGain * 400f;
            }
        }

        /// <summary>九大温度带判定</summary>
        private GameEnums.ClimateZone DetermineClimateZone(float temp, float absLat, float elevation, float precip)
        {
            if (elevation > 0.6f && temp < 5f)
                return GameEnums.ClimateZone.HighlandAlpine;

            if (absLat > 30f && absLat < 55f && precip < 300f)
                return GameEnums.ClimateZone.InlandAridTemperate;

            if (temp < -10f) return GameEnums.ClimateZone.PolarFrigid;
            if (temp < 0f) return GameEnums.ClimateZone.Subarctic;
            if (temp < 8f) return GameEnums.ClimateZone.TemperateCold;
            if (temp < 15f) return GameEnums.ClimateZone.TemperateMild;
            if (temp < 20f) return GameEnums.ClimateZone.TemperateWarm;
            if (temp < 24f) return GameEnums.ClimateZone.Subtropical;
            return GameEnums.ClimateZone.Tropical;
        }

        /// <summary>群系匹配</summary>
        private GameEnums.BiomeType DetermineBiome(GameEnums.ClimateZone zone, float temp, float precip, float elevation, float soilHumidity)
        {
            if (elevation > 0.7f) return GameEnums.BiomeType.Alpine;
            if (temp < -15f) return GameEnums.BiomeType.IceSheet;
            if (soilHumidity > 85f && elevation < 0.3f) return GameEnums.BiomeType.Wetland;

            return zone switch
            {
                GameEnums.ClimateZone.PolarFrigid => GameEnums.BiomeType.IceSheet,
                GameEnums.ClimateZone.Subarctic => precip > 300 ? GameEnums.BiomeType.Tundra : GameEnums.BiomeType.Tundra,
                GameEnums.ClimateZone.TemperateCold => precip > 500 ? GameEnums.BiomeType.BorealForest : GameEnums.BiomeType.Steppe,
                GameEnums.ClimateZone.TemperateMild => precip > 600 ? GameEnums.BiomeType.TemperateForest : GameEnums.BiomeType.TemperateGrassland,
                GameEnums.ClimateZone.TemperateWarm => precip > 700 ? GameEnums.BiomeType.TemperateForest : GameEnums.BiomeType.TemperateGrassland,
                GameEnums.ClimateZone.Subtropical => precip > 800 ? GameEnums.BiomeType.TropicalMonsoon : (precip > 400 ? GameEnums.BiomeType.Savanna : GameEnums.BiomeType.Desert),
                GameEnums.ClimateZone.Tropical => precip > 2000 ? GameEnums.BiomeType.TropicalRainforest : (precip > 800 ? GameEnums.BiomeType.TropicalMonsoon : (precip > 400 ? GameEnums.BiomeType.Savanna : GameEnums.BiomeType.Desert)),
                GameEnums.ClimateZone.HighlandAlpine => GameEnums.BiomeType.Alpine,
                GameEnums.ClimateZone.InlandAridTemperate => precip < 200 ? GameEnums.BiomeType.Desert : GameEnums.BiomeType.Steppe,
                _ => GameEnums.BiomeType.TemperateGrassland
            };
        }

        private float GetTileLatitude(int index)
        {
            int y = index / _width;
            float normalizedY = (float)y / (_height - 1);
            return Mathf.Lerp(_config.planetMaxLat, -_config.planetMaxLat, normalizedY);
        }
    }
}
