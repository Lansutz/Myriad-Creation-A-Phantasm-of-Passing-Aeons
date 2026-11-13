using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// Law 5：无 JSON / 旧档未知 lawId 的降级健壮性测试。
    /// 覆盖：清空/伪造路径→最小回退；未知 lawId 的 RealmLawState/旧 LawComposition/效果计算不抛；
    /// 还原真实 JSON 后加载正常且幂等。
    /// </summary>
    public class LawFallbackRobustnessTests
    {
        [SetUp]
        public void SetUp()
        {
            LawContentLoader.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            // 尽量还原真实 JSON，避免污染其它法律测试；文件不存在时自然回退最小集。
            LawContentLoader.Reset();
            LawContentLoader.LoadFromFile(LawContentLoader.ResolveDefaultPath());
        }

        private static string MissingPath()
        {
            string missing = Path.Combine(Path.GetTempPath(), "law_fallback_missing_9f3a.json");
            if (File.Exists(missing))
            {
                File.Delete(missing);
            }
            return missing;
        }

        [Test]
        public void MissingJson_FallsBackToMinimalCatalog_LastErrorReadable()
        {
            Assert.IsFalse(LawContentLoader.LoadFromFile(MissingPath()));
            Assert.IsFalse(LawContentLoader.HasContent, "缺失 JSON 时 HasContent 应为 false");
            Assert.IsFalse(string.IsNullOrEmpty(LawContentLoader.LastError), "LastError 应可读且非空");

            List<LawGroupDef> groups = DefaultLawCatalog.CreateGroups();
            Assert.AreEqual(2, groups.Count, "最小回退应为 2 组");
            Dictionary<string, LawDef> laws = DefaultLawCatalog.IndexLaws(groups);
            Assert.AreEqual(5, laws.Count, "最小回退应为 5 法");
            foreach (string id in new[] { "tax_light", "tax_standard", "tax_heavy", "militia", "standing_army" })
            {
                Assert.IsTrue(laws.ContainsKey(id), "最小回退缺少 " + id);
            }
            Assert.AreEqual(1, DefaultLawCatalog.CreateDecrees().Count, "最小回退应为 1 法令");
        }

        [Test]
        public void UnknownLawIds_AndNullLists_DoNotThrow_ReturnNeutral()
        {
            var state = new RealmLawState
            {
                groups = new List<LawGroupState>
                {
                    null,
                    new LawGroupState { groupId = "taxation", activeLawId = "missing_law" }
                },
                pendingLaws = new List<PendingLaw>
                {
                    new PendingLaw { lawId = "missing_pending", groupId = "other", startDay = 0, daysToEnact = 1 }
                },
                knownLawIds = new List<string> { "missing_law" }
            };
            var decreeState = new RealmDecreeState
            {
                active = null,
                cooldowns = null
            };
            Dictionary<string, LawDef> index = DefaultLawCatalog.IndexLaws(DefaultLawCatalog.CreateGroups());
            Dictionary<string, DecreeDef> decreeIndex = DefaultLawCatalog.IndexDecrees(DefaultLawCatalog.CreateDecrees());

            LawEffect lawEffect = null;
            Assert.DoesNotThrow(() => lawEffect = LawAndDecreeSystem.SumLawEffects(state, index));
            Assert.AreEqual(1f, lawEffect.taxMultiplier, 0.0001f);
            Assert.DoesNotThrow(() => LawAndDecreeSystem.SumDecreeEffects(decreeState, 1, decreeIndex));
            Assert.DoesNotThrow(() => LawAndDecreeSystem.RevokeDecree(decreeState, "missing", out _));
            Assert.DoesNotThrow(() => LawAndDecreeSystem.TickDecreeExpiry(decreeState, 1));
            Assert.DoesNotThrow(() => LawAndDecreeSystem.TickPendingLaws(state, 999));

            var realm = new RealmData
            {
                realmId = 1,
                lawState = state,
                decreeState = decreeState
            };
            Assert.DoesNotThrow(() => LawEffectRuntime.GetCombinedEffect(realm, 1));
            Assert.AreEqual(1f, LawEffectRuntime.GetTaxMultiplier(realm, 1), 0.0001f);
            Assert.DoesNotThrow(() => LawEffectRuntime.ApplyPeriodicEffects(realm, null, 1, 365));

            var nullLists = new RealmLawState
            {
                groups = null,
                pendingLaws = null,
                knownLawIds = null,
                charterHolderIds = null
            };
            Assert.DoesNotThrow(() => LawAndDecreeSystem.SumLawEffects(nullLists, index));
            Assert.DoesNotThrow(() => LawAndDecreeSystem.EnsureKnownLaws(nullLists, DefaultLawCatalog.CreateGroups()));
            Assert.DoesNotThrow(() => LawRealmHost.EnsureKnownLaws(new RealmData { lawState = nullLists }));
        }

        [Test]
        public void LegacyLawComposition_Migration_DoesNotThrow_AndSkipsMissingLaw()
        {
            // 最小回退包含 tax/military，迁移仍正常。
            Assert.IsFalse(LawContentLoader.LoadFromFile(MissingPath()));
            var composition = new LawComposition();
            composition.nationalTax.primary = 2;
            composition.militaryOrganization.primary = 1;
            RealmLawState migrated = null;
            Assert.DoesNotThrow(() => migrated = LawCompositionMigration.ToLawState(composition));
            Assert.AreEqual("tax_heavy", migrated.GetActiveLaw("taxation"));
            Assert.AreEqual("standing_army", migrated.GetActiveLaw("military"));

            // 伪造只有 other 组的目录：迁移映射 lawId 不在当前目录时应跳过并告警，不抛。
            string json = "{\"groups\":[{\"groupId\":\"other\",\"name\":\"Other\",\"order\":1," +
                          "\"laws\":[{\"lawId\":\"other_law\",\"groupId\":\"other\",\"name\":\"Other Law\"}]}]," +
                          "\"decrees\":[{\"decreeId\":\"other_decree\",\"name\":\"Other Decree\"}]}";
            Assert.IsTrue(LawContentLoader.ApplyJson(json), LawContentLoader.LastError);
            Assert.AreEqual(1, DefaultLawCatalog.CreateGroups().Count);
            var staleComposition = new LawComposition();
            staleComposition.nationalTax.primary = 0;
            staleComposition.militaryRecruitment.primary = 0;
            RealmLawState skipped = null;
            Assert.DoesNotThrow(() => skipped = LawCompositionMigration.ToLawState(staleComposition));
            Assert.AreEqual(0, skipped.groups.Count, "缺失映射 lawId 应跳过，不写入未知 active law");
            Assert.IsNull(skipped.GetActiveLaw("taxation"));
            Assert.IsNull(skipped.GetActiveLaw("military"));
        }

        [Test]
        public void RestoreRealJson_LoadsAndIsIdempotent()
        {
            string path = LawContentLoader.ResolveDefaultPath();
            Assert.IsTrue(File.Exists(path), "测试环境应存在真实 Law JSON：" + path);
            Assert.IsTrue(LawContentLoader.LoadFromFile(path), LawContentLoader.LastError);
            Assert.IsTrue(LawContentLoader.HasContent);
            Assert.AreEqual("", LawContentLoader.LastError);
            int groups = LawContentLoader.Groups.Count;
            int decrees = LawContentLoader.Decrees.Count;
            int laws = 0;
            foreach (LawGroupDef group in LawContentLoader.Groups)
            {
                laws += group?.laws?.Count ?? 0;
            }
            Assert.Greater(groups, 0);
            Assert.Greater(laws, 0);

            Assert.IsTrue(LawContentLoader.LoadFromFile(path), LawContentLoader.LastError);
            Assert.AreEqual(groups, LawContentLoader.Groups.Count, "重复加载组数应一致");
            Assert.AreEqual(decrees, LawContentLoader.Decrees.Count, "重复加载法令数应一致");
            int lawsAgain = 0;
            foreach (LawGroupDef group in LawContentLoader.Groups)
            {
                lawsAgain += group?.laws?.Count ?? 0;
            }
            Assert.AreEqual(laws, lawsAgain, "重复加载法律数应一致");
            Assert.AreEqual(groups, DefaultLawCatalog.CreateGroups().Count, "CreateGroups 应使用已加载 JSON");
        }
    }
}
