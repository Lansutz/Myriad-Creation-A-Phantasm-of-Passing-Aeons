using System;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.State
{
[Serializable]
public class ClassProfile
{
	public GameEnums.SocialClass socialClass;

	public EconomicClass economicClass;

	public SocialStratum stratum;

	public float population;

	[Range(0f, 1f)]
	public float populationShare;

	[Range(0f, 100f)]
	public float satisfaction;

	[Range(0f, 1f)]
	public float organization;

	[Range(0f, 100f)]
	public float influence;

	[Range(0f, 100f)]
	public float unrest;

	[Range(0f, 100f)]
	public float support;

	[Range(0f, 100f)]
	public float loyalty;

	public ClassNeedDimension chiefGrievance;

	public string chiefGrievanceReason = "";

	public ClassNeedReport needReport;
}
}
