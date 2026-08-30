using System.Collections.Generic;
using CivilizationEvolution.Core;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Tech;

namespace CivilizationEvolution.Economy
{
    /// <summary>
    /// 阶层出现事件（用户定稿：研究革新后新阶层出现的事件）
    /// 标志性革新完成 → 检测该革新解锁的阶层（含前置链检查）→ 事件文本+编年史
    /// 例：铸币完成→商人阶层出现；庄园制度→农奴出现；文字+官僚→士人出现
    /// </summary>
    public static class ClassEmergenceEvents
    {
        /// <summary>标志性革新 → 该革新解锁的亚阶层</summary>
        private static readonly Dictionary<int, List<GameEnums.SocialSubclass>> EmergenceMap =
            new Dictionary<int, List<GameEnums.SocialSubclass>>
            {
                [952] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Serf },           // 庄园制度→农奴
                [501] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Tenant },         // 封建→佃农
                [701] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Merchant },       // 铸币→商人
                [600] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Scholar },        // 文字→士人（需官僚/科举）
                [505] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.DebtSlave },      // 成文法→债务奴
                [503] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Scholar, GameEnums.SocialSubclass.StateSlave }, // 官僚→士人+官奴
                [502] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.StateSlave },     // 中央集权→官奴
                [980] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Citizen },        // 雅典议事→市民
                [500] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Citizen, GameEnums.SocialSubclass.DomesticSlave }, // 部落联盟→市民+家奴
                [100] = new List<GameEnums.SocialSubclass> { GameEnums.SocialSubclass.Freeholder }      // 刀耕火种→自耕农
            };

        /// <summary>
        /// 检测革新完成后新出现的阶层（完整可用性判定——排除尚未满足的次级条件）
        /// 返回新出现的亚阶层列表（空=无新阶层）
        /// </summary>
        public static List<GameEnums.SocialSubclass> GetEmergingClasses(int completedInnovationId,
            CultureData culture, InnovationTree innovations, int realmId)
        {
            var result = new List<GameEnums.SocialSubclass>();
            if (!EmergenceMap.TryGetValue(completedInnovationId, out var candidates)) return result;

            foreach (var subclass in candidates)
            {
                if (SocialClassAvailability.IsSubclassAvailable(subclass, culture, innovations, realmId))
                    result.Add(subclass);
            }
            return result;
        }

        /// <summary>阶层出现事件文本（中文——供通知/弹窗）</summary>
        public static string GetEventText(GameEnums.SocialSubclass subclass)
        {
            return subclass switch
            {
                GameEnums.SocialSubclass.Serf => "庄园制兴，农奴阶层出现——人身束缚于土地的新秩序。",
                GameEnums.SocialSubclass.Tenant => "土地租佃成风，佃农阶层出现。",
                GameEnums.SocialSubclass.Merchant => "钱币通行，商人阶层出现——市井之声渐盛。",
                GameEnums.SocialSubclass.Scholar => "文墨传世，士人阶层出现——读书人有了自己的位置。",
                GameEnums.SocialSubclass.DebtSlave => "律法既立，债务奴出现——欠债者以身抵偿。",
                GameEnums.SocialSubclass.StateSlave => "官府大兴，官奴阶层出现——国有劳役的征发对象。",
                GameEnums.SocialSubclass.Citizen => "城邦议事，市民阶层出现——公民权落地生根。",
                GameEnums.SocialSubclass.DomesticSlave => "私有既立，家奴出现——仆役成群之家渐多。",
                GameEnums.SocialSubclass.Freeholder => "垦荒成田，自耕农阶层出现——有地之家心安。",
                _ => "新阶层出现。"
            };
        }

        /// <summary>记录阶层出现编年史（返回出现的阶层数）</summary>
        public static int RecordEmergence(int completedInnovationId, string realmName,
            CultureData culture, InnovationTree innovations, int realmId, Chronicle chronicle)
        {
            var emerging = GetEmergingClasses(completedInnovationId, culture, innovations, realmId);
            foreach (var sub in emerging)
            {
                chronicle?.Add("class_emergence",
                    $"{realmName}：{GetEventText(sub)}",
                    major: false, realmId);
            }
            return emerging.Count;
        }
    }
}
