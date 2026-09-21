namespace CivilizationEvolution.Simulation.Diplomacy
{
    public enum SubordinationType
    {
        Tributary,          // 朝贡国：内政完全自主，象征性臣服+进贡（自治度0.9）
        Vassal,             // 附庸国：外交权受限，军事义务，内政基本自主（自治度0.65）
        Associate,          // 附属国：内政受法定监督（顾问/否决法律），外交国防代理（自治度0.45）
        Protectorate,       // 保护国：内政自主，外交与宣战权完全转让（自治度0.3）
        Puppet              // 傀儡国：首脑由宗主指定，一切重大决策需批准（自治度0.1）
    }
}
