using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Content.Definitions
{
/// <summary>
/// P1.3 WP3 绑定入口：把 religion 的选中教义解析成 DoctrineBindings。
/// 内容仍是 Doctrines.json 的 optionId；Faith 叶子只出快照，消费在 Simulation 层。
/// </summary>
public static class DoctrineBindingHost
{
	/// <summary>伦理绑定是否把该行为定为罪行（只有 Crime；Shunned 不入罪）。</summary>
	public static bool IsCrime(DoctrineBindings bindings, DoctrineCrimeBehavior behavior)
	{
		if (bindings == null)
		{
			return false;
		}
		return behavior switch
		{
			DoctrineCrimeBehavior.Homosexuality => bindings.homosexuality == DoctrineMoralStance.Crime,
			DoctrineCrimeBehavior.Adultery => bindings.adultery == DoctrineMoralStance.Crime,
			DoctrineCrimeBehavior.Witchcraft => bindings.witchcraft == DoctrineMoralStance.Crime,
			DoctrineCrimeBehavior.Drinking => bindings.drinking == DoctrineMoralStance.Crime,
			DoctrineCrimeBehavior.Usury => bindings.usury == DoctrineMoralStance.Crime,
			DoctrineCrimeBehavior.Kinslaying => bindings.kinslaying == DoctrineMoralStance.Crime,
			_ => false
		};
	}

	public static DoctrineMoralStance GetStance(DoctrineBindings bindings, DoctrineCrimeBehavior behavior)
	{
		if (bindings == null)
		{
			return DoctrineMoralStance.Unspecified;
		}
		return behavior switch
		{
			DoctrineCrimeBehavior.Homosexuality => bindings.homosexuality,
			DoctrineCrimeBehavior.Adultery => bindings.adultery,
			DoctrineCrimeBehavior.Witchcraft => bindings.witchcraft,
			DoctrineCrimeBehavior.Drinking => bindings.drinking,
			DoctrineCrimeBehavior.Usury => bindings.usury,
			DoctrineCrimeBehavior.Kinslaying => bindings.kinslaying,
			_ => DoctrineMoralStance.Unspecified
		};
	}

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
