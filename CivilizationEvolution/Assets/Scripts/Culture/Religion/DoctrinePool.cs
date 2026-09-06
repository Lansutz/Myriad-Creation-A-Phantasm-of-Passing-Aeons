using System.Collections.Generic;
using UnityEngine;

namespace CivilizationEvolution.Culture
{
    /// <summary>
    /// 教义选项池（支柱选项——CK3 tenets 结构参考：中性词汇+宗教专属风味化）
    /// 每个支柱下有选项池——节点从池中选择——选项差异决定偏离度
    /// </summary>
    [System.Serializable]
    public class DoctrineOptionDef
    {
        public string optionId;
        /// <summary>所属支柱（doctrine 教义/ethics 伦理教法/ritual 仪式/
        /// experience 体验/institution 组织/myth 神话/material 物质）</summary>
        public string pillar = "doctrine";
        /// <summary>词条名（中性——历史/宗教学普遍词汇）</summary>
        public string optionName;
        /// <summary>描述</summary>
        public string description = "";
        /// <summary>专属风味化（空=通用词条；非空=仅该宗教可用——
        /// 专属版=中性词条的风味化/加强版——如"吉兹亚"=不信者税的伊斯兰专属名）</summary>
        public List<int> exclusiveReligionIds = new List<int>();
        /// <summary>升级来源（专属版=通用版升级——如伊玛目无误=无中介的组织观升级）</summary>
        public string enhancedFrom = "";
    }

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
