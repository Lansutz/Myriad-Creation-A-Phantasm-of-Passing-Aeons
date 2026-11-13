using System.Collections.Generic;

namespace MyriadCreation.Content.Definitions
{
public static class InnovationDomainMap
{
	public static readonly Dictionary<InnovationField, InnovationDomain> FieldToDomain = new Dictionary<InnovationField, InnovationDomain>
	{
		[InnovationField.Mining] = InnovationDomain.Technology,
		[InnovationField.Agriculture] = InnovationDomain.Technology,
		[InnovationField.Metallurgy] = InnovationDomain.Technology,
		[InnovationField.Construction] = InnovationDomain.Technology,
		[InnovationField.Medicine] = InnovationDomain.Technology,
		[InnovationField.Machinery] = InnovationDomain.Technology,
		[InnovationField.Craft] = InnovationDomain.Technology,
		[InnovationField.Transport] = InnovationDomain.Technology,
		[InnovationField.Script] = InnovationDomain.Thought,
		[InnovationField.Mathematics] = InnovationDomain.Thought,
		[InnovationField.Philosophy] = InnovationDomain.Thought,
		[InnovationField.NaturalCognition] = InnovationDomain.Thought,
		[InnovationField.Theology] = InnovationDomain.Thought,
		[InnovationField.MilitaryThought] = InnovationDomain.Thought,
		[InnovationField.SocialThought] = InnovationDomain.Thought,
		[InnovationField.Governance] = InnovationDomain.Institution,
		[InnovationField.Law] = InnovationDomain.Institution,
		[InnovationField.Bureaucracy] = InnovationDomain.Institution,
		[InnovationField.Economic] = InnovationDomain.Institution,
		[InnovationField.MilitaryInstitution] = InnovationDomain.Institution,
		[InnovationField.ReligiousInstitution] = InnovationDomain.Institution,
		[InnovationField.Education] = InnovationDomain.Institution,
		[InnovationField.Ritual] = InnovationDomain.Tradition,
		[InnovationField.Festival] = InnovationDomain.Tradition,
		[InnovationField.Folkways] = InnovationDomain.Tradition,
		[InnovationField.Heritage] = InnovationDomain.Tradition
	};

	public static InnovationDomain GetDomain(InnovationField field)
	{
		if (!FieldToDomain.TryGetValue(field, out var value))
		{
			return InnovationDomain.Technology;
		}
		return value;
	}
}
}
