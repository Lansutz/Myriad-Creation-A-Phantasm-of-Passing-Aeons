using System.Collections.Generic;
using System.Reflection;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.WorldState;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// SuccessionSystem 端到端测试（真实 RealmData + CharacterManager 夹具）。
    /// 覆盖：长子继承（君主路径）、幼子继承（另一种继承法）、绝嗣、幼主争议、共和选举（资格+威望）、
    /// 继承线与大圣战受益人、"君主在世/空参"早退，以及修复后的资格横切/绝嗣去重/幼主双记录语义。
    /// 夹具说明：CharacterManager 没有"注册既有角色"的公开 API（正常路径 CreateCharacter 依赖内容表/遗传且随机），
    /// 这里用反射把手工构造的 CharacterData 注入私有 _characters；家族用公开 CreateFamily 建立并回填 familyId。
    /// GameWorld 级测试用 new GameObject + AddComponent&lt;GameWorld&gt;（Awake 为空实现），反射注入
    /// _characterManager/_chronicle 并调用私有 CheckRulerSuccessions；_regimeDynamics 留 null（Notify 内部早退）。
    /// TearDown 清理静态注册表/字段引用；GameWorld 测试在 finally 里 DestroyImmediate 创建的 GameObject。
    /// </summary>
    public class SuccessionEndToEndTests
    {
        private const int RealmId = 3;

        private CharacterManager _characters;
        private RealmData _realm;

        [SetUp]
        public void SetUp()
        {
            CharacterKnowledgeRegistry.Clear();
            _characters = new CharacterManager();
            _realm = CreateMonarchyRealm(RealmId, -1);
            _realm.realmName = "测试王朝";
        }

        [TearDown]
        public void TearDown()
        {
            // 本夹具除 GameWorld 用例（自身 finally 销毁）外不创建 GameObject/世界；
            // 防御性清理角色知识静态表，避免跨测试类残留。
            CharacterKnowledgeRegistry.Clear();
            _characters = null;
            _realm = null;
        }

        // ===================== 测试 1：长子继承（君主路径） =====================

        [Test]
        public void Primogeniture_ExecuteSuccession_PicksEldestSonAndUpdatesRealm()
        {
            _realm.monarchId = 100;
            _realm.laws.successionLaw = InheritanceLaw.Primogeniture();
            _realm.heirId = 55; // 旧 heir 必须在继位后被清空

            CharacterData ruler = AddCharacter(100, 60, true);
            CharacterData eldestSon = AddCharacter(101, 30, true, fatherId: 100);
            CharacterData secondSon = AddCharacter(102, 24, true, fatherId: 100);
            CharacterData elderDaughter = AddCharacter(103, 40, false, fatherId: 100);
            CharacterData minorSon = AddCharacter(104, 8, true, fatherId: 100);
            CreateDynasty(ruler, eldestSon, secondSon, elderDaughter, minorSon);
            Kill(ruler, 500);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 500);

            Assert.IsTrue(result.triggered, "君主死亡后应触发继承");
            Assert.IsTrue(result.succeeded, "有合法子嗣时继承应成功");
            Assert.IsFalse(result.disputed, "成年长子继位不应争议");
            Assert.AreEqual(100, result.deadRulerId);
            Assert.AreEqual(101, result.newRulerId, "男子优先 + 年长先：应选 30 岁长子，而非 40 岁女儿");
            Assert.AreEqual("", result.reason);

            // 真实 RealmData 字段：君主指针换人、heir 清空、执政官不受影响
            Assert.AreEqual(101, _realm.monarchId);
            Assert.AreEqual(-1, _realm.heirId, "继位后应清空旧 heirId");
            Assert.AreEqual(-1, _realm.consulId);
            Assert.AreEqual(101, _realm.GetSupremeRulerId());
            Assert.IsTrue(eldestSon.isAlive, "新君主应仍在世（isAlive=true）");
        }

        // ===================== 测试 2：另一种继承法（幼子继承） =====================

        [Test]
        public void Ultimogeniture_ExecuteSuccession_PicksYoungestSon_DifferentFromPrimogeniture()
        {
            _realm.monarchId = 300;
            // 幼子继承：宗族 + 长支 + 男子优先 + 年幼先（Ultimogeniture 工厂）
            _realm.laws.successionLaw = InheritanceLaw.Ultimogeniture();

            CharacterData ruler = AddCharacter(300, 70, true);
            CharacterData eldestSon = AddCharacter(301, 30, true, fatherId: 300);
            CharacterData youngestSon = AddCharacter(302, 20, true, fatherId: 300);
            CharacterData daughter = AddCharacter(303, 45, false, fatherId: 300);
            CreateDynasty(ruler, eldestSon, youngestSon, daughter);
            Kill(ruler, 600);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 600);

            Assert.IsTrue(result.triggered);
            Assert.IsTrue(result.succeeded);
            Assert.IsFalse(result.disputed, "20 岁幼子已成年，不应争议");
            Assert.AreEqual(302, result.newRulerId, "年幼先：应选 20 岁幼子，而不是 30 岁长子");
            Assert.AreNotEqual(301, result.newRulerId, "幼子继承的结果必须与长子继承不同");
            Assert.AreEqual(302, _realm.monarchId);
            Assert.AreEqual(-1, _realm.heirId);
            Assert.AreEqual(302, _realm.GetSupremeRulerId());
        }

        // ===================== 测试 3：绝嗣 / 无家族候选（修复后语义） =====================

        [Test]
        public void NoHeir_ExecuteSuccession_ReportsDisputedAndClearsHeirId()
        {
            _realm.monarchId = 200;
            _realm.laws.successionLaw = InheritanceLaw.Primogeniture();
            _realm.heirId = 77; // 修复后：绝嗣也要清空旧 heir，避免残留指向

            CharacterData ruler = AddCharacter(200, 70, true);
            CreateDynasty(ruler); // 家族里只有君主本人 → 候选为空
            Kill(ruler, 700);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 700);

            Assert.IsTrue(result.triggered, "死亡君主仍应标记 triggered");
            Assert.IsFalse(result.succeeded, "无候选时不应有继承成功");
            Assert.IsTrue(result.disputed, "绝嗣应标记 disputed");
            Assert.AreEqual("绝嗣", result.reason);
            Assert.AreEqual(200, result.deadRulerId);
            Assert.AreEqual(-1, result.newRulerId);
            Assert.AreEqual(-1, _realm.heirId, "修复后：绝嗣分支应清空 heirId（旧实现残留 77）");

            // 设计决策：不清 monarchId/consulId —— 其它系统按 id>=0 取角色，清掉会变空引用；
            // "无主"由 GameWorld 的继承危机窗口/编年史表达（去重测试见下方 GameWorld 用例）。
            Assert.AreEqual(200, _realm.monarchId, "绝嗣不清 monarchId（设计决策）");
            Assert.AreEqual(200, _realm.GetSupremeRulerId());

            // 系统保持幂等重放（API 层不新增字段标记"已处理"）：ExecuteSuccession 每天仍返回 triggered+disputed；
            // "不再重复触发"由 GameWorld 层实现——已为该死君主记录过危机时直接跳过继承判定（见测试 10）。
            SuccessionResult again = SuccessionSystem.ExecuteSuccession(_realm, _characters, 701);
            Assert.IsTrue(again.triggered, "绝嗣后系统 API 仍可重放（游戏循环层由 GameWorld 去重）");
            Assert.IsTrue(again.disputed);
            Assert.AreEqual(200, again.deadRulerId);
            Assert.AreEqual(-1, _realm.heirId);

            // 变体：君主没有家族（familyId<0）→ CollectCandidates 直接返回空，同样走绝嗣分支。
            CharacterManager isolatedManager = new CharacterManager();
            RealmData isolatedRealm = CreateMonarchyRealm(31, 230);
            CharacterData orphanRuler = NewCharacter(230, 70, true, 31);
            RegisterCharacter(isolatedManager, orphanRuler);
            Kill(orphanRuler, 700);

            SuccessionResult orphanResult = SuccessionSystem.ExecuteSuccession(isolatedRealm, isolatedManager, 700);
            Assert.IsTrue(orphanResult.triggered);
            Assert.IsFalse(orphanResult.succeeded);
            Assert.IsTrue(orphanResult.disputed);
            Assert.AreEqual("绝嗣", orphanResult.reason);
            Assert.AreEqual(230, orphanResult.deadRulerId);
            Assert.AreEqual(-1, isolatedRealm.heirId);
        }

        // ===================== 测试 4：共和选举（君主旗标关闭） =====================

        [Test]
        public void RepublicElective_ExecuteSuccession_UsesEligibilityAndPrestige()
        {
            _realm.monarchId = -1;
            _realm.consulId = 400;
            // ElectiveDirect + 非 Absolute 的 scope → IsMonarchy=false，走威望选举分支
            _realm.laws.supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect);
            _realm.laws.supremeScope = new ComponentChoice((int)SupremeScope.LegallyBound);
            _realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData ruler = AddCharacter(400, 65, true);
            CharacterData oldestMale = AddCharacter(401, 55, true, fatherId: 400, prestige: 5f);
            CharacterData prestigiousMale = AddCharacter(402, 30, true, fatherId: 400, prestige: 90f);
            CharacterData prestigiousFemale = AddCharacter(403, 40, false, fatherId: 400, prestige: 99f);
            CharacterData middleMale = AddCharacter(404, 20, true, fatherId: 400, prestige: 50f);
            CreateDynasty(ruler, oldestMale, prestigiousMale, prestigiousFemale, middleMale);
            Kill(ruler, 800);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 800);

            Assert.IsTrue(result.triggered);
            Assert.IsTrue(result.succeeded);
            Assert.IsFalse(result.disputed);
            Assert.AreEqual(400, result.deadRulerId);
            Assert.AreEqual(402, result.newRulerId,
                "资格过滤（男子专属）先剔除威望最高的女性 403，再按威望选 402");
            Assert.AreNotEqual(401, result.newRulerId, "共和选举结果不应等于最长者（长子继承语义）");

            // 君主路径与共和路径写不同字段
            Assert.AreEqual(-1, _realm.monarchId, "共和制不写 monarchId");
            Assert.AreEqual(402, _realm.consulId);
            Assert.AreEqual(402, _realm.GetSupremeRulerId());
            Assert.AreEqual(-1, _realm.heirId);
        }

        // ===================== 测试 5：幼主争议但仍继位 =====================

        [Test]
        public void MinorHeir_ExecuteSuccession_FlagsDisputedButStillSucceeds()
        {
            _realm.monarchId = 500;
            _realm.laws.successionLaw = InheritanceLaw.Primogeniture();

            CharacterData ruler = AddCharacter(500, 40, true);
            CharacterData olderMinor = AddCharacter(501, 10, true, fatherId: 500);
            CharacterData youngerMinor = AddCharacter(502, 6, true, fatherId: 500);
            CreateDynasty(ruler, olderMinor, youngerMinor);
            Kill(ruler, 900);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 900);

            Assert.IsTrue(result.triggered);
            Assert.IsTrue(result.succeeded, "幼主仍应完成继位（succeeded=true）");
            Assert.IsTrue(result.disputed, "年龄<16 应标记 disputed");
            Assert.AreEqual("幼主", result.reason);
            Assert.AreEqual(501, result.newRulerId);
            Assert.AreEqual(501, _realm.monarchId);
            Assert.AreEqual(-1, _realm.heirId);
        }

        // ===================== 测试 6：继承线 + 大圣战受益人 =====================

        [Test]
        public void InheritanceLine_AndGreatHolyWarBeneficiaries_UseLawOrder()
        {
            _realm.monarchId = 600;
            InheritanceLaw law = InheritanceLaw.Primogeniture();

            CharacterData ruler = AddCharacter(600, 60, true);
            CharacterData first = AddCharacter(601, 50, true, fatherId: 600);
            CharacterData second = AddCharacter(602, 40, true, fatherId: 600);
            CharacterData third = AddCharacter(603, 30, true, fatherId: 600);
            CharacterData female = AddCharacter(604, 20, false, fatherId: 600);
            CreateDynasty(ruler, first, second, third, female);

            // 排序（男子优先→年长先）= 600,601,602,603,604；默认资格（FreePeople）不额外剔除任何候选
            Assert.IsTrue(SuccessionSystem.IsInInheritanceLine(_realm, _characters, law, 601, 2),
                "601 是继承序列第 2 位：divisible=2 时在继承线上");
            Assert.IsFalse(SuccessionSystem.IsInInheritanceLine(_realm, _characters, law, 602, 2),
                "602 是第 3 位：divisible=2 时不在继承线上");
            Assert.IsFalse(SuccessionSystem.IsInInheritanceLine(_realm, _characters, law, 604, 4),
                "女性排最后：divisible=4 时仍不在继承线上");
            Assert.IsFalse(SuccessionSystem.IsInInheritanceLine(_realm, _characters, law, 9999, 2),
                "未知角色 id 不在继承线");

            List<int> allButTop2 = SuccessionSystem.GetGreatHolyWarBeneficiaryCandidates(
                _realm, _characters, law, 2);
            CollectionAssert.AreEquivalent(new[] { 602, 603, 604 }, allButTop2,
                "divisible=2 时，继承线（600/601）之外的角色才是大圣战受益人候选");

            List<int> allButTop4 = SuccessionSystem.GetGreatHolyWarBeneficiaryCandidates(
                _realm, _characters, law, 4);
            CollectionAssert.AreEquivalent(new[] { 604 }, allButTop4,
                "divisible=4 时只剩 604 在继承线之外");
        }

        // ===================== 测试 7：在世君主 / 空参早退 =====================

        [Test]
        public void AliveRuler_AndNullInputs_DoNotTrigger()
        {
            _realm.monarchId = 700;
            _realm.laws.successionLaw = InheritanceLaw.Primogeniture();
            CharacterData ruler = AddCharacter(700, 50, true);
            CreateDynasty(ruler);
            Assert.IsTrue(ruler.isAlive);

            SuccessionResult aliveResult = SuccessionSystem.ExecuteSuccession(_realm, _characters, 1000);
            Assert.IsFalse(aliveResult.triggered, "君主在世时不应触发继承");
            Assert.IsFalse(aliveResult.succeeded);
            Assert.AreEqual(-1, aliveResult.newRulerId);
            Assert.AreEqual(-1, aliveResult.deadRulerId);
            Assert.AreEqual(700, _realm.monarchId);

            SuccessionResult nullRealm = SuccessionSystem.ExecuteSuccession(null, _characters, 1000);
            Assert.IsFalse(nullRealm.triggered);

            SuccessionResult nullManager = SuccessionSystem.ExecuteSuccession(_realm, null, 1000);
            Assert.IsFalse(nullManager.triggered);
        }

        // ===================== 测试 8：P1-2 资格过滤（世袭路径） =====================

        [Test]
        public void Monarchy_EligibilityFilter_ExcludesIneligibleCandidates()
        {
            _realm = CreateMonarchyRealm(3, 800);
            // 继承法 gender=Equal：若只看继承法，35 岁长女应即位；
            // 但最高权力资格 MaleOnly 横切后必须选 20 岁儿子。
            _realm.laws.successionLaw = new InheritanceLaw(
                InheritanceScope.CognaticKin, InheritanceBranch.EldestLine,
                InheritanceGender.Equal, InheritanceAge.Seniority);
            _realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData ruler = AddCharacter(800, 55, true);
            CharacterData elderDaughter = AddCharacter(801, 35, false, fatherId: 800);
            CharacterData youngerSon = AddCharacter(802, 20, true, fatherId: 800);
            CreateDynasty(ruler, elderDaughter, youngerSon);
            Kill(ruler, 1100);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 1100);

            Assert.IsTrue(result.triggered);
            Assert.IsTrue(result.succeeded);
            Assert.IsFalse(result.disputed, "20 岁儿子已成年，不应争议");
            Assert.AreEqual(802, result.newRulerId,
                "最高权力资格 MaleOnly 必须把女性候选剔除（旧实现会选 35 岁长女）");
            Assert.AreEqual(802, _realm.monarchId);
            Assert.AreEqual(-1, _realm.heirId);

            // 场景 2：唯一候选是女性 → 资格过滤后候选为空 → 绝嗣（旧实现会直接立女性）
            CharacterManager isolatedManager = new CharacterManager();
            RealmData isolatedRealm = CreateMonarchyRealm(32, 850);
            isolatedRealm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };
            CharacterData isolatedRuler = NewCharacter(850, 60, true, 32);
            CharacterData onlyDaughter = NewCharacter(851, 30, false, 32, fatherId: 850);
            RegisterCharacter(isolatedManager, isolatedRuler);
            RegisterCharacter(isolatedManager, onlyDaughter);
            CreateFamilyFor(isolatedManager, 32, isolatedRuler, onlyDaughter);
            Kill(isolatedRuler, 1200);

            SuccessionResult noEligible = SuccessionSystem.ExecuteSuccession(isolatedRealm, isolatedManager, 1200);
            Assert.IsTrue(noEligible.triggered);
            Assert.IsFalse(noEligible.succeeded, "唯一候选女性被资格剔除后不应有继承成功");
            Assert.IsTrue(noEligible.disputed);
            Assert.AreEqual("绝嗣", noEligible.reason);
            Assert.AreEqual(-1, noEligible.newRulerId);
            Assert.AreEqual(-1, isolatedRealm.heirId);
        }

        // ===================== 测试 9：P2-4 阶级范围资格（共和路径） =====================

        [Test]
        public void Republic_ScopeEligibility_FiltersBySocialClass()
        {
            _realm = CreateRepublicRealm(RealmId, 860);
            _realm.laws.supremeEligibility = new EligibilityRules
            {
                gender = InheritanceGender.Equal,
                scope = EligibilityScope.Nobility
            };

            CharacterData ruler = AddCharacter(860, 60, true);
            CharacterData richCommoner = AddCharacter(861, 30, true, fatherId: 860, prestige: 90f,
                socialClassValue: GameEnums.SocialClass.Peasant);
            CharacterData noble = AddCharacter(862, 50, true, fatherId: 860, prestige: 50f,
                socialClassValue: GameEnums.SocialClass.Royalty);
            CreateDynasty(ruler, richCommoner, noble);
            Kill(ruler, 1300);

            SuccessionResult result = SuccessionSystem.ExecuteSuccession(_realm, _characters, 1300);

            Assert.IsTrue(result.triggered);
            Assert.IsTrue(result.succeeded);
            Assert.AreEqual(862, result.newRulerId,
                "威望更高的平民 861 应被 Nobility 范围资格排除，选贵族 862");
            Assert.AreEqual(862, _realm.consulId);
            Assert.AreEqual(-1, _realm.monarchId);

            // 场景 2：候选全为平民 → 范围资格过滤后无候选 → 绝嗣
            CharacterManager isolatedManager = new CharacterManager();
            RealmData isolatedRealm = CreateRepublicRealm(33, 880);
            isolatedRealm.laws.supremeEligibility = new EligibilityRules
            {
                gender = InheritanceGender.Equal,
                scope = EligibilityScope.Nobility
            };
            CharacterData isolatedRuler = NewCharacter(880, 55, true, 33);
            CharacterData peasant = NewCharacter(881, 30, true, 33, fatherId: 880);
            RegisterCharacter(isolatedManager, isolatedRuler);
            RegisterCharacter(isolatedManager, peasant);
            CreateFamilyFor(isolatedManager, 33, isolatedRuler, peasant);
            Kill(isolatedRuler, 1400);

            SuccessionResult noEligible = SuccessionSystem.ExecuteSuccession(isolatedRealm, isolatedManager, 1400);
            Assert.IsTrue(noEligible.disputed);
            Assert.IsFalse(noEligible.succeeded);
            Assert.AreEqual("绝嗣", noEligible.reason);
            Assert.AreEqual(880, isolatedRealm.consulId, "设计决策：绝嗣不清 consulId");
        }

        // ===================== 测试 10：P1-1 GameWorld 编年史按死君主去重 =====================

        [Test]
        public void NoHeir_GameWorld_RecordsCrisisOncePerDeadRuler()
        {
            GameObject worldObject = new GameObject("SuccessionTestWorld_NoHeir");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                CharacterManager manager = new CharacterManager();
                RealmData realm = CreateMonarchyRealm(9, 900);
                world.realms[realm.realmId] = realm;

                CharacterData ruler = NewCharacter(900, 70, true, 9);
                ruler.epithetEvaluated = true; // 跳过死亡评估链路（依赖地块/成就，与本测试无关）
                RegisterCharacter(manager, ruler);
                CreateFamilyFor(manager, 9, ruler);
                Kill(ruler, 100);

                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 100);

                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession_crisis"),
                    "首次判定绝嗣应记录一条继承危机");
                int totalAfterFirst = chronicle.Count;

                world.currentDay = 101;
                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession_crisis"),
                    "同一死君主的继承危机不能每天重复记录");
                Assert.AreEqual(totalAfterFirst, chronicle.Count, "第二个 tick 不应新增任何编年史");
                Assert.AreEqual(-1, realm.heirId, "绝嗣应清空 heirId");
                Assert.AreEqual(900, realm.monarchId, "设计决策：不清 monarchId");

                ChronicleEntry crisis = chronicle.GetEntriesByType("succession_crisis")[0];
                CollectionAssert.Contains(crisis.participants, 900,
                    "危机记录 participants 应包含死君主 id（去重依据）");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(worldObject);
            }
        }

        // ===================== 测试 11：P2-5 GameWorld 幼主双记录（先即位再危机） =====================

        [Test]
        public void MinorHeir_GameWorld_WritesAccessionBeforeCrisis()
        {
            GameObject worldObject = new GameObject("SuccessionTestWorld_Minor");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                CharacterManager manager = new CharacterManager();
                RealmData realm = CreateMonarchyRealm(10, 1000);
                world.realms[realm.realmId] = realm;

                CharacterData ruler = NewCharacter(1000, 45, true, 10);
                ruler.epithetEvaluated = true;
                CharacterData minor = NewCharacter(1001, 10, true, 10, fatherId: 1000);
                CharacterData younger = NewCharacter(1002, 6, true, 10, fatherId: 1000);
                RegisterCharacter(manager, ruler);
                RegisterCharacter(manager, minor);
                RegisterCharacter(manager, younger);
                CreateFamilyFor(manager, 10, ruler, minor, younger);
                Kill(ruler, 200);

                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 200);
                InvokePrivate(world, "CheckRulerSuccessions");

                List<ChronicleEntry> entries = chronicle.GetEntries();
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession"), "幼主也应有即位记录");
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession_crisis"), "幼主应有继承危机记录");
                int accessionIndex = FindChronicleEntryIndex(entries, "succession");
                int crisisIndex = FindChronicleEntryIndex(entries, "succession_crisis");
                Assert.GreaterOrEqual(accessionIndex, 0);
                Assert.GreaterOrEqual(crisisIndex, 0);
                Assert.Less(accessionIndex, crisisIndex, "记录顺序：先写新君即位、再写继承危机");
                StringAssert.Contains("幼主", entries[crisisIndex].description);

                Assert.AreEqual(1001, realm.monarchId);
                Assert.AreEqual(200, minor.accessionDay, "幼主继位应设置 accessionDay");
                Assert.IsTrue(minor.achievements.youngAccession, "幼主应标记 youngAccession");
                Assert.IsTrue(minor.achievements.ruledUnderRegency, "争议幼主应标记 ruledUnderRegency");
                Assert.AreEqual(-1, realm.heirId);

                int totalAfterFirst = chronicle.Count;
                world.currentDay = 201;
                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.AreEqual(totalAfterFirst, chronicle.Count,
                    "新君在世后不应每天重复写即位/危机记录");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(worldObject);
            }
        }

        // ===================== 夹具脚手架 =====================

        private static RealmData CreateMonarchyRealm(int realmId, int monarchId)
        {
            var realm = new RealmData
            {
                realmId = realmId,
                realmName = "测试王朝" + realmId,
                // 关键：关闭"继承法取自文化"，否则 GetEffectiveSuccessionLaw 会查 ContentRegistry；
                // 关闭后固定走 laws.successionLaw，测试不依赖内容表初始化。
                successionLawFromCulture = false,
                monarchId = monarchId,
                consulId = -1,
                heirId = -1
            };
            realm.laws.supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary);
            realm.laws.successionLaw = InheritanceLaw.Primogeniture();
            return realm;
        }

        private static RealmData CreateRepublicRealm(int realmId, int consulId)
        {
            var realm = new RealmData
            {
                realmId = realmId,
                realmName = "测试共和国" + realmId,
                successionLawFromCulture = false,
                monarchId = -1,
                consulId = consulId,
                heirId = -1
            };
            realm.laws.supremeSuccession = new ComponentChoice((int)SupremeSuccession.ElectiveDirect);
            realm.laws.supremeScope = new ComponentChoice((int)SupremeScope.LegallyBound);
            return realm;
        }

        /// <summary>手工构造角色并注册进当前 CharacterManager 的私有角色字典（测试专用）。</summary>
        private CharacterData AddCharacter(int characterId, int age, bool isMale,
            int fatherId = -1, int motherId = -1, float prestige = 0f,
            GameEnums.SocialClass socialClassValue = GameEnums.SocialClass.Peasant)
        {
            CharacterData character = NewCharacter(characterId, age, isMale, RealmId,
                fatherId, motherId, prestige, socialClassValue);
            RegisterCharacter(_characters, character);
            return character;
        }

        private static CharacterData NewCharacter(int characterId, int age, bool isMale, int realmId,
            int fatherId = -1, int motherId = -1, float prestige = 0f,
            GameEnums.SocialClass socialClassValue = GameEnums.SocialClass.Peasant)
        {
            return new CharacterData
            {
                characterId = characterId,
                firstName = "测",
                lastName = "试" + characterId,
                age = age,
                isMale = isMale,
                realmId = realmId,
                fatherId = fatherId,
                motherId = motherId,
                prestige = prestige,
                socialClass = socialClassValue
            };
        }

        /// <summary>把已有 CharacterData 写进 CharacterManager._characters（无公开注册既有角色 API）。</summary>
        private static void RegisterCharacter(CharacterManager manager, CharacterData character)
        {
            FieldInfo charactersField = typeof(CharacterManager).GetField(
                "_characters", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(charactersField, "测试脚手架：CharacterManager._characters 字段应存在");
            var characters = charactersField.GetValue(manager) as Dictionary<int, CharacterData>;
            Assert.IsNotNull(characters, "测试脚手架：CharacterManager._characters 应为角色字典");
            characters[character.characterId] = character;
        }

        /// <summary>
        /// 建立家族（公开 CreateFamily 注册并回填 founder 的 familyId），
        /// 再把其余成员加入 memberIds 并统一 familyId（CollectCandidates 依赖家族成员表）。
        /// </summary>
        private FamilyNode CreateDynasty(CharacterData ruler, params CharacterData[] members)
        {
            return CreateFamilyFor(_characters, RealmId, ruler, members);
        }

        private static FamilyNode CreateFamilyFor(CharacterManager manager, int realmId, CharacterData ruler,
            params CharacterData[] members)
        {
            FamilyNode family = manager.CreateFamily("测试家族", ruler.characterId, 0, realmId);
            Assert.IsNotNull(family, "CreateFamily 应返回家族节点");
            Assert.AreEqual(family.familyId, ruler.familyId, "CreateFamily 应回填 founder 的 familyId");
            foreach (CharacterData member in members)
            {
                member.familyId = family.familyId;
                family.AddMember(member.characterId);
            }
            return family;
        }

        private static void Kill(CharacterData character, int day)
        {
            character.deathDay = day;
            character.deathYear = 1;
            Assert.IsFalse(character.isAlive, "死亡标记后 isAlive 应为 false");
        }

        // ===================== GameWorld 私有状态注入/调用（仅测试用） =====================

        private static void AttachWorldState(GameWorld world, CharacterManager manager, Chronicle chronicle, int currentDay)
        {
            world.currentDay = currentDay;
            SetPrivateField(world, "_characterManager", manager);
            SetPrivateField(world, "_chronicle", chronicle);
            // _regimeDynamics 保持 null：NotifyRulerTransition 首行即早退，避免依赖政权博弈系统。
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "测试脚手架：字段 " + fieldName + " 应存在");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "测试脚手架：私有方法 " + methodName + " 应存在");
            method.Invoke(target, null);
        }

        private static int CountChronicleEntries(Chronicle chronicle, string eventType)
        {
            int count = 0;
            foreach (ChronicleEntry entry in chronicle.GetEntries())
            {
                if (entry.eventType == eventType) count++;
            }
            return count;
        }

        private static int FindChronicleEntryIndex(List<ChronicleEntry> entries, string eventType)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].eventType == eventType) return i;
            }
            return -1;
        }
    }
}
