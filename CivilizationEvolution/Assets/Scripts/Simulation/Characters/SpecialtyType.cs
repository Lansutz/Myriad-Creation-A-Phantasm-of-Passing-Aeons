using System;
using System.Collections.Generic;

namespace CivilizationEvolution.Simulation.Characters
{
    /// <summary>
    /// 专精类型（六维专精，0-100）
    /// 在基本能力基础上发展出来的、面向特定领域的专业技能
    /// 专精 = 基本能力 + 学到的技能
    /// </summary>
    public enum SpecialtyType
    {
        /// <summary>社交：处理各种关系的能力，范围从国家间的正式关系到个人间的非正式关系</summary>
        Social,

        /// <summary>阴谋：秘密策划和政治手腕的能力</summary>
        Conspiracy,

        /// <summary>军事：军事指挥和战略能力</summary>
        Military,

        /// <summary>管理：行政管理和经济治理能力</summary>
        Management,

        /// <summary>学识：知识水平和学习能力</summary>
        Scholarship,

        /// <summary>勇武：个人战斗能力和勇猛程度</summary>
        Prowess
    }

    /// <summary>
    /// 专精类型扩展方法
    /// </summary>
    public static class SpecialtyTypeExtensions
    {
        /// <summary>所有专精类型的数组</summary>
        public static readonly SpecialtyType[] All = {
            SpecialtyType.Social,
            SpecialtyType.Conspiracy,
            SpecialtyType.Military,
            SpecialtyType.Management,
            SpecialtyType.Scholarship,
            SpecialtyType.Prowess
        };

        /// <summary>获取专精的中文显示名</summary>
        public static string GetDisplayName(this SpecialtyType type) => type switch
        {
            SpecialtyType.Social => "社交",
            SpecialtyType.Conspiracy => "阴谋",
            SpecialtyType.Military => "军事",
            SpecialtyType.Management => "管理",
            SpecialtyType.Scholarship => "学识",
            SpecialtyType.Prowess => "勇武",
            _ => type.ToString()
        };

        /// <summary>获取专精的描述</summary>
        public static string GetDescription(this SpecialtyType type) => type switch
        {
            SpecialtyType.Social => "处理各种关系的能力，范围从国家间的正式关系到个人间的非正式关系",
            SpecialtyType.Conspiracy => "秘密策划和政治手腕的能力",
            SpecialtyType.Military => "军事指挥和战略能力",
            SpecialtyType.Management => "行政管理和经济治理能力",
            SpecialtyType.Scholarship => "知识水平和学习能力",
            SpecialtyType.Prowess => "个人战斗能力和勇猛程度",
            _ => ""
        };
    }
}
