using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Character
{
    public static class PersonalityDimensions
    {
 /// <summary>七维固定顺序（遍历、数组下标、模板 bias 对齐均以此为唯一来源）</summary>
        public static readonly PersonalityDimension[] All =
        {
            PersonalityDimension.Boldness,
            PersonalityDimension.Compassion,
            PersonalityDimension.Greed,
            PersonalityDimension.Honor,
            PersonalityDimension.Rationality,
            PersonalityDimension.Vengefulness,
            PersonalityDimension.Piety
        };

 /// <summary>数据/存档/事件 JSON 使用的字符串键（与历史拼写完全一致，保证旧数据兼容）</summary>
        public static string Key(this PersonalityDimension dim) => dim switch
        {
            PersonalityDimension.Boldness => "boldness",
            PersonalityDimension.Compassion => "compassion",
            PersonalityDimension.Greed => "greed",
            PersonalityDimension.Honor => "honor",
            PersonalityDimension.Rationality => "rationality",
            PersonalityDimension.Vengefulness => "vengefulness",
            PersonalityDimension.Piety => "piety",
            _ => ""
        };

 /// <summary>中文显示名</summary>
        public static string DisplayName(this PersonalityDimension dim) => dim switch
        {
            PersonalityDimension.Boldness => "大胆",
            PersonalityDimension.Compassion => "悲悯",
            PersonalityDimension.Greed => "贪婪",
            PersonalityDimension.Honor => "荣誉",
            PersonalityDimension.Rationality => "理性",
            PersonalityDimension.Vengefulness => "报复",
            PersonalityDimension.Piety => "虔信",
            _ => "?"
        };

 /// <summary>字符串键解析为枚举（容错：无法识别返回 false，供数据驱动/事件入口使用）</summary>
        public static bool TryParse(string key, out PersonalityDimension dim)
        {
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var d in All)
                    if (string.Equals(d.Key(), key, StringComparison.OrdinalIgnoreCase))
                    { dim = d; return true; }
            }
            dim = default;
            return false;
        }
    }
}
