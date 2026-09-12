namespace CivilizationEvolution.Politics
{
    public enum ClassNeedDimension
    {
        Subsistence,             // 生存保障：粮食/温饱（农民、奴隶最敏感）
        Security,                // 人身安全：战乱、本土劫掠、治安、灾害
        TaxBurden,               // 税负合理度：对应阶层税种税率（由 TaxSystem 算痛感）
        PoliticalAccess,         // 政治参与/上升通道：政体是否给该阶层通道（选举/议会/科举/城市特许）
        EconomicOpportunity,     // 经济机会：贸易畅通、货币稳定、市场（自由民最敏感）
        InstitutionalRecognition,// 制度承认：该阶层是否被革新/制度承认为合法存在
        Legitimacy,              // 合法性与秩序：政权稳定度、统治合法性（王室/贵族敏感）
        Privilege                // 特权保障：世袭权、免税、土地保障（贵族敏感）
    }
}
