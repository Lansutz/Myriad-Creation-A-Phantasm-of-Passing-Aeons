namespace CivilizationEvolution.Character
{
        [System.Serializable]
        public struct AchievementRecord
        {
            public int warsWon;          // 胜仗
            public int conquests;        // 征服领地
            public int cultureActs;      // 文治（学院/法典/艺术）
            public int poetryActs;       // 诗作/文学创作（诗人/诗人王判定——文治细分）
            public int religionActs;     // 宗教（建寺/朝圣/护教）
            public int faithChanges;     // 改宗次数（叛教者判定）
            public int defeatedBattles;  // 败仗（常胜者判定——无败仗）
            public int massacres;        // 屠城（屠夫判定——负评价）
            public int rebellions;       // 叛乱次数（受爱戴/被憎恨判定）
            public int defensiveWins;    // 防御大捷（铁锤判定——打退入侵）
            public bool usurpedThrone;   // 篡位上位（篡位者判定）
            public bool canonized;       // 死后封圣（圣者判定——死亡结算传入）
            public bool youngAccession;     // 幼年即位（年轻者判定——即位时<16——
 // bool 默认 false 安全——struct 无初始化器）
            public bool ruledUnderRegency;  // 摄政掌权（被架空/护国公/年轻者判定）
            public int schemesSucceeded; // 诈术成功（外交欺诈/密谋）
            public int threatsResolved;  // 化解危机（叛乱/密谋/边境）
            public int expeditions;      // 远征/探险
            public int lostAllLands;     // 失地（反讽绰号）
            public bool famineUnderRule; // 治下大饥荒（负评价）
            public float reignYears;
 /// <summary>区域影响力 0-1（该角色在其所在地区[政治圈/文化圈/地理区]
 /// 的相对影响力——领土规模/声望/周边承认综合——由调用方计算——
 /// 伟大者的判定依据：≥0.6=区域内前列[中等偏上]即可——阿尔弗雷德式：
 /// 未控制整个英格兰但区域内影响力大）</summary>
            public float regionalInfluence;
        }
}
