using System.Collections.Generic;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Simulation.LawHost
{
/// <summary>
/// 法律/法令系统契约适配器（B6）：实现 Core.Contracts 的 ILawQuery/ILawCommand，
/// 内部转调现有 LawAndDecreeSystem / LawDecreeViewBuilder / DefaultLawCatalog。
/// UI/其它系统改为依赖接口后，法律实现可独立演进（后续拆 asmdef 时 Core 不反向依赖 Simulation）。
/// 旧静态 API 全部保留，适配器只是 facade 层。
/// </summary>
public sealed class LawContracts : ILawQuery, ILawCommand
{
	private readonly GameWorld _world;

	private Dictionary<string, LawDef> _lawIndex;

	private Dictionary<string, DecreeDef> _decreeIndex;

	public LawContracts(GameWorld world)
	{
		_world = world;
	}

	private RealmData GetRealm(int realmId)
	{
		if (_world?.realms == null || !_world.realms.TryGetValue(realmId, out RealmData value))
		{
			return null;
		}
		return value;
	}

	private Dictionary<string, LawDef> LawIndex()
	{
		if (_lawIndex == null)
		{
			_lawIndex = DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups());
		}
		return _lawIndex;
	}

	private Dictionary<string, DecreeDef> DecreeIndex()
	{
		if (_decreeIndex == null)
		{
			_decreeIndex = DefaultLawCatalog.IndexDecrees(DefaultLawCatalog.CreateDecrees());
		}
		return _decreeIndex;
	}

	private string ResolveCharacterName(int characterId)
	{
		CharacterData character = _world?.GetCharacterManager()?.GetCharacter(characterId);
		return ((character != null) ? (character.firstName + " " + character.lastName) : null);
	}

	/// <summary>
	/// 政权已掌握革新的 id 集合，供法律/法令的 requiredInnovationIds 前置校验。
	/// 革新树不可用时返回 null（= 不做革新门槛校验，保持旧行为）；树存在但该政权暂无革新时返回空集合（= 门槛生效）。
	/// </summary>
	private IReadOnlyCollection<int> OwnedInnovationIds(int realmId)
	{
		InnovationTree tree = _world?.Innovations;
		if (tree == null)
		{
			return null;
		}
		return tree.GetRealmInnovations(realmId);
	}

	// ===== ILawQuery =====

	public string GetActiveLaw(int realmId, string groupId)
	{
		return GetRealm(realmId)?.lawState?.GetActiveLaw(groupId) ?? "";
	}

	public IReadOnlyList<PendingLawInfoDto> GetPendingLaws(int realmId)
	{
		List<PendingLawInfoDto> list = new List<PendingLawInfoDto>();
		RealmData realm = GetRealm(realmId);
		if (realm?.lawState?.pendingLaws == null)
		{
			return list;
		}
		Dictionary<string, LawDef> lawIndex = LawIndex();
		int currentDay = ((_world != null) ? _world.currentDay : 0);
		foreach (PendingLaw pendingLaw in realm.lawState.pendingLaws)
		{
			if (pendingLaw == null)
			{
				continue;
			}
			lawIndex.TryGetValue(pendingLaw.lawId, out LawDef definition);
			list.Add(new PendingLawInfoDto
			{
				lawId = pendingLaw.lawId,
				groupId = pendingLaw.groupId,
				name = ((definition != null) ? definition.name : ""),
				remainingDays = pendingLaw.RemainingDays(currentDay),
				proposerCharacterId = pendingLaw.proposerCharacterId
			});
		}
		return list;
	}

	public IReadOnlyList<string> GetKnownLaws(int realmId)
	{
		RealmData realm = GetRealm(realmId);
		if (realm?.lawState?.knownLawIds == null)
		{
			return new List<string>();
		}
		return new List<string>(realm.lawState.knownLawIds);
	}

	public LawGovernmentViewDto GetGovernmentView(int realmId, int currentDay)
	{
		LawGovernmentViewDto dto = new LawGovernmentViewDto();
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			return dto;
		}
		LawDecreeView view = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), currentDay, ResolveCharacterName, OwnedInnovationIds(realmId));
		foreach (LawViewItem item in view.enactedLaws)
		{
			dto.enactedLaws.Add(ToLawDto(item));
		}
		foreach (LawViewItem item in view.pendingLaws)
		{
			dto.pendingLaws.Add(ToLawDto(item));
		}
		foreach (DecreeViewItem item in view.activeDecrees)
		{
			dto.activeDecrees.Add(ToDecreeDto(item));
		}
		foreach (DecreeViewItem item in view.pendingDecrees)
		{
			dto.pendingDecrees.Add(ToDecreeDto(item));
		}
		foreach (LawViewItem item in view.proposableLaws)
		{
			dto.proposableLaws.Add(ToLawDto(item));
		}
		foreach (DecreeDef decree in view.enactableDecrees)
		{
			dto.enactableDecrees.Add(new DecreeItemDto
			{
				decreeId = decree.decreeId,
				label = ((decree.name != null) ? decree.name : "")
			});
		}
		return dto;
	}

	private static LawItemDto ToLawDto(LawViewItem item)
	{
		return new LawItemDto
		{
			lawId = ((item?.law != null) ? item.law.lawId : ""),
			groupId = ((item?.law != null) ? item.law.groupId : ""),
			label = ((item != null) ? item.label : "")
		};
	}

	private static DecreeItemDto ToDecreeDto(DecreeViewItem item)
	{
		return new DecreeItemDto
		{
			decreeId = ((item?.decree != null) ? item.decree.decreeId : ""),
			label = ((item != null) ? item.label : ""),
			orderCount = ((item != null) ? item.orderCount : 0)
		};
	}

	// ===== ILawCommand =====

	public void EnsureKnownLaws(int realmId)
	{
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			return;
		}
		// 面板/旧档可能在 lawState/decreeState 未建立时进入：由命令层统一兜底初始化。
		realm.lawState ??= new RealmLawState();
		realm.decreeState ??= new RealmDecreeState();
		LawRealmHost.EnsureKnownLaws(realm);
	}

	public int TickPendingLaws(int realmId, int currentDay)
	{
		RealmData realm = GetRealm(realmId);
		if (realm?.lawState == null)
		{
			return 0;
		}
		int completed = LawAndDecreeSystem.TickPendingLaws(realm.lawState, currentDay)?.Count ?? 0;
		LawCompositionDeriver.Apply(realm.lawState, realm.laws);
		return completed;
	}

	public void TickDecreeExpiry(int realmId, int currentDay)
	{
		RealmData realm = GetRealm(realmId);
		if (realm?.decreeState != null)
		{
			LawAndDecreeSystem.TickDecreeExpiry(realm.decreeState, currentDay);
		}
	}

	public bool BeginEnactLaw(int realmId, string lawId, int proposerCharacterId, int currentDay, out string reason)
	{
		reason = "";
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		if (!LawIndex().TryGetValue(lawId, out LawDef law))
		{
			reason = "法律不存在：" + lawId;
			return false;
		}
		return LawRealmHost.BeginEnactLaw(realm, law, proposerCharacterId, currentDay, OwnedInnovationIds(realmId), out reason) != null;
	}

	public bool CancelPendingLaw(int realmId, string groupId, int requesterCharacterId, out string reason)
	{
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		return LawRealmHost.CancelPendingLaw(realm, groupId, requesterCharacterId, out reason);
	}

	public bool EnactDecree(int realmId, string decreeId, int currentDay, out string reason)
	{
		reason = "";
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		if (!DecreeIndex().TryGetValue(decreeId, out DecreeDef decree))
		{
			reason = "法令不存在：" + decreeId;
			return false;
		}
		float authority = realm.lawState.authority;
		ActiveDecree active = LawAndDecreeSystem.EnactDecree(decree, realm.lawState, realm.decreeState, ref realm.treasury, ref authority, realm.lawState.legitimacy, currentDay, OwnedInnovationIds(realmId), out reason);
		realm.lawState.authority = authority;
		return active != null;
	}

	public bool RevokeDecree(int realmId, string decreeId, out string reason)
	{
		reason = "";
		RealmData realm = GetRealm(realmId);
		if (realm?.decreeState == null)
		{
			reason = "政权法令状态不存在";
			return false;
		}
		bool num = LawAndDecreeSystem.RevokeDecree(realm.decreeState, decreeId, out ActiveDecree active);
		if (!num)
		{
			reason = "没有生效中的该法令";
		}
		return num;
	}

	public bool ForceSetLaw(int realmId, string groupId, string lawId, int currentDay, out string reason)
	{
		RealmData realm = GetRealm(realmId);
		if (realm == null)
		{
			reason = "政权不存在";
			return false;
		}
		bool num = LawRealmHost.ForceSetLaw(realm, groupId, lawId, currentDay, out reason);
		if (num)
		{
			_lawIndex = null;
		}
		return num;
	}
}
}
