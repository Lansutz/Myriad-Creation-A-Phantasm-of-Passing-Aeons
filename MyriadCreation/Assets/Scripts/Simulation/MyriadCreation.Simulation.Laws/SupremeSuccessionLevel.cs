using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Laws
{
public static class SupremeSuccessionLevel
{
	public static bool IsMonarchy(SupremeSuccession s)
	{
		return s == SupremeSuccession.Hereditary || s == SupremeSuccession.Usurpation;
	}

	public static bool IsElective(SupremeSuccession s)
	{
		if (s != SupremeSuccession.ElectiveDirect && s != SupremeSuccession.ElectiveRepresentative)
		{
			return s == SupremeSuccession.Rotation;
		}
		return true;
	}

	public static bool IsMonarchy(SupremeSuccession s, SupremeScope scope)
	{
		if (IsMonarchy(s))
		{
			return true;
		}
		if (IsElective(s))
		{
			return scope == SupremeScope.Absolute;
		}
		return false;
	}

	public static bool IsRepublic(SupremeSuccession s, SupremeScope scope)
	{
		return !IsMonarchy(s, scope);
	}

	public static bool IsMonarchy(LawComposition comp)
	{
		return IsMonarchy((SupremeSuccession)comp.supremeSuccession.primary, (SupremeScope)comp.supremeScope.primary);
	}

	public static bool IsRepublic(LawComposition comp)
	{
		return !IsMonarchy(comp);
	}
}
}
