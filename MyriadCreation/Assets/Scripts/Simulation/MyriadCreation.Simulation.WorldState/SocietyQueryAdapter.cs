using System;
using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.WorldState
{
/// <summary>
/// UI 4b3：ISocietyQuery 的 Simulation host 适配器。
/// 转调 GameWorld.GetRealmSociety(realmId) 的 RealmSociety/ClassProfile；空世界返回 null。
/// </summary>
public sealed class SocietyQueryAdapter : ISocietyQuery
{
	private readonly GameWorld _world;

	public SocietyQueryAdapter(GameWorld world)
	{
		_world = world;
	}

	public SocietySnapshotDto GetSociety(int realmId)
	{
		RealmSociety society = _world?.GetRealmSociety(realmId);
		if (society == null)
		{
			return null;
		}

		RealmData realm = null;
		if (_world?.realms != null)
		{
			_world.realms.TryGetValue(realmId, out realm);
		}
		CultureData culture = ResolveCulture(realm);
		InnovationTree innovations = _world?.GetInnovationTree();

		SocietySnapshotDto dto = new SocietySnapshotDto
		{
			realmId = realmId,
			realmName = realm?.realmName ?? "",
			totalPopulation = society.totalPopulation,
			unrestScore = society.unrestScore,
			dominantClassId = (int)society.dominantClass,
			dominantClassName = ClassName(society.dominantClass),
			mostRestlessClassId = (int)society.mostRestlessClass,
			mostRestlessClassName = ClassName(society.mostRestlessClass)
		};

		List<SocialClassSnapshotDto> classes = new List<SocialClassSnapshotDto>();
		foreach (GameEnums.SocialClass socialClass in Enum.GetValues(typeof(GameEnums.SocialClass)))
		{
			ClassProfile profile = null;
			if (society.classes != null)
			{
				society.classes.TryGetValue(socialClass, out profile);
			}
			bool available = SocialClassAvailability.IsClassAvailable(socialClass, culture, innovations, realmId);
			bool unlocked = profile != null || available;
			classes.Add(ToClassDto(socialClass, profile, unlocked));
		}
		classes.Sort((a, b) => a.classId.CompareTo(b.classId));
		dto.classes = classes;
		return dto;
	}

	private CultureData ResolveCulture(RealmData realm)
	{
		if (realm == null || _world?.cultures == null)
		{
			return null;
		}
		_world.cultures.TryGetValue(realm.primaryCultureId, out CultureData culture);
		return culture;
	}

	private static SocialClassSnapshotDto ToClassDto(GameEnums.SocialClass socialClass, ClassProfile profile, bool unlocked)
	{
		SocialClassSnapshotDto dto = new SocialClassSnapshotDto
		{
			classId = (int)socialClass,
			className = ClassName(socialClass),
			unlocked = unlocked,
			availabilityReason = unlocked ? "" : "尚未满足社会/创新条件"
		};
		if (profile == null)
		{
			return dto;
		}

		dto.population = profile.population;
		dto.populationShare = profile.populationShare;
		dto.satisfaction = profile.satisfaction;
		dto.loyalty = profile.loyalty;
		dto.organization = profile.organization;
		dto.influence = profile.influence;
		dto.unrest = profile.unrest;
		dto.support = profile.support;
		dto.chiefGrievanceId = (int)profile.chiefGrievance;
		dto.chiefGrievanceReason = profile.chiefGrievanceReason ?? "";

		if (profile.needReport?.dimensions != null)
		{
			List<ClassNeedScoreDto> scores = new List<ClassNeedScoreDto>();
			foreach (var pair in profile.needReport.dimensions)
			{
				scores.Add(new ClassNeedScoreDto
				{
					dimensionId = (int)pair.Key,
					dimensionName = DimensionName(pair.Key),
					score = pair.Value.score,
					reason = pair.Value.reason ?? ""
				});
			}
			scores.Sort((a, b) => a.dimensionId.CompareTo(b.dimensionId));
			dto.needScores = scores;
		}
		return dto;
	}

	private static string ClassName(GameEnums.SocialClass socialClass)
	{
		switch (socialClass)
		{
		case GameEnums.SocialClass.Royalty: return "王室";
		case GameEnums.SocialClass.NobilityClergy: return "贵族教士";
		case GameEnums.SocialClass.MerchantFreeman: return "市民商人";
		case GameEnums.SocialClass.Peasant: return "农民";
		case GameEnums.SocialClass.Slave: return "奴隶";
		default: return socialClass.ToString();
		}
	}

	private static string DimensionName(ClassNeedDimension dimension)
	{
		switch (dimension)
		{
		case ClassNeedDimension.Subsistence: return "生存保障";
		case ClassNeedDimension.Security: return "人身安全";
		case ClassNeedDimension.TaxBurden: return "税负合理";
		case ClassNeedDimension.PoliticalAccess: return "政治通道";
		case ClassNeedDimension.EconomicOpportunity: return "经济机会";
		case ClassNeedDimension.InstitutionalRecognition: return "制度承认";
		case ClassNeedDimension.Legitimacy: return "合法秩序";
		case ClassNeedDimension.Privilege: return "特权保障";
		default: return dimension.ToString();
		}
	}
}
}
