using System;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class ClassNeedWeightTable
{
	public float[] royalty = new float[8] { 0.1f, 0.2f, 0f, 0f, 0.15f, 0f, 0.3f, 0.25f };

	public float[] nobilityClergy = new float[8] { 0.1f, 0.15f, 0f, 0.25f, 0f, 0f, 0.2f, 0.3f };

	public float[] merchantFreeman = new float[8] { 0.15f, 0.1f, 0.2f, 0.25f, 0.3f, 0f, 0f, 0f };

	public float[] peasant = new float[8] { 0.35f, 0.25f, 0.25f, 0.05f, 0f, 0f, 0.1f, 0f };

	public float[] slave = new float[8] { 0.7f, 0.3f, 0f, 0f, 0f, 0f, 0f, 0f };

	public float[] GetWeights(GameEnums.SocialClass cls)
	{
		return cls switch
		{
			GameEnums.SocialClass.Royalty => royalty, 
			GameEnums.SocialClass.NobilityClergy => nobilityClergy, 
			GameEnums.SocialClass.MerchantFreeman => merchantFreeman, 
			GameEnums.SocialClass.Peasant => peasant, 
			GameEnums.SocialClass.Slave => slave, 
			_ => peasant, 
		};
	}
}
}
