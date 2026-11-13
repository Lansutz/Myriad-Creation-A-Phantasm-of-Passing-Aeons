using System;
using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
/// <summary>伦理立场：罪行 / 忌讳 / 接纳（未指定 = 无该组教义）。</summary>
public enum DoctrineMoralStance
{
	Unspecified,
	Crime,
	Shunned,
	Accepted
}

public enum DoctrineMarriageStance
{
	Unspecified,
	Forbidden,
	Permitted,
	Allowed
}

public enum DoctrineFuneralRite
{
	Unspecified,
	Burial,
	Cremation,
	SkyBurial,
	Mummification
}

public enum DoctrineClericalGender
{
	Unspecified,
	MaleOnly,
	Either
}

public enum DoctrineClericalMarriage
{
	Unspecified,
	Celibate,
	Allowed
}

/// <summary>教义 schema 的解析结果：把选中选项 id 映射成可被系统消费的绑定点。</summary>
[Serializable]
public class DoctrineBindings
{
	public DoctrineMoralStance homosexuality = DoctrineMoralStance.Unspecified;
	public DoctrineMoralStance adultery = DoctrineMoralStance.Unspecified;
	public DoctrineMoralStance witchcraft = DoctrineMoralStance.Unspecified;
	public DoctrineMarriageStance divorce = DoctrineMarriageStance.Unspecified;
	public DoctrineFuneralRite funeral = DoctrineFuneralRite.Unspecified;
	public DoctrineClericalGender clericalGender = DoctrineClericalGender.Unspecified;
	public DoctrineClericalMarriage clericalMarriage = DoctrineClericalMarriage.Unspecified;
	public bool pacifism;
	public bool sacredKingship;

	public static DoctrineBindings Resolve(IReadOnlyCollection<string> selectedDoctrines)
	{
		DoctrineBindings bindings = new DoctrineBindings();
		if (selectedDoctrines == null)
		{
			return bindings;
		}
		foreach (string optionId in selectedDoctrines)
		{
			bindings.ApplyOption(optionId);
		}
		return bindings;
	}

	public void ApplyOption(string optionId)
	{
		if (string.IsNullOrEmpty(optionId))
		{
			return;
		}
		switch (optionId)
		{
			case "doctrine_homosexuality_crime": homosexuality = DoctrineMoralStance.Crime; break;
			case "doctrine_homosexuality_shunned": homosexuality = DoctrineMoralStance.Shunned; break;
			case "doctrine_homosexuality_accepted": homosexuality = DoctrineMoralStance.Accepted; break;
			case "doctrine_adultery_crime": adultery = DoctrineMoralStance.Crime; break;
			case "doctrine_adultery_shunned": adultery = DoctrineMoralStance.Shunned; break;
			case "doctrine_adultery_accepted": adultery = DoctrineMoralStance.Accepted; break;
			case "doctrine_witchcraft_crime": witchcraft = DoctrineMoralStance.Crime; break;
			case "doctrine_witchcraft_shunned": witchcraft = DoctrineMoralStance.Shunned; break;
			case "doctrine_witchcraft_accepted": witchcraft = DoctrineMoralStance.Accepted; break;
			case "doctrine_divorce_forbidden": divorce = DoctrineMarriageStance.Forbidden; break;
			case "doctrine_divorce_permitted": divorce = DoctrineMarriageStance.Permitted; break;
			case "doctrine_divorce_allowed": divorce = DoctrineMarriageStance.Allowed; break;
			case "doctrine_funeral_burial": funeral = DoctrineFuneralRite.Burial; break;
			case "doctrine_funeral_cremation": funeral = DoctrineFuneralRite.Cremation; break;
			case "doctrine_funeral_sky_burial": funeral = DoctrineFuneralRite.SkyBurial; break;
			case "doctrine_funeral_mummification": funeral = DoctrineFuneralRite.Mummification; break;
			case "doctrine_clerical_gender_male": clericalGender = DoctrineClericalGender.MaleOnly; break;
			case "doctrine_clerical_gender_either": clericalGender = DoctrineClericalGender.Either; break;
			case "doctrine_clerical_marriage_celibate": clericalMarriage = DoctrineClericalMarriage.Celibate; break;
			case "doctrine_clerical_marriage_allowed": clericalMarriage = DoctrineClericalMarriage.Allowed; break;
			case "doctrine_pacifism": pacifism = true; break;
			case "doctrine_sacred_kingship": sacredKingship = true; break;
		}
	}
}
}
