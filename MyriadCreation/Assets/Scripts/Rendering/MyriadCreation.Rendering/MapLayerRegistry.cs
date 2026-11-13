using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyriadCreation.Rendering
{
    /// <summary>地图图层定义：UI/渲染共用的注册项。</summary>
    public sealed class MapLayerDefinition
    {
        public string id = "";
        public string label = "";
        public string group = "";
        public MapDisplayMode mode;
        public int order;
        public bool isOverlay;
        public Func<int, Color> colorProvider; // 可选：按 cell 上色；null=沿用现有渲染器
    }

    /// <summary>
    /// 地图图层注册表：替代 UI 里硬编码的模式列表。
    /// 新增图层只注册一次，Renderer/UI 都从这里枚举。
    /// </summary>
    public static class MapLayerRegistry
    {
        private static readonly Dictionary<string, MapLayerDefinition> _layers =
            new Dictionary<string, MapLayerDefinition>();
        private static bool _builtInsLoaded;

        public static IReadOnlyDictionary<string, MapLayerDefinition> All
        {
            get { EnsureBuiltIns(); return _layers; }
        }

        public static void Clear()
        {
            _layers.Clear();
            _builtInsLoaded = false;
        }

        public static void Register(MapLayerDefinition def)
        {
            if (def == null || string.IsNullOrWhiteSpace(def.id)) return;
            EnsureBuiltIns(); // 自定义层可以覆盖内置层
            _layers[def.id] = def;
        }

        public static bool TryGet(string id, out MapLayerDefinition def)
        {
            EnsureBuiltIns();
            return _layers.TryGetValue(id, out def);
        }

        public static List<MapLayerDefinition> List(string group = null, bool includeOverlays = true)
        {
            EnsureBuiltIns();
            var result = new List<MapLayerDefinition>();
            foreach (var layer in _layers.Values)
            {
                if (!includeOverlays && layer.isOverlay) continue;
                if (!string.IsNullOrEmpty(group) && layer.group != group) continue;
                result.Add(layer);
            }
            result.Sort((a, b) => a.order.CompareTo(b.order));
            return result;
        }

        private static void EnsureBuiltIns()
        {
            if (_builtInsLoaded) return;
            _builtInsLoaded = true;
            Add("terrain", "地形", "nature", MapDisplayMode.Terrain, 0);
            Add("climate", "气候", "nature", MapDisplayMode.Climate, 1);
            Add("biome", "群系", "nature", MapDisplayMode.Biome, 2);
            Add("political", "政治", "society", MapDisplayMode.Political, 10);
            Add("population", "人口", "society", MapDisplayMode.Population, 11);
            Add("diplomacy", "外交", "society", MapDisplayMode.Diplomacy, 12);
            Add("alliance", "联盟", "society", MapDisplayMode.Alliance, 13);
            Add("culture", "文化", "society", MapDisplayMode.Culture, 20);
            Add("culture_branch", "文化分支", "society", MapDisplayMode.CultureBranch, 21);
            Add("religion", "宗教", "society", MapDisplayMode.Religion, 22);
            Add("religion_succession", "教统", "society", MapDisplayMode.ReligionSuccession, 23);
            Add("religion_tradition", "宗教传统", "society", MapDisplayMode.ReligionTradition, 24);
            Add("province_borders", "省界", "overlay", MapDisplayMode.Political, 100, true);
            Add("admin_borders", "行政层级", "overlay", MapDisplayMode.Political, 101, true);
        }

        private static void Add(string id, string label, string group, MapDisplayMode mode,
            int order, bool overlay = false)
        {
            _layers[id] = new MapLayerDefinition
            {
                id = id, label = label, group = group, mode = mode,
                order = order, isOverlay = overlay,
            };
        }
    }
}
