using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 教义选项池（支柱选项——CK3 tenets 结构参考：中性词汇+宗教专属风味化）
    /// 每个支柱下有选项池——节点从池中选择——选项差异决定偏离度
    /// </summary>


    /// <summary>教义选项池（ReligionCatalog 加载 Doctrines.json）</summary>
    public static class DoctrinePool
    {
        private static Dictionary<string, DoctrineOptionDef> _options = new Dictionary<string, DoctrineOptionDef>();
        private static Dictionary<string, List<DoctrineOptionDef>> _byPillar = new Dictionary<string, List<DoctrineOptionDef>>();

        public static void Load(List<DoctrineOptionDef> options)
        {
            _options.Clear();
            _byPillar.Clear();
            if (options == null) return;
            foreach (var o in options)
            {
                if (o == null || string.IsNullOrEmpty(o.optionId)) continue;
                _options[o.optionId] = o;
                if (!_byPillar.TryGetValue(o.pillar, out var list))
                    _byPillar[o.pillar] = list = new List<DoctrineOptionDef>();
                list.Add(o);
            }
        }

        public static DoctrineOptionDef Get(string optionId)
            => _options.TryGetValue(optionId, out var o) ? o : null;

        /// <summary>某支柱的可用选项（宗教专属过滤：exclusiveReligionIds 空=通用，
        /// 非空=仅匹配宗教）</summary>
        public static List<DoctrineOptionDef> GetOptions(string pillar, int religionId)
        {
            var result = new List<DoctrineOptionDef>();
            if (_byPillar.TryGetValue(pillar, out var list))
                foreach (var o in list)
                    if (o.exclusiveReligionIds.Count == 0 || o.exclusiveReligionIds.Contains(religionId))
                        result.Add(o);
            return result;
        }
    }
}
