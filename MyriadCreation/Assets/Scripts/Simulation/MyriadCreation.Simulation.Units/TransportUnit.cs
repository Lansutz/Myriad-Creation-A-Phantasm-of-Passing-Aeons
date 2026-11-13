using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Units
{
[Serializable]
public class TransportUnit
{
	public const int MaxMembers = 10;

	public const float BaseCapacityPerMember = 20f;

	public TransportUnitType type;

	/// <summary>单位正在执行的采集类活动；采集不要求定居点。</summary>
	public UnitActivity activity = UnitActivity.None;

	public UnitSubcategory subcategory;

	public UnitFunding funding = new UnitFunding();

	public string unitName = "";

	public int actorId = -1;

	public List<MemberSlot> members = new List<MemberSlot>();

	public int leaderCharacterId = -1;

	public List<PackAnimalKind> animals = new List<PackAnimalKind>();

	public List<CartKind> carts = new List<CartKind>();

	public int boats;

	public List<CargoEntry> cargo = new List<CargoEntry>();

	public float foundingProgressDays;

	public float DailyFoodDemand => (float)members.Count * 1f + (float)animals.Count * 1f + (float)carts.Count * 0.25f + (float)boats * 0.25f;

	public float BaseCapacity => (float)members.Count * 20f;

	public int FoundingDaysRequired => 10;

	public float GetCargoAmount(int goodsId)
	{
		float num = 0f;
		foreach (CargoEntry item in cargo)
		{
			if (item.goodsId == goodsId)
			{
				num += item.amount;
			}
		}
		return num;
	}

	public void AddCargo(int goodsId, float amount, float unitWeight = 0.1f, bool fragile = false)
	{
		// 0 号是粮食（合法物产），"无效"用 -1
		if (goodsId < 0 || amount <= 0f)
		{
			return;
		}
		for (int i = 0; i < cargo.Count; i++)
		{
			if (cargo[i].goodsId == goodsId)
			{
				CargoEntry value = cargo[i];
				value.amount += amount;
				cargo[i] = value;
				return;
			}
		}
		cargo.Add(new CargoEntry
		{
			goodsId = goodsId,
			amount = amount,
			unitWeight = unitWeight,
			fragile = fragile
		});
	}

	public bool RemoveCargo(int goodsId, float amount)
	{
		// 0 号是粮食（合法物产），"无效"用 -1
		if (goodsId < 0 || amount <= 0f)
		{
			return false;
		}
		if (GetCargoAmount(goodsId) < amount)
		{
			return false;
		}
		int num = cargo.Count - 1;
		while (num >= 0 && amount > 0f)
		{
			if (cargo[num].goodsId == goodsId)
			{
				CargoEntry value = cargo[num];
				float num2 = Mathf.Min(value.amount, amount);
				value.amount -= num2;
				amount -= num2;
				if (value.amount <= 0.0001f)
				{
					cargo.RemoveAt(num);
				}
				else
				{
					cargo[num] = value;
				}
			}
			num--;
		}
		return true;
	}

	public float ConsumeFood(IReadOnlyDictionary<int, GoodsDef> goodsDefs, float days, out float shortage)
	{
		float num = DailyFoodDemand * Math.Max(0f, days);
		shortage = 0f;
		if (num <= 0f)
		{
			return 0f;
		}
		int num2 = cargo.Count - 1;
		while (num2 >= 0 && num > 0.0001f)
		{
			CargoEntry value = cargo[num2];
			if (IsFood(value.goodsId, goodsDefs))
			{
				float num3 = Mathf.Min(value.amount, num);
				value.amount -= num3;
				num -= num3;
				if (value.amount <= 0.0001f)
				{
					cargo.RemoveAt(num2);
				}
				else
				{
					cargo[num2] = value;
				}
			}
			num2--;
		}
		shortage = Mathf.Max(0f, num);
		return shortage;
	}

	public float FoodDays(IReadOnlyDictionary<int, GoodsDef> goodsDefs)
	{
		float num = 0f;
		foreach (CargoEntry item in cargo)
		{
			if (IsFood(item.goodsId, goodsDefs))
			{
				num += item.amount;
			}
		}
		if (!(DailyFoodDemand <= 0f))
		{
			return num / DailyFoodDemand;
		}
		return float.PositiveInfinity;
	}

	private static bool IsFood(int goodsId, IReadOnlyDictionary<int, GoodsDef> goodsDefs)
	{
		if (goodsDefs == null || !goodsDefs.TryGetValue(goodsId, out var value))
		{
			return false;
		}
		if (value.category != GameEnums.GoodsCategory.Food && value.category != GameEnums.GoodsCategory.Crop)
		{
			return value.category == GameEnums.GoodsCategory.Livestock;
		}
		return true;
	}

	public TransportUnit DetachSubunit(IReadOnlyCollection<int> memberIds, UnitSubcategory newSubcategory, int newLeaderId = -1)
	{
		if (memberIds == null || memberIds.Count == 0)
		{
			return null;
		}
		List<int> selected = new List<int>();
		foreach (int id in memberIds)
		{
			if (id != leaderCharacterId && members.Exists((MemberSlot m) => m.characterId == id))
			{
				selected.Add(id);
			}
		}
		if (selected.Count == 0)
		{
			return null;
		}
		int num = Math.Max(1, members.Count);
		TransportUnit transportUnit = new TransportUnit
		{
			type = type,
			subcategory = newSubcategory,
			unitName = unitName + "·分遣",
			boats = 0
		};
		for (int num2 = cargo.Count - 1; num2 >= 0; num2--)
		{
			CargoEntry value = cargo[num2];
			float num3 = value.amount * (float)selected.Count / (float)num;
			if (!(num3 <= 0f))
			{
				transportUnit.AddCargo(value.goodsId, num3, value.unitWeight, value.fragile);
				value.amount -= num3;
				if (value.amount <= 0.0001f)
				{
					cargo.RemoveAt(num2);
				}
				else
				{
					cargo[num2] = value;
				}
			}
		}
		foreach (int id2 in selected)
		{
			MemberSlot item = members.Find((MemberSlot m) => m.characterId == id2);
			transportUnit.members.Add(item);
		}
		transportUnit.leaderCharacterId = (selected.Contains(newLeaderId) ? newLeaderId : selected[0]);
		for (int num4 = 0; num4 < transportUnit.members.Count; num4++)
		{
			MemberSlot value2 = transportUnit.members[num4];
			if (value2.characterId == transportUnit.leaderCharacterId)
			{
				value2.role = UnitRole.Leader;
			}
			transportUnit.members[num4] = value2;
		}
		members.RemoveAll((MemberSlot m) => selected.Contains(m.characterId));
		return transportUnit;
	}

	public bool TryAddMember(int characterId, UnitRole role = UnitRole.Soldier)
	{
		if (characterId < 0 || members.Count >= 10)
		{
			return false;
		}
		if (members.Exists((MemberSlot m) => m.characterId == characterId))
		{
			return false;
		}
		members.Add(new MemberSlot
		{
			characterId = characterId,
			role = role
		});
		return true;
	}

	public bool TrySetLeader(int characterId)
	{
		if (!members.Exists((MemberSlot m) => m.characterId == characterId))
		{
			return false;
		}
		leaderCharacterId = characterId;
		for (int num = 0; num < members.Count; num++)
		{
			MemberSlot value = members[num];
			value.role = ((value.characterId != characterId) ? value.role : UnitRole.Leader);
			members[num] = value;
		}
		return true;
	}

	public void RemoveMember(int characterId)
	{
		members.RemoveAll((MemberSlot m) => m.characterId == characterId);
		if (leaderCharacterId == characterId)
		{
			leaderCharacterId = -1;
		}
	}

	public bool AssignRole(int characterId, UnitRole role)
	{
		for (int i = 0; i < members.Count; i++)
		{
			if (members[i].characterId == characterId)
			{
				MemberSlot value = members[i];
				value.role = role;
				members[i] = value;
				return true;
			}
		}
		return false;
	}

	public static List<UnitRole> DefaultRoles(TransportUnitType type)
	{
		switch (type)
		{
		case TransportUnitType.Caravan:
			return new List<UnitRole>
			{
				UnitRole.Leader,
				UnitRole.Trader,
				UnitRole.Guide,
				UnitRole.Quartermaster
			};
		case TransportUnitType.Settlers:
			return new List<UnitRole>
			{
				UnitRole.Leader,
				UnitRole.Guide,
				UnitRole.Artisan,
				UnitRole.Healer,
				UnitRole.Scholar
			};
		case TransportUnitType.Army:
		case TransportUnitType.Warband:
			return new List<UnitRole>
			{
				UnitRole.Leader,
				UnitRole.Guard,
				UnitRole.Scout,
				UnitRole.Healer,
				UnitRole.Quartermaster
			};
		default:
			return new List<UnitRole>
			{
				UnitRole.Leader,
				UnitRole.Guide,
				UnitRole.Quartermaster
			};
		}
	}

	public int DraftStrength()
	{
		int num = 0;
		foreach (PackAnimalKind animal in animals)
		{
			num += TransportCatalog.GetAnimal(animal).draftStrength;
		}
		return num;
	}

	public float AnimalCapacity()
	{
		float num = 0f;
		foreach (PackAnimalKind animal in animals)
		{
			num += TransportCatalog.GetAnimal(animal).capacity;
		}
		return num;
	}

	public float CartCapacity()
	{
		int num = DraftStrength();
		float num2 = 0f;
		foreach (CartKind cart2 in carts)
		{
			CartProfile cart = TransportCatalog.GetCart(cart2);
			if (cart.requiredDraftStrength <= num)
			{
				num2 += cart.capacity;
				num -= cart.requiredDraftStrength;
			}
			else if (cart.canBePulledByPeople && members.Count >= 1)
			{
				num2 += cart.capacity;
			}
		}
		return num2;
	}

	public float TotalCapacity()
	{
		return BaseCapacity + AnimalCapacity() + CartCapacity();
	}

	public float CargoWeight()
	{
		float num = 0f;
		foreach (CargoEntry item in cargo)
		{
			num += item.amount * item.unitWeight;
		}
		return num;
	}

	public float FragileWeight()
	{
		float num = 0f;
		foreach (CargoEntry item in cargo)
		{
			if (item.fragile)
			{
				num += item.amount * item.unitWeight;
			}
		}
		return num;
	}

	public float LoadRatio()
	{
		float num = TotalCapacity();
		if (!(num <= 0f))
		{
			return Mathf.Clamp01(CargoWeight() / num);
		}
		return 1f;
	}

	public bool TryAddCargo(int goodsId, float amount, float unitWeight, bool fragile = false)
	{
		if (goodsId < 0 || amount <= 0f || unitWeight < 0f)
		{
			return false;
		}
		if (CargoWeight() + amount * unitWeight > TotalCapacity() + 0.0001f)
		{
			return false;
		}
		for (int i = 0; i < cargo.Count; i++)
		{
			if (cargo[i].goodsId == goodsId && cargo[i].fragile == fragile)
			{
				CargoEntry value = cargo[i];
				value.amount += amount;
				cargo[i] = value;
				return true;
			}
		}
		cargo.Add(new CargoEntry
		{
			goodsId = goodsId,
			amount = amount,
			unitWeight = unitWeight,
			fragile = fragile
		});
		return true;
	}

	public bool TryRemoveCargo(int goodsId, float amount)
	{
		if (goodsId < 0 || amount <= 0f)
		{
			return false;
		}
		for (int i = 0; i < cargo.Count; i++)
		{
			if (cargo[i].goodsId == goodsId)
			{
				if (cargo[i].amount + 0.0001f < amount)
				{
					return false;
				}
				CargoEntry value = cargo[i];
				value.amount -= amount;
				if (value.amount <= 1E-05f)
				{
					cargo.RemoveAt(i);
				}
				else
				{
					cargo[i] = value;
				}
				return true;
			}
		}
		return false;
	}

	public bool TryConvertCargoToAnimal(int goodsId, int count)
	{
		if (count <= 0 || !PackAnimalGoods.TryGetKind(goodsId, out var kind))
		{
			return false;
		}
		if (!TryRemoveCargo(goodsId, count))
		{
			return false;
		}
		for (int i = 0; i < count; i++)
		{
			animals.Add(kind);
		}
		return true;
	}

	public float ManagementBurden()
	{
		return (float)members.Count + (float)animals.Count * 0.5f + (float)carts.Count * 1.5f;
	}

	public float ManagementCapacity()
	{
		float num = 4f;
		if (members.Exists((MemberSlot m) => m.role == UnitRole.Leader))
		{
			num += 3f;
		}
		if (members.Exists((MemberSlot m) => m.role == UnitRole.Quartermaster))
		{
			num += 2f;
		}
		if (UnitTypeRules.IsArmyType(type))
		{
			num *= 1.5f;
		}
		return num;
	}

	public bool HasRole(UnitRole role)
	{
		return members.Exists((MemberSlot m) => m.role == role);
	}

	public bool ConvertTo(TransportUnitType target)
	{
		if (!UnitTypeRules.CanConvert(type, target))
		{
			return false;
		}
		type = target;
		return true;
	}

	public float GetCargo(int goodsId)
	{
		foreach (CargoEntry item in cargo)
		{
			if (item.goodsId == goodsId)
			{
				return item.amount;
			}
		}
		return 0f;
	}

	public float MoveSpeedMultiplier(TerrainConditions conditions)
	{
		float num = 1f;
		bool flag = false;
		float num2 = 0f;
		foreach (PackAnimalKind animal2 in animals)
		{
			PackAnimalProfile animal = TransportCatalog.GetAnimal(animal2);
			num += animal.speedBonus * 0.1f;
			float num3 = AnimalSuitability(animal, conditions);
			if (!flag || num3 > num2)
			{
				num2 = num3;
			}
			flag = true;
		}
		if (!conditions.IsLand)
		{
			num = ((boats > 0) ? 0.6f : 0.05f);
		}
		else
		{
			if (flag)
			{
				num *= Mathf.Clamp(num2, 0.05f, 1f);
			}
			num *= conditions.CrossingMultiplier(boats > 0);
		}
		float num4 = conditions.GroundBearing();
		foreach (CartKind cart in carts)
		{
			num -= TransportCatalog.GetCart(cart).speedPenalty;
			if (!conditions.IsLand || num4 < 0.35f)
			{
				num *= 0.5f;
			}
		}
		if (carts.Count > 0 && DraftStrength() <= 0 && !carts.TrueForAll((CartKind c) => TransportCatalog.GetCart(c).canBePulledByPeople))
		{
			num *= 0.5f;
		}
		num -= LoadRatio() * 0.4f;
		float num5 = ManagementBurden();
		float num6 = Mathf.Max(1f, ManagementCapacity());
		if (num5 > num6)
		{
			num -= Mathf.Clamp01((num5 - num6) / num6) * 0.25f;
		}
		float num7 = Mathf.Max(1f, TotalCapacity());
		num -= Mathf.Clamp01(FragileWeight() / num7) * 0.3f;
		return Mathf.Clamp(num, 0.15f, 2f);
	}

	private static float AnimalSuitability(PackAnimalProfile profile, TerrainConditions c)
	{
		float num = 1f;
		if (c.TemperatureC < profile.minTempC)
		{
			num *= Mathf.Clamp(1f - (profile.minTempC - c.TemperatureC) / 30f, 0.4f, 1f);
		}
		else if (c.TemperatureC > profile.maxTempC)
		{
			num *= Mathf.Clamp(1f - (c.TemperatureC - profile.maxTempC) / 30f, 0.4f, 1f);
		}
		if (c.HumidityPct > profile.maxHumidityPct)
		{
			num *= 0.7f;
		}
		if (c.SoilHumidityPct > profile.maxSoilHumidityPct)
		{
			num *= 0.6f;
		}
		if (c.SlopeDegrees > profile.maxSlopeDegrees)
		{
			num *= 0.5f;
		}
		if (c.Elevation01 > profile.maxElevation01)
		{
			num *= 0.6f;
		}
		if (!c.IsLand)
		{
			num *= 0.05f;
		}
		return num;
	}

	public bool AdvanceFounding(float days)
	{
		if (days <= 0f)
		{
			return false;
		}
		foundingProgressDays += days;
		return foundingProgressDays + 0.0001f >= (float)FoundingDaysRequired;
	}

	public bool IsFoundingReady(IReadOnlyDictionary<int, float> materialCost)
	{
		if (foundingProgressDays + 0.0001f >= (float)FoundingDaysRequired)
		{
			return CanFoundSettlement(materialCost);
		}
		return false;
	}

	public bool CanFoundSettlement(IReadOnlyDictionary<int, float> materialCost)
	{
		if (materialCost == null)
		{
			return false;
		}
		foreach (KeyValuePair<int, float> item in materialCost)
		{
			if (GetCargo(item.Key) + 0.0001f < item.Value)
			{
				return false;
			}
		}
		return true;
	}

	public bool TryConsumeFoundingMaterials(IReadOnlyDictionary<int, float> materialCost)
	{
		if (!CanFoundSettlement(materialCost))
		{
			return false;
		}
		foreach (KeyValuePair<int, float> item in materialCost)
		{
			if (!TryRemoveCargo(item.Key, item.Value))
			{
				return false;
			}
		}
		return true;
	}
}
}
