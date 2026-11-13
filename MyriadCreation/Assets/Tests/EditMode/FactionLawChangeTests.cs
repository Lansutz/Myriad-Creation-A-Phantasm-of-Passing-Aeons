using NUnit.Framework;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 派系斗争/政变 → catalog 法律同步测试：ForceReformSync 映射表命中后 ForceSetLaw
    /// （active 变更 / 同组 pending 清除 / 并入 known），无映射维度返回 false 不改状态；
    /// 并验证 LawReform.Reform 成功分支会把政变结果同步到 catalog 法律。
    /// </summary>
    public class FactionLawChangeTests
    {
        private static RealmData NewRealm(int monarchId = 100)
        {
            var realm = new RealmData
            {
                realmId = 1,
                realmName = "派系测试国",
                successionLawFromCulture = false,
                monarchId = monarchId,
                consulId = -1,
                heirId = -1,
                treasury = 1000f,
                prestige = 500f,
                centralization = 0.5f
            };
            realm.lawState = new RealmLawState { legitimacy = 60f };
            realm.laws.centralInstitution = new ComponentChoice((int)CentralInstitution.None);
            LawRealmHost.EnsureKnownLaws(realm);
            return realm;
        }

        private static void AddPending(RealmData realm, string groupId, string lawId)
        {
            realm.lawState.pendingLaws.Add(new PendingLaw
            {
                lawId = lawId,
                groupId = groupId,
                startDay = 1,
                daysToEnact = 30,
                proposerCharacterId = 100
            });
        }

        // ===================== 1. 命中映射：军事委员会 → military 组常备军 =====================

        [Test]
        public void ForceReformSync_MilitaryCouncil_MapsToStandingArmy()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 5);
            AddPending(realm, "military", "militia");
            AddPending(realm, "taxation", "tax_standard");

            bool ok = LawRealmHost.ForceReformSync(realm,
                PolityComponentInnovations.PolityDimension.CentralInstitution,
                (int)CentralInstitution.MilitaryCouncil, 55, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual("standing_army", realm.lawState.GetActiveLaw("military"), "政变改革应直接固化常备军");
            Assert.IsTrue(realm.lawState.IsLawKnown("standing_army"), "被政变固化的法律应并入 known");
            Assert.IsFalse(realm.lawState.HasPendingLaw("military"), "同组 pending 应被清除");
            Assert.IsTrue(realm.lawState.HasPendingLaw("taxation"), "其它组 pending 不应被误伤");
            Assert.AreEqual("tax_light", realm.lawState.GetActiveLaw("taxation"), "其它组现行法律不变");
            Assert.AreEqual((int)CentralInstitution.None, realm.laws.centralInstitution.primary, "同步不修改 realm.laws 的 Reform 结果");
        }

        // ===================== 2. 命中映射：议会 → privilege 组自由市特许 =====================

        [Test]
        public void ForceReformSync_Assembly_MapsToCharterFreeCity()
        {
            var realm = NewRealm();

            bool ok = LawRealmHost.ForceReformSync(realm,
                PolityComponentInnovations.PolityDimension.CentralInstitution,
                (int)CentralInstitution.Assembly, 66, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual("charter_free_city", realm.lawState.GetActiveLaw("privilege"), "议会改革应固化为自由市特许");
            Assert.IsTrue(realm.lawState.IsLawKnown("charter_free_city"));
        }

        // ===================== 3. 无映射：返回 false 且不改状态 =====================

        [Test]
        public void ForceReformSync_NoMapping_ReturnsFalseWithoutChange()
        {
            var realm = NewRealm();
            realm.lawState.SetActiveLaw("taxation", "tax_light", 5);
            int groupsBefore = realm.lawState.groups.Count;

            // 越界组件：该维度已全覆盖，但组件 99 无 catalog 法律映射
            bool unmappedComponent = LawRealmHost.ForceReformSync(realm,
                PolityComponentInnovations.PolityDimension.SupremeSuccession, 99, 70, out string reason);
            Assert.IsFalse(unmappedComponent, "无映射组件应返回 false");
            StringAssert.Contains("无 catalog 法律映射", reason);

            bool unmappedInstitution = LawRealmHost.ForceReformSync(realm,
                PolityComponentInnovations.PolityDimension.CentralInstitution, (int)CentralInstitution.None, 71, out reason);
            Assert.IsFalse(unmappedInstitution, "无映射中央机构组件应返回 false");
            StringAssert.Contains("无 catalog 法律映射", reason);

            Assert.AreEqual("tax_light", realm.lawState.GetActiveLaw("taxation"), "无映射不应改现行法律");
            Assert.IsNull(realm.lawState.GetActiveLaw("military"));
            Assert.AreEqual(groupsBefore, realm.lawState.groups.Count);
        }

        // ===================== 4. 空政权不崩 =====================

        [Test]
        public void ForceReformSync_NullRealm_ReturnsFalseWithoutThrow()
        {
            bool ok = LawRealmHost.ForceReformSync(null,
                PolityComponentInnovations.PolityDimension.CentralInstitution,
                (int)CentralInstitution.MilitaryCouncil, 1, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("政权法律状态不存在", reason);
        }

        // ===================== 5. LawReform.Reform 成功分支接线（真实 Reform 路径） =====================

        [Test]
        public void LawReform_Reform_SyncsCatalogLaw_AndKeepsComposition()
        {
            ContentRegistry.Reset();
            ContentRegistry.Initialize();
            try
            {
                var tree = new InnovationTree();
                var realm = NewRealm();
                realm.laws.centralInstitution = new ComponentChoice((int)CentralInstitution.None);
                var chronicle = new Chronicle();

                bool reformed = LawReform.Reform(realm,
                    PolityComponentInnovations.PolityDimension.CentralInstitution,
                    (int)CentralInstitution.Court, tree, chronicle, 77);

                Assert.IsTrue(reformed, "中央机构=王庭（无革新前置）应可改革");
                Assert.AreEqual((int)CentralInstitution.Court, realm.laws.centralInstitution.primary,
                    "Reform 对 LawComposition 的结果必须保留");
                Assert.AreEqual("charter_free_city", realm.lawState.GetActiveLaw("privilege"),
                    "政变/派系改革成功后应同步固化 catalog 法律");
                Assert.IsTrue(realm.lawState.IsLawKnown("charter_free_city"));
                Assert.AreEqual(1, chronicle.GetEntriesByType("reform").Count, "原有改革编年史保留");
                Assert.AreEqual(1, chronicle.GetEntriesByType("law_reform_sync").Count, "同步应写一条编年史");

                // 无 catalog 映射的组件：Reform 仍成功，但不产生 catalog 法律同步
                bool reformedUnmapped = LawReform.Reform(realm,
                    PolityComponentInnovations.PolityDimension.SupremeSuccession, 99, tree, chronicle, 78);
                Assert.IsTrue(reformedUnmapped);
                Assert.AreEqual(1, chronicle.GetEntriesByType("law_reform_sync").Count,
                    "无 catalog 映射的组件不应重复写同步记录");
            }
            finally
            {
                ContentRegistry.Reset();
            }
        }

        // ===================== 6. Reform 失败不触发同步 =====================

        [Test]
        public void LawReform_Reform_NullInnovations_ReturnsFalseWithoutSync()
        {
            var realm = NewRealm();
            var chronicle = new Chronicle();

            bool reformed = LawReform.Reform(realm,
                PolityComponentInnovations.PolityDimension.CentralInstitution,
                (int)CentralInstitution.Court, null, chronicle, 1);

            Assert.IsFalse(reformed, "无革新树时 Reform 应失败");
            Assert.IsNull(realm.lawState.GetActiveLaw("privilege"), "失败不应同步 catalog 法律");
            Assert.AreEqual(0, chronicle.Count);
        }

        // ===================== 7. 全部 PolityDimension 均有 catalog 法律映射 =====================

        [Test]
        public void ForceReformSync_CoversEveryPolityDimension()
        {
            var cases = new (PolityComponentInnovations.PolityDimension dimension, int component, string groupId, string lawId)[]
            {
                (PolityComponentInnovations.PolityDimension.SupremeSuccession, 0, "succession", "succ_hereditary"),
                (PolityComponentInnovations.PolityDimension.SupremeSuccession, 1, "succession", "succ_elective"),
                (PolityComponentInnovations.PolityDimension.SupremeScope, 1, "supreme_scope", "scope_legally_bound"),
                (PolityComponentInnovations.PolityDimension.CentralSuccession, 2, "central_succession", "cent_succ_examination"),
                (PolityComponentInnovations.PolityDimension.CentralInstitution, 6, "military", "standing_army"),
                (PolityComponentInnovations.PolityDimension.LocalSuccession, 3, "local_succession", "local_succ_hereditary"),
                (PolityComponentInnovations.PolityDimension.LocalScope, 1, "local_scope", "local_scope_fiscal_judicial"),
                (PolityComponentInnovations.PolityDimension.SpatialStructure, 1, "spatial", "spatial_federal")
            };
            foreach (var item in cases)
            {
                var realm = NewRealm();
                Assert.IsTrue(LawRealmHost.TryMapReformToCatalogLaw(item.dimension, item.component,
                    out string groupId, out string lawId, out string reason), reason);
                Assert.AreEqual(item.groupId, groupId, item.dimension + " 组映射");
                Assert.AreEqual(item.lawId, lawId, item.dimension + " 法律映射");

                Assert.IsTrue(LawRealmHost.ForceReformSync(realm, item.dimension, item.component, 10, out reason), reason);
                Assert.AreEqual(item.lawId, realm.lawState.GetActiveLaw(item.groupId),
                    item.dimension + " 政变后应固化到 catalog 法律");
                Assert.IsTrue(realm.lawState.IsLawKnown(item.lawId), item.dimension + " 命中映射应并入 known");
            }
        }
    }
}
