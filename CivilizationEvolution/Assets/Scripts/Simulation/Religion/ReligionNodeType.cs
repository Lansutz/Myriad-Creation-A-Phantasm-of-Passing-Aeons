namespace CivilizationEvolution.Culture
{
    public enum ReligionNodeType
    {
        Religion,     // 宗教（根·组织最大单位）
        Succession,   // 教统（组织性单位·互斥——必有主教链/法脉——原"宗派"：
 // 罗马公教会/东正教/各自主教会=独立教统）
        Tradition,    // 传统（思想性传承·可共存——禅宗/唯识宗/法华宗——可创建）
        School,       // 学派（前组织形态·未稳定——无固定仪轨/座堂圣座——可演化）
        ReligiousSchool // 宗教学派（宗教内部思想学派——谶纬/经院学派/教法学派——
 // 与世俗学派（School）区分：世俗学派=宗教的原料[可宗教化]； // 宗教学派=宗教的产物[宗教产生后才有的内部分支]）
    }
}
