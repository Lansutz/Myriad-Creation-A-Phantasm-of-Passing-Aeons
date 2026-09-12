namespace CivilizationEvolution.Simulation.Politics
{
    public enum SupremeSuccession
    {
        Hereditary,                 // 世袭：子选项=继承法（四轴+头衔+领地）；预立太子=内部预案
        ElectiveDirect,             // 选举·直接：公民大会/忽里台/红衣主教团/部落议事会推举（禅让在此）
        ElectiveRepresentative,     // 选举·代议：议会/选举人团（神罗选帝侯/英国议会）
        Usurpation,                 // 僭夺：武力夺权（兵强者上）
        Rotation,                   // 轮座：部落长老轮值
        Divine                      // 神命：祭司/神谕认定
    }
}
