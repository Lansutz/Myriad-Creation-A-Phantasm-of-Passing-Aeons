using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using UnityEngine;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 法律/法令流水线测试：已知法 → 推行中（旧法仍有效）→ 到期固化；提案授权（首脑/中央机构/特许权）；
    /// 同组唯一 pending、取消、政变直改（ForceSetLaw）、RealmDTO 往返、UI 三段视图不泄漏组/类别。
    /// </summary>
    public class LawDecreePipelineTests
    {
        private static LawDef FindLaw(string lawId)
        {
            return DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups())[lawId];
        }

        private static DecreeDef FindDecree(string decreeId)
        {
            return DefaultLawCatalog.IndexDecrees(DefaultLawCatalog.CreateDecrees())[decreeId];
        }

        private static RealmData NewRealm(int monarchId = 100)
        {
            var realm = new RealmData
            {
                realmId = 1,
                realmName = "测试国",
                successionLawFromCulture = false,
                monarchId = monarchId,
                consulId = -1,
                heirId = -1,
                treasury = 1000f,
                prestige = 500f,
                centralization = 0.5f
            };
            realm.lawState = new RealmLawState { legitimacy = 60f };
            realm.laws.centralInstitution = new ComponentChoice((int)CentralInstitution.BureaucraticCore);
            LawRealmHost.EnsureKnownLaws(realm);
            return realm;
        }

        // ===================== 1. 未知法不能提案/推行 =====================

        [Test]
        public void UnknownLaw_CannotProposeOrBegin()
        {
            var realm = NewRealm();
            var heavy = FindLaw("tax_heavy"); // 目录中标记为"初始未知"

            Assert.IsFalse(LawRealmHost.CanPropose(realm, 100, heavy, out string reason), "未知法不可提案");
            StringAssert.Contains("未知", reason);

            Assert.IsNull(LawRealmHost.BeginEnactLaw(realm, heavy, 100, 10, out reason), "未知法不可开始推行");
            Assert.AreEqual(0, realm.lawState.pendingLaws.Count);

            Assert.IsTrue(LawRealmHost.AddKnownLaw(realm, "tax_heavy"), "外部来源可解锁已知法");
            Assert.IsTrue(LawRealmHost.CanPropose(realm, 100, heavy, out _), "解锁后可提案");
        }

        // ===================== 2. 已知法可推行：进入 pending，旧法继续有效 =====================

        [Test]
        public void KnownLaw_BeginsPending_OldLawStillActive()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);
            var standard = FindLaw("tax_standard");
            float treasuryBefore = realm.treasury;

            var pending = LawRealmHost.BeginEnactLaw(realm, standard, 100, 10, out string reason);

            Assert.IsNotNull(pending, reason);
            Assert.AreEqual(1, realm.lawState.pendingLaws.Count);
            Assert.AreEqual("tax_standard", pending.lawId);
            Assert.AreEqual("taxation", pending.groupId);
            Assert.AreEqual(10, pending.startDay);
            Assert.AreEqual(standard.enactmentDays, pending.daysToEnact);
            Assert.AreEqual(100, pending.proposerCharacterId);
            Assert.AreEqual("tax_light", realm.lawState.GetActiveLaw("taxation"), "生效前旧法不能被顶掉");
            Assert.Less(realm.treasury, treasuryBefore, "登记时扣除承诺成本");
            Assert.AreEqual(standard.enactmentDays, pending.RemainingDays(10), "刚登记时剩余天数 = 推行天数");
        }

        // ===================== 3. 到期才替换现行法律 =====================

        [Test]
        public void TickPending_CompletesOnlyAtDueDay()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);
            var standard = FindLaw("tax_standard");
            var pending = LawRealmHost.BeginEnactLaw(realm, standard, 100, 10, out _);
            Assert.IsNotNull(pending);
            int dueDay = pending.startDay + pending.daysToEnact;

            var notYet = LawAndDecreeSystem.TickPendingLaws(realm.lawState, dueDay - 1);
            Assert.AreEqual(0, notYet.Count);
            Assert.AreEqual("tax_light", realm.lawState.GetActiveLaw("taxation"), "未到期旧法仍有效");
            Assert.AreEqual(1, realm.lawState.pendingLaws.Count);

            var completed = LawAndDecreeSystem.TickPendingLaws(realm.lawState, dueDay);
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual("tax_standard", completed[0].lawId);
            Assert.AreEqual("tax_standard", realm.lawState.GetActiveLaw("taxation"), "到期才替换为推行中的法律");
            Assert.AreEqual(0, realm.lawState.pendingLaws.Count);
            LawGroupState taxation = realm.lawState.groups.Find(g => g.groupId == "taxation");
            Assert.IsNotNull(taxation);
            Assert.AreEqual(dueDay, taxation.enactedDay);
        }

        // ===================== 4. 同组唯一 pending =====================

        [Test]
        public void SameGroup_SecondPending_Rejected()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);
            Assert.IsNotNull(LawRealmHost.BeginEnactLaw(realm, FindLaw("tax_standard"), 100, 10, out _));
            Assert.IsTrue(LawRealmHost.AddKnownLaw(realm, "tax_heavy"));

            var second = LawRealmHost.BeginEnactLaw(realm, FindLaw("tax_heavy"), 100, 15, out string reason);

            Assert.IsNull(second, "同组不允许第二个 pending");
            StringAssert.Contains("该组已有推行中", reason);
            Assert.AreEqual(1, realm.lawState.pendingLaws.Count);
        }

        // ===================== 5. 取消 pending：发起人或首脑 =====================

        [Test]
        public void CancelPending_RequiresProposerOrHead()
        {
            var realm = NewRealm(monarchId: 100);
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);
            Assert.IsNotNull(LawRealmHost.BeginEnactLaw(realm, FindLaw("tax_standard"), 100, 10, out _));

            Assert.IsFalse(LawRealmHost.CancelPendingLaw(realm, "taxation", 999, out string reason), "无关角色不能取消");
            StringAssert.Contains("发起人或首脑", reason);

            Assert.IsTrue(LawRealmHost.CancelPendingLaw(realm, "taxation", 100, out _), "发起人可取消");
            Assert.AreEqual(0, realm.lawState.pendingLaws.Count);
            Assert.AreEqual("tax_light", realm.lawState.GetActiveLaw("taxation"), "取消后现行法律不变");

            Assert.IsFalse(LawRealmHost.CancelPendingLaw(realm, "taxation", 100, out reason));
            StringAssert.Contains("没有该组", reason);
        }

        // ===================== 6. 提案授权矩阵：首脑 / 中央机构 / 特许权 =====================

        [Test]
        public void Authority_HeadCentralCharterMatrix()
        {
            var realm = NewRealm(monarchId: 100);

            Assert.IsTrue(LawRealmHost.CanPropose(realm, 100, FindLaw("tax_standard"), out _), "首脑可提案");
            Assert.IsFalse(LawRealmHost.CanPropose(realm, 999, FindLaw("tax_standard"), out string reason));
            StringAssert.Contains("首脑", reason);

            // 中央机构：需要官职持有者
            var militia = FindLaw("militia");
            Assert.IsFalse(LawRealmHost.CanPropose(realm, 100, militia, out reason), "无官职者不可推中央机构法");
            StringAssert.Contains("中央机构", reason);
            realm.officeHolders[1] = 200;
            Assert.IsTrue(LawRealmHost.CanPropose(realm, 200, militia, out _), "中央机构 holder 可提案");

            // 机构不匹配
            var assemblyLaw = new LawDef
            {
                lawId = "assembly_law",
                groupId = "military",
                name = "军事委员会法",
                proposalAuthority = ProposalAuthority.CentralInstitution,
                requiredInstitution = CentralInstitution.Assembly
            };
            Assert.IsTrue(LawRealmHost.AddKnownLaw(realm, assemblyLaw.lawId));
            Assert.IsFalse(LawRealmHost.CanPropose(realm, 200, assemblyLaw, out reason), "中央机构不匹配应拒绝");
            StringAssert.Contains("中央机构", reason);
            realm.laws.centralInstitution = new ComponentChoice((int)CentralInstitution.Assembly);
            Assert.IsTrue(LawRealmHost.CanPropose(realm, 200, assemblyLaw, out _), "机构匹配后可提案");

            // 特许权
            var charterLaw = FindLaw("charter_free_city");
            Assert.IsFalse(LawRealmHost.CanPropose(realm, 200, charterLaw, out reason), "非特许权持有者不可提案");
            StringAssert.Contains("特许权", reason);
            Assert.IsTrue(LawRealmHost.GrantCharter(realm, 300));
            Assert.IsTrue(LawRealmHost.CanPropose(realm, 300, charterLaw, out _), "特许权持有者可提案");
            Assert.IsTrue(LawRealmHost.RevokeCharter(realm, 300));
            Assert.IsFalse(LawRealmHost.CanPropose(realm, 300, charterLaw, out _));
        }

        // ===================== 7. 已知法集初始化 =====================

        [Test]
        public void KnownLaws_InitializedFromCatalog_AndActiveLaws()
        {
            var state = new RealmLawState();
            int added = LawAndDecreeSystem.EnsureKnownLaws(state, DefaultLawCatalog.CreateGroups());

            Assert.Greater(added, 0);
            Assert.IsTrue(state.IsLawKnown("tax_light"));
            Assert.IsTrue(state.IsLawKnown("tax_standard"));
            Assert.IsTrue(state.IsLawKnown("militia"));
            Assert.IsTrue(state.IsLawKnown("charter_free_city"));
            Assert.IsFalse(state.IsLawKnown("tax_heavy"), "目录中初始未知法不应进已知集");
            Assert.IsFalse(state.IsLawKnown("standing_army"));
            Assert.IsFalse(state.IsLawKnown("charter_guild"));

            state.SetActiveLaw("military", "standing_army", 5);
            LawAndDecreeSystem.EnsureKnownLaws(state, DefaultLawCatalog.CreateGroups());
            Assert.IsTrue(state.IsLawKnown("standing_army"), "当前生效法律应自动补进已知集");
            Assert.IsFalse(state.AddKnownLaw("tax_light"), "重复登记返回 false");
        }

        // ===================== 8. 政变直改：ForceSetLaw 绕过流水线 =====================

        [Test]
        public void ForceSetLaw_BypassesKnownAndPending()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);
            Assert.IsNotNull(LawRealmHost.BeginEnactLaw(realm, FindLaw("tax_standard"), 100, 10, out _));

            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "taxation", "tax_heavy", 20, out string reason), reason);
            Assert.AreEqual("tax_heavy", realm.lawState.GetActiveLaw("taxation"), "政变可绕过已知/授权/推行直接改法");
            Assert.AreEqual(0, realm.lawState.pendingLaws.Count, "直改后清空同组 pending");
            Assert.IsTrue(realm.lawState.IsLawKnown("tax_heavy"), "被强制固化的法律补进已知集");

            // 未知且无授权的法律也可被强制固化
            Assert.IsTrue(LawRealmHost.ForceSetLaw(realm, "privilege", "charter_guild", 21, out _));
            Assert.AreEqual("charter_guild", realm.lawState.GetActiveLaw("privilege"));
        }

        // ===================== 9. 法令：正在生效 / 正在推行（命令） =====================

        [Test]
        public void Decree_ActiveAndPendingOrders_ToView()
        {
            var realm = NewRealm();
            float authority = realm.lawState.authority;
            DecreeDef decree = FindDecree("decree_tax_relief");
            ActiveDecree active = LawAndDecreeSystem.EnactDecree(decree, realm.lawState, realm.decreeState,
                ref realm.treasury, ref authority, realm.lawState.legitimacy, 10, null, out string reason);
            realm.lawState.authority = authority;
            Assert.IsNotNull(active, reason);

            realm.decreeState.orders.Add(new DecreeOrder { decreeId = decree.decreeId, state = DecreeOrderState.InTransit });
            realm.decreeState.orders.Add(new DecreeOrder { decreeId = decree.decreeId, state = DecreeOrderState.Pending });

            var view = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), 10, null);
            Assert.AreEqual(1, view.activeDecrees.Count, "正在生效法令一段");
            StringAssert.Contains("法令：", view.activeDecrees[0].label);
            StringAssert.Contains("剩余", view.activeDecrees[0].label);
            Assert.AreEqual(1, view.pendingDecrees.Count, "正在推行法令一段（按法令聚合命令）");
            Assert.AreEqual(2, view.pendingDecrees[0].orderCount);
            StringAssert.Contains("2 道命令", view.pendingDecrees[0].label);

            LawAndDecreeSystem.TickDecreeExpiry(realm.decreeState, active.endDay + 1);
            var expiredView = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), active.endDay + 1, null);
            Assert.AreEqual(0, expiredView.activeDecrees.Count, "到期法令不再出现在生效段");
        }

        // ===================== 10. RealmDTO 往返：已知/推行中/特许保留 =====================

        [Test]
        public void RealmDto_RoundTrip_PreservesKnownPendingCharter()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 3);
            Assert.IsNotNull(LawRealmHost.BeginEnactLaw(realm, FindLaw("tax_standard"), 100, 10, out _));
            Assert.IsTrue(LawRealmHost.GrantCharter(realm, 300));

            RealmDTO dto = RealmDTO.FromRealmData(realm);
            string json = JsonUtility.ToJson(dto);
            RealmDTO reloaded = JsonUtility.FromJson<RealmDTO>(json);
            Assert.IsNotNull(reloaded, "RealmDTO JSON 反序列化不应失败");

            RealmData restored = reloaded.ToRealmData();
            Assert.IsTrue(restored.lawState.IsLawKnown("tax_light"), "已知法应随 lawState 入档");
            Assert.IsTrue(restored.lawState.IsLawKnown("tax_standard"));
            Assert.AreEqual("tax_light", restored.lawState.GetActiveLaw("taxation"));
            Assert.AreEqual(1, restored.lawState.pendingLaws.Count, "pending 应随档往返");
            PendingLaw pending = restored.lawState.pendingLaws[0];
            Assert.AreEqual("tax_standard", pending.lawId);
            Assert.AreEqual("taxation", pending.groupId);
            Assert.AreEqual(10, pending.startDay);
            Assert.AreEqual(100, pending.proposerCharacterId);
            Assert.IsTrue(restored.lawState.IsCharterHolder(300), "特许权持有者应随档往返");
        }

        // ===================== 11. UI 三段 helper：不泄漏组/类别 =====================

        [Test]
        public void LawDecreeView_ThreeSegments_NoGroupLeak()
        {
            var realm = NewRealm(monarchId: 100);
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);
            Assert.IsNotNull(LawRealmHost.BeginEnactLaw(realm, FindLaw("tax_standard"), 100, 10, out _));
            float authority = realm.lawState.authority;
            DecreeDef decree = FindDecree("decree_tax_relief");
            Assert.IsNotNull(LawAndDecreeSystem.EnactDecree(decree, realm.lawState, realm.decreeState,
                ref realm.treasury, ref authority, realm.lawState.legitimacy, 10, null, out _));
            realm.lawState.authority = authority;
            realm.decreeState.orders.Add(new DecreeOrder { decreeId = decree.decreeId, state = DecreeOrderState.InTransit });

            LawDecreeView view = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(),
                DefaultLawCatalog.CreateDecrees(), 10, id => id == 100 ? "张三" : null);

            Assert.AreEqual(1, view.enactedLaws.Count, "已固化制度一段");
            StringAssert.Contains("制度：轻赋", view.enactedLaws[0].label);
            Assert.AreEqual(1, view.pendingLaws.Count, "正在推行法律一段");
            StringAssert.Contains("推行中：常赋", view.pendingLaws[0].label);
            StringAssert.Contains("剩余", view.pendingLaws[0].label);
            StringAssert.Contains("发起人：张三", view.pendingLaws[0].label);
            Assert.AreEqual(1, view.activeDecrees.Count, "正在生效法令一段");
            Assert.AreEqual(1, view.pendingDecrees.Count, "正在推行法令一段");

            string[] leaks = { "taxation", "military", "privilege", "赋税", "军制", "特权", "tax_", "militia", "standing_army", "charter_" };
            foreach (LawViewItem item in view.enactedLaws) AssertNoGroupLeak(item.label, leaks);
            foreach (LawViewItem item in view.pendingLaws) AssertNoGroupLeak(item.label, leaks);
            foreach (LawViewItem item in view.proposableLaws) AssertNoGroupLeak(item.label, leaks);
        }

        // ===================== 12. 候选按钮：只列已知+有权+满足前置 =====================

        [Test]
        public void ProposableLaws_RespectAuthorityAndPrerequisites()
        {
            var realm = NewRealm(monarchId: 100);
            realm.lawState.SetActiveLaw("taxation", "tax_light", 0);

            var view = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), 10, null);
            Assert.IsTrue(view.proposableLaws.Exists(i => i.law.lawId == "tax_standard"), "已知+首脑+前置满足 → 可推行");
            Assert.IsFalse(view.proposableLaws.Exists(i => i.law.lawId == "tax_heavy"), "未知法不作为候选");
            Assert.IsFalse(view.proposableLaws.Exists(i => i.law.lawId == "militia"), "首脑无权推行中央机构法");
            Assert.IsFalse(view.proposableLaws.Exists(i => i.law.lawId == "charter_free_city"), "首脑无权推行特许法");

            // 首脑同时持有中央官职 + 特许 → 候选出现（机构匹配 BureaucraticCore）
            realm.officeHolders[1] = 100;
            Assert.IsTrue(LawRealmHost.GrantCharter(realm, 100));
            var viewAuthorized = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), 10, null);
            Assert.IsTrue(viewAuthorized.proposableLaws.Exists(i => i.law.lawId == "militia"), "中央机构 holder 的首脑可推行中央机构法");
            Assert.IsTrue(viewAuthorized.proposableLaws.Exists(i => i.law.lawId == "charter_free_city"), "特许权持有者可推行特许法");

            // 前置（财政）不满足 → 候选剔除
            realm.treasury = 0f;
            var viewPoor = LawDecreeViewBuilder.Build(realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), 10, null);
            Assert.IsFalse(viewPoor.proposableLaws.Exists(i => i.law.lawId == "tax_standard"), "财政不足的候选不应出现");
        }

        private static void AssertNoGroupLeak(string label, string[] leaks)
        {
            Assert.IsNotNull(label);
            foreach (string leak in leaks)
            {
                Assert.IsFalse(label.Contains(leak), "UI 标签不应泄漏组/类别/内部键：" + leak + " → " + label);
            }
        }
    }
}
