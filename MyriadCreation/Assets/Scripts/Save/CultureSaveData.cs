using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>
    /// 文化存档 DTO（A1/A2 迁移：文件 schema 与运行时对象分离）。
    /// 字段名/JSON 形状与旧版直接序列化 <c>CultureData</c> 完全一致：
    /// 枚举落成数字、继承法展开为同名字段、官衔条目只留字符串。
    /// 映射器在 Simulation 侧（CultureSaveMapping）。
    /// </summary>
    [Serializable]
    public class CultureSaveData
    {
        public int cultureId;

        public string cultureName;

        public string cultureColorHex = "#ffffff";

        public GameEnums.CultureStage stage;

        // 5A.3 迁移期旧板块字段：保留原字段名以兼容老档，运行时不再新增写入。
        public int livelihoodType;
        public int mobilityType;
        public int burialType;
        public List<int> burialTypes = new List<int>();
        public float[] worshipVector;
        public int materialStyle;
        public int symbolicFocus;
        public List<int> symbolicFoci = new List<int>();
        public int environmentAdapt;
        public List<int> environmentAdapts = new List<int>();
        public int productionPreference;

        public int faithId = -1;

        public string languageId = "";
        public string languageFamilyId = "";
        public string ethnicGroupId = "";
        public string heritageId = "";
        public string ethosId = "";
        public List<string> traditionIds = new List<string>();

        public float maturity;
        public float spreadPower = 1f;
        public int parentCultureId = -1;
        public List<int> childCultureIds = new List<int>();
        public bool allowsBranching = true;
        public bool heraldryEnabled;
        public int mergedIntoCultureId = -1;
        public int lastSplitDay = -1;

        public Color color = Color.white;

        public InheritanceLawSaveData defaultSuccessionLaw = new InheritanceLawSaveData();
        public List<OfficeTitleSaveData> officialTitles = new List<OfficeTitleSaveData>();
        public List<string> innovationAffinities = new List<string>();
    }

    /// <summary>继承法快照：字段名与继承法枚举一致，数值即枚举序（老档 JSON 兼容）。</summary>
    [Serializable]
    public class InheritanceLawSaveData
    {
        public int scope;
        public int branch;
        public int gender;
        public int age;
        public int titleMode;
        public int landMode;
    }

    [Serializable]
    public class OfficeTitleSaveData
    {
        public string office;
        public string polityKey;
        public string titleKey;
    }
}
