using System;
using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.World.Terrain
{
 /// 地形模板两大分类：完整世界 vs 局部。
 /// 两者的生成逻辑和参数体系不同：
 /// - 完整世界：面向全球海陆格局，核心参数是大陆数量、海陆比例、海洋连通性
 /// - 局部：面向区域地理特征，核心参数是边界条件、中心特征
    public enum TerrainScale
    {
 /// <summary>完整世界：全球海陆格局（大陆/海洋/群岛整体分布）</summary>
        World,
 /// <summary>局部：区域地理特征（流域/半岛/盆地/山脉等，含边界条件）</summary>
        Regional
    }

 /// <summary>完整世界地形模板</summary>
    public enum WorldTemplate
    {
 /// <summary>盘古大陆：一块超级大陆，周围环绕海洋</summary>
        [Tooltip("一块超级大陆，周围环绕海洋。陆地集中，海洋连通。")]
        Pangaea,
 /// <summary>双大陆：两块大陆隔海相望</summary>
        [Tooltip("两块大陆隔海相望，中间为海洋通道。")]
        DualContinents,
 /// <summary>多大陆：3-5块大陆散布全球</summary>
        [Tooltip("3-5块大陆散布，海洋分割各大陆。")]
        MultiContinents,
 /// <summary>群岛世界：大量岛屿，无大块陆地</summary>
        [Tooltip("无大块陆地，大量岛屿散布，陆地比例低。")]
        ArchipelagoWorld,
 /// <summary>环形大陆：大陆环绕中央内海</summary>
        [Tooltip("大陆环绕中央内海，类似地中海但规模更大。")]
        RingContinent,
 /// <summary>地中海世界：中央海+周围多块陆地</summary>
        [Tooltip("中央海为核心，周围多块陆地环绕，海陆交错。")]
        Mediterranean,
 /// <summary>类地球：类似地球的海陆分布</summary>
        [Tooltip("类似地球的海陆分布：几块大陆+大片海洋。")]
        EarthLike,
 /// <summary>自定义：纯参数，无模板预设</summary>
        [Tooltip("不使用模板预设，完全由参数滑块控制。")]
        Custom
    }

 /// <summary>局部地形模板</summary>
    public enum RegionalTemplate
    {
 /// <summary>河流流域：一条大河+流域平原</summary>
        [Tooltip("一条大河贯穿，周围为流域平原，两侧缓升。")]
        RiverValley,
 /// <summary>半岛：伸入海洋的半岛</summary>
        [Tooltip("陆地从一侧伸入海洋，三面环海。")]
        Peninsula,
 /// <summary>群岛：一片群岛散布</summary>
        [Tooltip("一片群岛散布在海洋中，无大块陆地。")]
        Archipelago,
 /// <summary>内陆海：中央海+周围陆地</summary>
        [Tooltip("中央为海，周围被陆地环绕，仅一侧可能通外海。")]
        InlandSea,
 /// <summary>山脉屏障：一条大山脉+两侧低地</summary>
        [Tooltip("一条大山脉横贯，两侧为低地/平原。")]
        MountainRange,
 /// <summary>盆地：四周高中央低</summary>
        [Tooltip("四周高山环绕，中央为低地/平原，可能有内流湖。")]
        Basin,
 /// <summary>沿海平原：一侧海一侧缓升陆地</summary>
        [Tooltip("一侧为海洋，陆地向另一侧缓慢升高，海岸线平直。")]
        CoastalPlain,
 /// <summary>峡湾：冰川侵蚀的锯齿海岸</summary>
        [Tooltip("冰川侵蚀形成的锯齿状海岸，多峡湾和岛屿。")]
        Fjord,
 /// <summary>自定义：纯参数，无模板预设</summary>
        [Tooltip("不使用模板预设，完全由参数滑块控制。")]
        Custom
    }

 /// 地形模板参数预设。
 /// 每个模板对应一组 PlanetTerrainGenerator 参数，
 /// 应用后玩家仍可用滑块微调。
    [Serializable]
    public class TerrainTemplatePreset
    {
        public string Name;
        public string Description;
        public TerrainScale Scale;

 // PlanetTerrainGenerator 参数
        public float TargetLandFraction = 0.30f;
        public float TerrainFrequency = 1.8f;
        public int TerrainOctaves = 6;
        public float WarpStrength = 0.7f;
        public float WarpFrequency = 1.3f;
        public float MountainHeight = 0.35f;
        public int PlateCount = 12;
        public float PlateBoundaryMountainBoost = 0.25f;

 // 大陆数量与大小范围（完整世界模板用；局部模板可忽略） // 模板是"以N块主大陆为主"，并非绝对只有N块——还会局部生成零散岛屿/半岛
        [Tooltip("主大陆最小数量")]
        public int MinContinentCount = 1;
        [Tooltip("主大陆最大数量")]
        public int MaxContinentCount = 1;
        [Tooltip("单块主大陆最小占比（占总陆地面积比例，0~1）")]
        public float MinContinentSize = 0.15f;
        [Tooltip("单块主大陆最大占比（占总陆地面积比例，0~1）")]
        public float MaxContinentSize = 0.60f;
        [Tooltip("零散岛屿/半岛密度（0=无，1=大量）。模板以主大陆为主，辅以零散小东西。")]
        public float IslandDensity = 0.3f;

 // 特殊形状标记（用于生成逻辑中应用特殊算法）
        public bool HasSpecialShape = false;
        public string SpecialShapeType = "";
    }

 /// 地形模板系统：管理完整世界/局部两大分类的模板定义和参数预设。
 /// 参考 Azgaar FMG 的模板系统，但按规模分类，避免功能混乱。
    public static class TerrainTemplateSystem
    {
 /// <summary>完整世界模板预设表</summary>
        private static readonly Dictionary<WorldTemplate, TerrainTemplatePreset> _worldPresets = new()
        {
            {
                WorldTemplate.Pangaea, new TerrainTemplatePreset {
                    Name = "盘古大陆", Description = "以一块超级主大陆为主，周围环绕海洋，辅以零散岛屿和半岛",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.35f, TerrainFrequency = 1.2f, TerrainOctaves = 5,
                    WarpStrength = 0.5f, WarpFrequency = 1.0f, MountainHeight = 0.40f,
                    PlateCount = 8, PlateBoundaryMountainBoost = 0.30f,
                    MinContinentCount = 1, MaxContinentCount = 1,
                    MinContinentSize = 0.50f, MaxContinentSize = 0.80f,
                    IslandDensity = 0.20f,
                    HasSpecialShape = true, SpecialShapeType = "SingleContinent"
                }
            },
            {
                WorldTemplate.DualContinents, new TerrainTemplatePreset {
                    Name = "双大陆", Description = "以两块主大陆为主隔海相望（大小随机，各有范围），辅以零散岛屿",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.32f, TerrainFrequency = 1.5f, TerrainOctaves = 6,
                    WarpStrength = 0.6f, WarpFrequency = 1.2f, MountainHeight = 0.38f,
                    PlateCount = 10, PlateBoundaryMountainBoost = 0.28f,
                    MinContinentCount = 2, MaxContinentCount = 2,
                    MinContinentSize = 0.20f, MaxContinentSize = 0.45f,
                    IslandDensity = 0.30f,
                    HasSpecialShape = true, SpecialShapeType = "DualContinent"
                }
            },
            {
                WorldTemplate.MultiContinents, new TerrainTemplatePreset {
                    Name = "多大陆", Description = "以3-5块主大陆为主散布全球（大小随机，各有范围），辅以零散岛屿",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.30f, TerrainFrequency = 2.0f, TerrainOctaves = 6,
                    WarpStrength = 0.8f, WarpFrequency = 1.5f, MountainHeight = 0.35f,
                    PlateCount = 14, PlateBoundaryMountainBoost = 0.25f,
                    MinContinentCount = 3, MaxContinentCount = 5,
                    MinContinentSize = 0.10f, MaxContinentSize = 0.35f,
                    IslandDensity = 0.40f,
                    HasSpecialShape = true, SpecialShapeType = "MultiContinent"
                }
            },
            {
                WorldTemplate.ArchipelagoWorld, new TerrainTemplatePreset {
                    Name = "群岛世界", Description = "以大量岛屿为主，无大块主陆地，岛屿大小随机",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.15f, TerrainFrequency = 3.5f, TerrainOctaves = 7,
                    WarpStrength = 1.0f, WarpFrequency = 2.0f, MountainHeight = 0.20f,
                    PlateCount = 20, PlateBoundaryMountainBoost = 0.15f,
                    MinContinentCount = 0, MaxContinentCount = 0,
                    MinContinentSize = 0f, MaxContinentSize = 0.10f,
                    IslandDensity = 0.90f,
                    HasSpecialShape = false
                }
            },
            {
                WorldTemplate.RingContinent, new TerrainTemplatePreset {
                    Name = "环形大陆", Description = "以环形主大陆为主环绕中央内海，辅以零散岛屿和半岛",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.40f, TerrainFrequency = 1.0f, TerrainOctaves = 5,
                    WarpStrength = 0.4f, WarpFrequency = 0.8f, MountainHeight = 0.45f,
                    PlateCount = 6, PlateBoundaryMountainBoost = 0.35f,
                    MinContinentCount = 1, MaxContinentCount = 1,
                    MinContinentSize = 0.40f, MaxContinentSize = 0.60f,
                    IslandDensity = 0.25f,
                    HasSpecialShape = true, SpecialShapeType = "RingContinent"
                }
            },
            {
                WorldTemplate.Mediterranean, new TerrainTemplatePreset {
                    Name = "地中海世界", Description = "以中央海为核心，周围多块主陆地环绕（大小随机），辅以零散岛屿",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.38f, TerrainFrequency = 1.8f, TerrainOctaves = 6,
                    WarpStrength = 0.7f, WarpFrequency = 1.3f, MountainHeight = 0.38f,
                    PlateCount = 12, PlateBoundaryMountainBoost = 0.28f,
                    MinContinentCount = 3, MaxContinentCount = 6,
                    MinContinentSize = 0.08f, MaxContinentSize = 0.30f,
                    IslandDensity = 0.45f,
                    HasSpecialShape = true, SpecialShapeType = "Mediterranean"
                }
            },
            {
                WorldTemplate.EarthLike, new TerrainTemplatePreset {
                    Name = "类地球", Description = "以几块主大陆为主类似地球海陆分布（大小随机），辅以零散岛屿",
                    Scale = TerrainScale.World,
                    TargetLandFraction = 0.29f, TerrainFrequency = 1.8f, TerrainOctaves = 6,
                    WarpStrength = 0.7f, WarpFrequency = 1.3f, MountainHeight = 0.35f,
                    PlateCount = 12, PlateBoundaryMountainBoost = 0.25f,
                    MinContinentCount = 4, MaxContinentCount = 7,
                    MinContinentSize = 0.05f, MaxContinentSize = 0.30f,
                    IslandDensity = 0.35f,
                    HasSpecialShape = false
                }
            },
            {
                WorldTemplate.Custom, new TerrainTemplatePreset {
                    Name = "自定义", Description = "纯参数，无模板预设",
                    Scale = TerrainScale.World,
                    HasSpecialShape = false
                }
            }
        };

 /// <summary>局部模板预设表</summary>
        private static readonly Dictionary<RegionalTemplate, TerrainTemplatePreset> _regionalPresets = new()
        {
            {
                RegionalTemplate.RiverValley, new TerrainTemplatePreset {
                    Name = "河流流域", Description = "一条大河+流域平原",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.70f, TerrainFrequency = 1.5f, TerrainOctaves = 5,
                    WarpStrength = 0.5f, WarpFrequency = 1.0f, MountainHeight = 0.25f,
                    PlateCount = 6, PlateBoundaryMountainBoost = 0.20f,
                    HasSpecialShape = true, SpecialShapeType = "RiverValley"
                }
            },
            {
                RegionalTemplate.Peninsula, new TerrainTemplatePreset {
                    Name = "半岛", Description = "伸入海洋的半岛",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.55f, TerrainFrequency = 1.8f, TerrainOctaves = 5,
                    WarpStrength = 0.6f, WarpFrequency = 1.2f, MountainHeight = 0.30f,
                    PlateCount = 8, PlateBoundaryMountainBoost = 0.22f,
                    HasSpecialShape = true, SpecialShapeType = "Peninsula"
                }
            },
            {
                RegionalTemplate.Archipelago, new TerrainTemplatePreset {
                    Name = "群岛", Description = "一片群岛散布",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.25f, TerrainFrequency = 3.0f, TerrainOctaves = 7,
                    WarpStrength = 0.9f, WarpFrequency = 1.8f, MountainHeight = 0.22f,
                    PlateCount = 16, PlateBoundaryMountainBoost = 0.18f,
                    HasSpecialShape = true, SpecialShapeType = "Archipelago"
                }
            },
            {
                RegionalTemplate.InlandSea, new TerrainTemplatePreset {
                    Name = "内陆海", Description = "中央海+周围陆地",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.60f, TerrainFrequency = 1.2f, TerrainOctaves = 5,
                    WarpStrength = 0.4f, WarpFrequency = 0.9f, MountainHeight = 0.35f,
                    PlateCount = 8, PlateBoundaryMountainBoost = 0.25f,
                    HasSpecialShape = true, SpecialShapeType = "InlandSea"
                }
            },
            {
                RegionalTemplate.MountainRange, new TerrainTemplatePreset {
                    Name = "山脉屏障", Description = "一条大山脉+两侧低地",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.75f, TerrainFrequency = 1.0f, TerrainOctaves = 5,
                    WarpStrength = 0.3f, WarpFrequency = 0.7f, MountainHeight = 0.55f,
                    PlateCount = 4, PlateBoundaryMountainBoost = 0.40f,
                    HasSpecialShape = true, SpecialShapeType = "MountainRange"
                }
            },
            {
                RegionalTemplate.Basin, new TerrainTemplatePreset {
                    Name = "盆地", Description = "四周高中央低",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.80f, TerrainFrequency = 1.3f, TerrainOctaves = 5,
                    WarpStrength = 0.4f, WarpFrequency = 0.9f, MountainHeight = 0.45f,
                    PlateCount = 6, PlateBoundaryMountainBoost = 0.30f,
                    HasSpecialShape = true, SpecialShapeType = "Basin"
                }
            },
            {
                RegionalTemplate.CoastalPlain, new TerrainTemplatePreset {
                    Name = "沿海平原", Description = "一侧海一侧缓升陆地",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.65f, TerrainFrequency = 1.0f, TerrainOctaves = 4,
                    WarpStrength = 0.3f, WarpFrequency = 0.6f, MountainHeight = 0.20f,
                    PlateCount = 4, PlateBoundaryMountainBoost = 0.15f,
                    HasSpecialShape = true, SpecialShapeType = "CoastalPlain"
                }
            },
            {
                RegionalTemplate.Fjord, new TerrainTemplatePreset {
                    Name = "峡湾", Description = "冰川侵蚀的锯齿海岸",
                    Scale = TerrainScale.Regional,
                    TargetLandFraction = 0.50f, TerrainFrequency = 2.5f, TerrainOctaves = 7,
                    WarpStrength = 1.0f, WarpFrequency = 2.0f, MountainHeight = 0.40f,
                    PlateCount = 10, PlateBoundaryMountainBoost = 0.30f,
                    HasSpecialShape = true, SpecialShapeType = "Fjord"
                }
            },
            {
                RegionalTemplate.Custom, new TerrainTemplatePreset {
                    Name = "自定义", Description = "纯参数，无模板预设",
                    Scale = TerrainScale.Regional,
                    HasSpecialShape = false
                }
            }
        };

 /// <summary>获取完整世界模板预设</summary>
        public static TerrainTemplatePreset GetWorldPreset(WorldTemplate template) =>
            _worldPresets.TryGetValue(template, out var preset) ? preset : _worldPresets[WorldTemplate.Custom];

 /// <summary>获取局部模板预设</summary>
        public static TerrainTemplatePreset GetRegionalPreset(RegionalTemplate template) =>
            _regionalPresets.TryGetValue(template, out var preset) ? preset : _regionalPresets[RegionalTemplate.Custom];

 /// <summary>获取所有完整世界模板名称（用于UI下拉）</summary>
        public static string[] GetWorldTemplateNames()
        {
            var names = new string[_worldPresets.Count];
            int i = 0;
            foreach (var kv in _worldPresets) names[i++] = kv.Value.Name;
            return names;
        }

 /// <summary>获取所有局部模板名称（用于UI下拉）</summary>
        public static string[] GetRegionalTemplateNames()
        {
            var names = new string[_regionalPresets.Count];
            int i = 0;
            foreach (var kv in _regionalPresets) names[i++] = kv.Value.Name;
            return names;
        }

 /// 应用模板预设到 PlanetTerrainGenerator。
 /// Custom 模板不应用（保留当前参数）。
        public static void ApplyPreset(TerrainTemplatePreset preset, PlanetTerrainGenerator generator)
        {
            if (preset == null || generator == null) return;
            if (preset.Name == "自定义") return; // Custom 不覆盖参数

            generator.TargetLandFraction = preset.TargetLandFraction;
            generator.TerrainFrequency = preset.TerrainFrequency;
            generator.TerrainOctaves = preset.TerrainOctaves;
            generator.WarpStrength = preset.WarpStrength;
            generator.WarpFrequency = preset.WarpFrequency;
            generator.MountainHeight = preset.MountainHeight;
            generator.PlateCount = preset.PlateCount;
            generator.PlateBoundaryMountainBoost = preset.PlateBoundaryMountainBoost;
        }
    }
}
