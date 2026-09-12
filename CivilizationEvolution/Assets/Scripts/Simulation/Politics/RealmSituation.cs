using System.Collections.Generic;
using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class RealmSituation
    {
        public int realmId;

 // —— 生存 ——
        [UnityEngine.Range(0f, 1.5f)] public float foodSecurity = 1f;   // 食品库存/需求比，1=刚好满足，&gt;1有余，&lt;1短缺

 // —— 安全 ——
        [UnityEngine.Range(0f, 100f)] public float publicOrder = 60f;   // 政权地块平均治安/秩序
        public bool atWar;                                  // 是否处于战争状态
        public bool warOnHomeSoil;                          // 本土是否有交战/占领（比 atWar 更伤）
        [UnityEngine.Range(0f, 100f)] public float disasterSeverity;    // 近期灾害/饥荒/瘟疫严重度

 // —— 税负：各阶层税负痛感 0~100（越高越痛；由 TaxSystem.GetTaxSatisfactionImpact 换算）——
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, float> taxPain = new Dictionary<GameEnums.SocialClass, float>();

 // —— 政治通道：各阶层在当前政体下的通道畅通度 0~1（由 PoliticalAccessAnalyzer 解析 composition）——
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, float> politicalAccess = new Dictionary<GameEnums.SocialClass, float>();

 // —— 经济 ——
        [UnityEngine.Range(0f, 1f)] public float tradeFlow = 1f;        // 贸易路线畅通度（被劫/封锁则下降）
        [UnityEngine.Range(0f, 1f)] public float monetaryStability = 1f;// 货币稳定度（1-通胀归一）

 // —— 制度承认：各阶层是否被制度/革新承认为合法存在（SocialClassAvailability 预计算）——
        [System.NonSerialized]
        public Dictionary<GameEnums.SocialClass, bool> classRecognized = new Dictionary<GameEnums.SocialClass, bool>();

 // —— 合法性 ——
        [UnityEngine.Range(0f, 100f)] public float stability = 50f;     // 政权稳定度（RealmData.stability）
        [UnityEngine.Range(0f, 100f)] public float legitimacy = 50f;    // 统治合法性（威望/正统/信仰，外部综合）

 // —— 特权保障：贵族世袭/免税/土地特权被政体保障的程度 0~1 ——
        [UnityEngine.Range(0f, 1f)] public float privilegeSecurity = 1f;

        public float GetTaxPain(GameEnums.SocialClass cls) => taxPain != null ? taxPain.GetValueOrDefault(cls, 20f) : 20f;
        public float GetPoliticalAccess(GameEnums.SocialClass cls) => politicalAccess != null ? politicalAccess.GetValueOrDefault(cls, 0.2f) : 0.2f;
        public bool IsRecognized(GameEnums.SocialClass cls) => classRecognized == null || classRecognized.GetValueOrDefault(cls, true);
    }
}
