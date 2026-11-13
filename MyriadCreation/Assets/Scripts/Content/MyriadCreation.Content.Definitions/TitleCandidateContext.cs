using System;

namespace MyriadCreation.Content.Definitions
{
[Serializable]
public class TitleCandidateContext
{
	public bool isMale;

	public bool royal;

	public EligibilityScope? scope;

	public string institution;

	public string cultureId;

	public string faithId;

	public string languageId;

	public string regionId;

	public int developmentStage;

	public float scale;
}
}
