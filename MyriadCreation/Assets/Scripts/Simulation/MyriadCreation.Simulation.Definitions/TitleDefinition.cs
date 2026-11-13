using System;
using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Simulation.Definitions
{
[Serializable]
public class TitleDefinition
{
	public string titleId;

	public string designId = "";

	public TitleDerivedCategory category;

	public MyriadCreation.Core.Enums.TitleSuccessionMode successionMode;

	public string ownLawPreset = "";
	public float rank;

	public float weight = 1f;

	public string semantic = "";


	public string cultureId;

	public string ethnicGroupId;

	public string regionId;

	public string settlementId;

	public string appellationId;

	public int tier = 4;

	public string superiorTitle = "";

	public Dictionary<string, bool> modules = new Dictionary<string, bool>();

	public List<TitleAppointmentMethod> appointmentOrder = new List<TitleAppointmentMethod>();

	public TitleEligibility eligibility = new TitleEligibility();

	public List<TitleHolding> holders = new List<TitleHolding>();

	public bool inheritable;

	public bool enabled = true;

	public int seats = 1;

	public int maxSeatsPerFamily = -1;

	public int termYears;

	public float salary;

	public float annualPrestige;

	public float familyPowerWeight;

	public float personnelWeight = 1f;

	public bool canPromote;

	public float dismissalResistance;

	public float baseLoyalty = 0.5f;

	public TitleResidence residence = TitleResidence.StateRealm;

	public string nameColor = "";

	public string validPosition = "";

	public List<string> onGetTitle = new List<string>();

	public List<string> onFired = new List<string>();

	public float TotalWeight => personnelWeight * (float)Math.Max(0, seats);

	public bool IsModuleOn(string moduleKey)
	{
		if (string.IsNullOrEmpty(moduleKey) || modules == null)
		{
			return false;
		}
		bool value;
		return modules.TryGetValue(moduleKey, out value) & value;
	}
}
}
