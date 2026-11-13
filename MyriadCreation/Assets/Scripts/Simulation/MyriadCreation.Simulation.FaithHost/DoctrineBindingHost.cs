using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.FaithHost
{
/// <summary>
/// P1.3 WP3 绑定入口：把 religion 的选中教义解析成 DoctrineBindings。
/// 内容仍是 Doctrines.json 的 optionId；Faith 叶子只出快照，消费在 Simulation 层。
/// </summary>
public static class DoctrineBindingHost
{
	public static DoctrineBindings Resolve(ReligionDef religion)
	{
		if (religion == null)
		{
			return new DoctrineBindings();
		}
		return DoctrineBindings.Resolve(religion.selectedDoctrines);
	}
}
}
