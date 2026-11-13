using System;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class Subordination
{
	public SubordinationType type;

	public int suzerainId;

	public int vassalId;

	public int establishedDay;

	public bool isActive = true;

	public float tributeAmount;

	public float tributeRatio;

	public bool militaryObligation;

	public bool foreignPolicyControl;

	public bool successionControl;

	public float autonomy = 1f;

	public float CalculateAnnualTribute(float vassalIncome)
	{
		return tributeAmount + vassalIncome * tributeRatio;
	}
}
}
