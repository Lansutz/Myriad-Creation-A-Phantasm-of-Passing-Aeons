namespace MyriadCreation.Simulation.Units
{
/// <summary>
/// 运行时单位类别（军事/贸易/迁徙/匪帮/荒野，用于 MapUnit/MapActor 与模块规则）。
/// 与 GameEnums.UnitArmType（步兵/骑兵/海军 军种大类）不是同一概念，保留本名以示区别。
/// </summary>
public enum UnitCategory
{
	Military,
	Trade,
	Migration,
	Bandit,
	Wilderness
}
}
