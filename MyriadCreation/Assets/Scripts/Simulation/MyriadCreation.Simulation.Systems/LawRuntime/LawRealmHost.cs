using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Simulation.LawHost
{
    /// <summary>
    /// Laws 的 host 侧入口（B6 Laws 首步）：保留原 <c>LawAndDecreeSystem</c> 的 RealmData 公共 API 形状，
    /// 但实现移出 <c>MyriadCreation.Simulation.Laws</c>，使 Laws 核心文件不再直接依赖 RealmData/GameWorld/Systems。
    /// 只做状态编排；纯规则仍在 <c>LawAndDecreeSystem</c>（LawAndDecreeSystem 新签名接收 ILawContext/RealmLawState）。
    /// </summary>
    public static class LawRealmHost
    {
        // ===================== 已知法律 =====================

        /// <summary>把"初始已知"法律与当前生效法律补进政权已知集（新政权/旧档初始化用）。</summary>
        public static int EnsureKnownLaws(RealmData realm)
        {
            if (realm == null)
            {
                return 0;
            }
            realm.lawState ??= new RealmLawState();
            return LawAndDecreeSystem.EnsureKnownLaws(realm.lawState, DefaultLawCatalog.CreateGroups());
        }

        /// <summary>外部来源（文化/革新/接触等）登记一条已知法律。</summary>
        public static bool AddKnownLaw(RealmData realm, string lawId)
        {
            if (realm == null)
            {
                return false;
            }
            realm.lawState ??= new RealmLawState();
            return realm.lawState.AddKnownLaw(lawId);
        }

        /// <summary>授予特许权（特许权持有者可推行 ProposalAuthority.CharterHolders 的法律）。</summary>
        public static bool GrantCharter(RealmData realm, int characterId)
        {
            if (realm == null)
            {
                return false;
            }
            realm.lawState ??= new RealmLawState();
            return realm.lawState.AddCharterHolder(characterId);
        }

        /// <summary>收回特许权。</summary>
        public static bool RevokeCharter(RealmData realm, int characterId)
        {
            return realm?.lawState != null && realm.lawState.RemoveCharterHolder(characterId);
        }

        // ===================== 提案授权 =====================

        /// <summary>提案授权（host 重载）：装配 <see cref="LawRealmContext"/> 后走 Laws 纯逻辑。</summary>
        public static bool CanPropose(RealmData realm, int proposerCharacterId, LawDef law, out string reason)
        {
            if (realm == null)
            {
                reason = "政权不存在";
                return false;
            }
            return LawAndDecreeSystem.CanPropose(
                new LawRealmContext(realm), realm.lawState, realm.laws, proposerCharacterId, law, out reason);
        }

        // ===================== 推行流水线 =====================

        /// <summary>
        /// 开始推行：只登记 pending（不立即替换现行法律，旧法在到期前持续有效）。
        /// 同组唯一；成本在登记时扣除（取消不退还）。
        /// </summary>
        public static PendingLaw BeginEnactLaw(RealmData realm, LawDef law, int proposerCharacterId, int day, out string reason)
        {
            return BeginEnactLaw(realm, law, proposerCharacterId, day, null, out reason);
        }

        public static PendingLaw BeginEnactLaw(RealmData realm, LawDef law, int proposerCharacterId, int day, IReadOnlyCollection<int> ownedInnovationIds, out string reason)
        {
            reason = "";
            if (!CanPropose(realm, proposerCharacterId, law, out reason))
            {
                return null;
            }
            RealmLawState lawState = realm.lawState;
            if (lawState.GetPendingLaw(law.groupId) != null)
            {
                reason = "该组已有推行中的法律";
                return null;
            }
            if (lawState.GetActiveLaw(law.groupId) == law.lawId)
            {
                reason = "该法律已经生效";
                return null;
            }
            if (!LawAndDecreeSystem.CanEnactLaw(law, lawState, realm.treasury, realm.prestige, lawState.legitimacy, realm.centralization, ownedInnovationIds, out reason))
            {
                return null;
            }
            int num = Math.Max(1, law.enactmentDays);
            RealmTreasury.Set(realm, Math.Max(0f, realm.treasury - law.enactmentCostTreasury));
            realm.prestige = Math.Max(0f, realm.prestige - law.enactmentCostPrestige);
            PendingLaw pendingLaw = new PendingLaw
            {
                lawId = law.lawId,
                groupId = law.groupId,
                startDay = day,
                daysToEnact = num,
                proposerCharacterId = proposerCharacterId,
                status = "enacting"
            };
            lawState.pendingLaws ??= new List<PendingLaw>();
            lawState.pendingLaws.Add(pendingLaw);
            LawGroupState groupState = lawState.groups?.Find(g => g != null && g.groupId == law.groupId);
            if (groupState == null)
            {
                groupState = new LawGroupState { groupId = law.groupId };
                lawState.groups ??= new List<LawGroupState>();
                lawState.groups.Add(groupState);
            }
            groupState.stage = LegalNormStage.Pending;
            return pendingLaw;
        }

        /// <summary>推进多个政权的 pending（GameWorld 每日调度入口用）；返回 (realmId, lawId) 列表。</summary>
        public static List<(int realmId, string lawId)> TickAllPendingLaws(IEnumerable<RealmData> realms, int day)
        {
            List<(int, string)> list = new List<(int, string)>();
            if (realms == null)
            {
                return list;
            }
            foreach (RealmData realm in realms)
            {
                if (realm?.lawState == null)
                {
                    continue;
                }
                foreach (PendingLaw pendingLaw in LawAndDecreeSystem.TickPendingLaws(realm.lawState, day))
                {
                    list.Add((realm.realmId, pendingLaw.lawId));
                }
                LawCompositionDeriver.Apply(realm.lawState, realm.laws);
            }
            return list;
        }

        /// <summary>取消推行中法律：发起人或首脑可取消；已扣成本不退还，现行法律始终未变。</summary>
        public static bool CancelPendingLaw(RealmData realm, string groupId, int requesterCharacterId, out string reason)
        {
            reason = "";
            if (realm?.lawState == null)
            {
                reason = "政权法律状态不存在";
                return false;
            }
            PendingLaw pendingLaw = realm.lawState.GetPendingLaw(groupId);
            if (pendingLaw == null)
            {
                reason = "没有该组推行中的法律";
                return false;
            }
            if (requesterCharacterId != pendingLaw.proposerCharacterId && requesterCharacterId != realm.GetSupremeRulerId())
            {
                reason = "只有发起人或首脑可以取消";
                return false;
            }
            realm.lawState.pendingLaws.Remove(pendingLaw);
            return true;
        }

        // ===================== 政变/派系路径 =====================

        /// <summary>
        /// 派系斗争/政变/起义路径：绕过已知/授权/前置/推行，直接固化法律。
        /// 清同组 pending，并把目标法补进已知集；调用方负责写编年史。
        /// </summary>
        public static bool ForceSetLaw(RealmData realm, string groupId, string lawId, int day, out string reason)
        {
            reason = "";
            if (realm?.lawState == null)
            {
                reason = "政权法律状态不存在";
                return false;
            }
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(lawId))
            {
                reason = "法律未归组";
                return false;
            }
            realm.lawState.pendingLaws?.RemoveAll((PendingLaw p) => p != null && p.groupId == groupId);
            realm.lawState.SetActiveLaw(groupId, lawId, day);
            realm.lawState.AddKnownLaw(lawId);
            LawCompositionDeriver.Apply(realm.lawState, realm.laws);
            return true;
        }

        /// <summary>
        /// 派系斗争/政变 Reform 成功后同步到 catalog 法律流水线：
        /// 命中映射则 ForceSetLaw（清同组 pending、并入 known、立即固化）；
        /// 不修改调用方已写入的 realm.laws（LawComposition）改革结果。
        /// </summary>
        public static bool ForceReformSync(RealmData realm, PolityComponentInnovations.PolityDimension dimension, int newComponent, int day, out string reason)
        {
            reason = "";
            if (!TryMapReformToCatalogLaw(dimension, newComponent, out string groupId, out string lawId, out reason))
            {
                return false;
            }
            if (!ForceSetLaw(realm, groupId, lawId, day, out reason))
            {
                return false;
            }
            reason = "";
            return true;
        }
    /// <summary>
    /// 派系/政变改革维度 + 新组件 → catalog 法律（groupId/lawId）的最小映射表（host 侧，B6）：
    /// 仅接线有 catalog 法律语义的维度；其余返回 false 并给出 reason。
    /// </summary>
    public static bool TryMapReformToCatalogLaw(PolityComponentInnovations.PolityDimension dimension, int newComponent, out string groupId, out string lawId, out string reason)
    {
        string mappedGroup = null;
        string mappedLaw = null;
        switch (dimension)
        {
            case PolityComponentInnovations.PolityDimension.SupremeSuccession:
                switch (newComponent)
                {
                    case 0:
                        mappedGroup = "succession";
                        mappedLaw = "succ_hereditary";
                        break;
                    case 1:
                        mappedGroup = "succession";
                        mappedLaw = "succ_elective";
                        break;
                    case 2:
                        mappedGroup = "succession";
                        mappedLaw = "succ_elective";
                        break;
                    case 3:
                        mappedGroup = "succession";
                        mappedLaw = "succ_usurpation";
                        break;
                    case 4:
                        mappedGroup = "succession";
                        mappedLaw = "succ_rotation";
                        break;
                }
                break;
            case PolityComponentInnovations.PolityDimension.SupremeScope:
                switch (newComponent)
                {
                    case 0:
                        mappedGroup = "supreme_scope";
                        mappedLaw = "scope_absolute";
                        break;
                    case 1:
                        mappedGroup = "supreme_scope";
                        mappedLaw = "scope_legally_bound";
                        break;
                    case 2:
                        mappedGroup = "supreme_scope";
                        mappedLaw = "scope_custom_bound";
                        break;
                    case 3:
                        mappedGroup = "supreme_scope";
                        mappedLaw = "scope_consensual";
                        break;
                    case 4:
                        mappedGroup = "supreme_scope";
                        mappedLaw = "scope_divinely_bound";
                        break;
                }
                break;
            case PolityComponentInnovations.PolityDimension.CentralSuccession:
                switch (newComponent)
                {
                    case 0:
                        mappedGroup = "central_succession";
                        mappedLaw = "cent_succ_appointed";
                        break;
                    case 1:
                        mappedGroup = "central_succession";
                        mappedLaw = "cent_succ_elected";
                        break;
                    case 2:
                        mappedGroup = "central_succession";
                        mappedLaw = "cent_succ_examination";
                        break;
                    case 3:
                        mappedGroup = "central_succession";
                        mappedLaw = "cent_succ_hereditary";
                        break;
                }
                break;
            case PolityComponentInnovations.PolityDimension.CentralInstitution:
                switch (newComponent)
                {
                    case 1:
                        mappedGroup = "privilege";
                        mappedLaw = "charter_free_city";
                        break;
                    case 2:
                        mappedGroup = "privilege";
                        mappedLaw = "charter_free_city";
                        break;
                    case 3:
                        mappedGroup = "privilege";
                        mappedLaw = "charter_free_city";
                        break;
                    case 4:
                        mappedGroup = "military";
                        mappedLaw = "standing_army";
                        break;
                    case 6:
                        mappedGroup = "military";
                        mappedLaw = "standing_army";
                        break;
                }
                break;
            case PolityComponentInnovations.PolityDimension.LocalSuccession:
                switch (newComponent)
                {
                    case 0:
                        mappedGroup = "local_succession";
                        mappedLaw = "local_succ_appointed";
                        break;
                    case 1:
                        mappedGroup = "local_succession";
                        mappedLaw = "local_succ_elected";
                        break;
                    case 3:
                        mappedGroup = "local_succession";
                        mappedLaw = "local_succ_hereditary";
                        break;
                    case 4:
                        mappedGroup = "local_succession";
                        mappedLaw = "local_succ_charter";
                        break;
                }
                break;
            case PolityComponentInnovations.PolityDimension.LocalScope:
                switch (newComponent)
                {
                    case 0:
                        mappedGroup = "local_scope";
                        mappedLaw = "local_scope_full_autonomy";
                        break;
                    case 1:
                        mappedGroup = "local_scope";
                        mappedLaw = "local_scope_fiscal_judicial";
                        break;
                    case 2:
                        mappedGroup = "local_scope";
                        mappedLaw = "local_scope_military_only";
                        break;
                    case 3:
                        mappedGroup = "local_scope";
                        mappedLaw = "local_scope_none";
                        break;
                }
                break;
            case PolityComponentInnovations.PolityDimension.SpatialStructure:
                switch (newComponent)
                {
                    case 0:
                        mappedGroup = "spatial";
                        mappedLaw = "spatial_unitary";
                        break;
                    case 1:
                        mappedGroup = "spatial";
                        mappedLaw = "spatial_federal";
                        break;
                    case 2:
                        mappedGroup = "spatial";
                        mappedLaw = "spatial_confederal";
                        break;
                }
                break;
        }
        if (!string.IsNullOrEmpty(mappedLaw))
        {
            groupId = mappedGroup;
            lawId = mappedLaw;
            reason = "";
            return true;
        }
        groupId = "";
        lawId = "";
        reason = "改革维度 " + dimension + " 组件 " + newComponent + " 无 catalog 法律映射";
        return false;
    }

    }
}
