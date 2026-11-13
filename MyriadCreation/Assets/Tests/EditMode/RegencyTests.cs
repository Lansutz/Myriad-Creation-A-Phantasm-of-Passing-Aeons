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
    /// 摄政/空位期（绝嗣兜底）测试：绝嗣进入摄政、摄政期 GetSupremeRulerId 指向摄政者、
    /// 合法继承人出现后加冕并结束摄政、无合格摄政者空转不崩不刷屏、摄政者死亡后重选、
    /// 以及 RealmDTO 存档往返保留 interregnum/regent 状态。
    /// 夹具：真实 RealmData + CharacterManager（无公开注册既有角色 API → 反射注入私有 _characters）；
    /// GameWorld 级用例用 new GameObject + AddComponent&lt;GameWorld&gt;（Awake 为空实现），
    /// 反射注入 _characterManager/_chronicle 并调用私有 CheckRulerSuccessions；_regimeDynamics 留 null。
    /// </summary>
    public class RegencyTests
    {
        [SetUp]
        public void SetUp()
        {
            CharacterKnowledgeRegistry.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // 本类不创建持久 GameObject/世界（GameWorld 用例在 finally 里 DestroyImmediate）；
            // 防御性清理角色知识静态表。
            CharacterKnowledgeRegistry.Clear();
        }

        // ===================== 1. 绝嗣进入摄政并选出同家族摄政者 =====================

        [Test]
        public void NoHeir_EntersInterregnum_SelectsFamilyRegent()
        {
            CharacterManager manager = new CharacterManager();
            RealmData realm = CreateMonarchyRealm(9, 900);
            // 男子专属资格：成年女儿不能继承 → 绝嗣；但她可以摄政。
            realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData deadRuler = NewCharacter(900, 70, true, 9);
            deadRuler.epithetEvaluated = true;
            CharacterData familyAdultFemale = NewCharacter(901, 30, false, 9);
            RegisterCharacter(manager, deadRuler);
            RegisterCharacter(manager, familyAdultFemale);
            FamilyNode family = CreateFamilyFor(manager, 9, deadRuler, familyAdultFemale);
            Assert.IsNotNull(family);
            Kill(deadRuler, 100);

            GameObject worldObject = new GameObject("RegencyTestWorld_Enter");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                world.realms[realm.realmId] = realm;
                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 100);

                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.IsTrue(realm.inInterregnum, "无合法继承人 → 进入摄政/空位期");
                Assert.IsTrue(realm.IsInterregnum);
                Assert.AreEqual(901, realm.regentCharacterId, "应推举同家族成年成员 901 摄政");
                Assert.AreEqual(100, realm.interregnumStartDay);
                Assert.AreEqual(0, realm.interregnumMonths);
                Assert.AreEqual(900, realm.monarchId, "摄政期保留死君主指针（兼容既有调用）");
                Assert.AreEqual(900, realm.GetLegalRulerId(), "法理统治者仍是死君主 900");
                Assert.AreEqual(901, realm.GetSupremeRulerId(), "摄政期 GetSupremeRulerId 返回摄政者");
                Assert.AreEqual(901, realm.GetEffectiveRulerId(), "GetEffectiveRulerId 返回实际掌权的摄政者");

                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession_crisis"), "绝嗣危机记录一次");
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "regency_enter"), "进入摄政记录一次");
                Assert.AreEqual(0, CountChronicleEntries(chronicle, "succession"), "未加冕，不应有新君即位记录");
                ChronicleEntry enter = chronicle.GetEntriesByType("regency_enter")[0];
                StringAssert.Contains("摄政", enter.description);
                StringAssert.Contains("901", enter.description);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(worldObject);
            }
        }

        // ===================== 2. 摄政期出现合法继承人 → 加冕并结束摄政 =====================

        [Test]
        public void Interregnum_HeirAppears_CrownsAndEndsRegency()
        {
            CharacterManager manager = new CharacterManager();
            RealmData realm = CreateMonarchyRealm(10, 1000);
            realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData deadRuler = NewCharacter(1000, 70, true, 10);
            deadRuler.epithetEvaluated = true;
            CharacterData regentCandidate = NewCharacter(1001, 30, false, 10);
            RegisterCharacter(manager, deadRuler);
            RegisterCharacter(manager, regentCandidate);
            FamilyNode family = CreateFamilyFor(manager, 10, deadRuler, regentCandidate);
            Assert.IsNotNull(family);
            Kill(deadRuler, 200);

            GameObject worldObject = new GameObject("RegencyTestWorld_Crown");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                world.realms[realm.realmId] = realm;
                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 200);

                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.IsTrue(realm.inInterregnum);
                Assert.AreEqual(1001, realm.GetSupremeRulerId());

                // 摄政期间出现合法继承人（成年男性加入家族）
                CharacterData heir = NewCharacter(1003, 25, true, 10, fatherId: 1000);
                RegisterCharacter(manager, heir);
                heir.familyId = family.familyId;
                family.AddMember(heir.characterId);

                world.currentDay = 201;
                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.IsFalse(realm.inInterregnum, "合法继承人出现 → 摄政期结束");
                Assert.AreEqual(-1, realm.regentCharacterId, "加冕后清空摄政者");
                Assert.AreEqual(1003, realm.monarchId, "继承人 1003 加冕");
                Assert.AreEqual(1003, realm.GetSupremeRulerId());
                Assert.AreEqual(-1, realm.heirId);
                Assert.AreEqual(0, realm.interregnumStartDay, "摄政期字段应复位");
                Assert.AreEqual(0, realm.interregnumMonths, "摄政期字段应复位");
                Assert.AreEqual(201, heir.accessionDay, "加冕应记录 accessionDay");
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession"), "应写一条新君即位记录");
                ChronicleEntry accession = chronicle.GetEntriesByType("succession")[0];
                StringAssert.Contains("新君即位", accession.description);

                int totalAfterCrown = chronicle.Count;
                world.currentDay = 202;
                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.AreEqual(totalAfterCrown, chronicle.Count, "加冕后新君在世，不应重复写记录");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(worldObject);
            }
        }

        // ===================== 3. 无合格摄政者 → 空转不崩、不重复写记录 =====================

        [Test]
        public void Interregnum_NoQualifiedRegent_StallsWithoutChronicleSpam()
        {
            CharacterManager manager = new CharacterManager();
            RealmData realm = CreateMonarchyRealm(11, 1100);
            realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData deadRuler = NewCharacter(1100, 70, true, 11);
            deadRuler.epithetEvaluated = true;
            RegisterCharacter(manager, deadRuler);
            // 不给家族、不给其它角色 → 绝嗣且无合格摄政者
            Kill(deadRuler, 300);

            GameObject worldObject = new GameObject("RegencyTestWorld_Vacant");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                world.realms[realm.realmId] = realm;
                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 300);

                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.IsTrue(realm.inInterregnum, "无继承人 → 仍进入摄政/空位期");
                Assert.AreEqual(-1, realm.regentCharacterId, "无合格摄政者 → regent=-1（王国空转）");
                Assert.AreEqual(1100, realm.GetSupremeRulerId(), "无摄政者时回退死君主（兼容既有调用）");
                Assert.AreEqual(1100, realm.GetEffectiveRulerId());
                Assert.AreEqual(1100, realm.GetLegalRulerId());
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "regency_enter"), "进入摄政记录一次（含空转）");
                StringAssert.Contains("暂无合格摄政者", chronicle.GetEntriesByType("regency_enter")[0].description);

                int totalAfterFirst = chronicle.Count;
                world.currentDay = 301;
                InvokePrivate(world, "CheckRulerSuccessions");
                world.currentDay = 302;
                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.AreEqual(totalAfterFirst, chronicle.Count, "空转期间不应重复写编年史/危机");
                Assert.AreEqual(-1, realm.regentCharacterId);
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "succession_crisis"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(worldObject);
            }
        }

        // ===================== 4. 摄政者死亡/失格 → 重选 =====================

        [Test]
        public void Interregnum_RegentDies_Reelected()
        {
            CharacterManager manager = new CharacterManager();
            RealmData realm = CreateMonarchyRealm(12, 1200);
            realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData deadRuler = NewCharacter(1200, 70, true, 12);
            deadRuler.epithetEvaluated = true;
            CharacterData firstRegent = NewCharacter(1201, 30, false, 12);
            firstRegent.prestige = 150f;
            CharacterData secondRegent = NewCharacter(1202, 35, false, 12);
            secondRegent.prestige = 10f;
            RegisterCharacter(manager, deadRuler);
            RegisterCharacter(manager, firstRegent);
            RegisterCharacter(manager, secondRegent);
            CreateFamilyFor(manager, 12, deadRuler, firstRegent, secondRegent);
            Kill(deadRuler, 400);

            GameObject worldObject = new GameObject("RegencyTestWorld_Reelect");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                world.realms[realm.realmId] = realm;
                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 400);

                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.IsTrue(realm.inInterregnum);
                Assert.AreEqual(1201, realm.regentCharacterId, "威望等级高者优先出任摄政");

                Kill(firstRegent, 400);
                world.currentDay = 401;
                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.IsTrue(realm.inInterregnum, "摄政者死亡不应结束摄政期");
                Assert.AreEqual(1202, realm.regentCharacterId, "摄政者死亡后应重选 1202");
                Assert.AreEqual(1202, realm.GetSupremeRulerId());
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "regency"), "摄政者变更写一条记录");
                StringAssert.Contains("1202", chronicle.GetEntriesByType("regency")[0].description);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(worldObject);
            }
        }

        // ===================== 5. 摄政期 GetSupremeRulerId / 进入 API =====================

        [Test]
        public void Interregnum_GetSupremeRulerId_ReturnsRegent_AndEnterApiIsIdempotent()
        {
            CharacterManager manager = new CharacterManager();
            RealmData realm = CreateMonarchyRealm(13, 1300);
            realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData deadRuler = NewCharacter(1300, 70, true, 13);
            CharacterData adultFemale = NewCharacter(1301, 30, false, 13);
            RegisterCharacter(manager, deadRuler);
            RegisterCharacter(manager, adultFemale);
            CreateFamilyFor(manager, 13, deadRuler, adultFemale);
            Kill(deadRuler, 500);

            SuccessionResult succession = SuccessionSystem.ExecuteSuccession(realm, manager, 500);
            Assert.IsTrue(succession.disputed);
            Assert.IsFalse(succession.succeeded, "资格过滤后无继承人 → 绝嗣");

            bool entered = SuccessionSystem.TryEnterInterregnum(realm, manager, 500, out int regentId);
            Assert.IsTrue(entered);
            Assert.AreEqual(1301, regentId);
            Assert.IsTrue(realm.IsInterregnum);
            Assert.AreEqual(1301, realm.GetSupremeRulerId(), "摄政期最高统治者 = 摄政者");
            Assert.AreEqual(1301, realm.GetEffectiveRulerId());
            Assert.AreEqual(1300, realm.GetLegalRulerId(), "法理统治者仍是死君主");
            Assert.AreEqual(500, realm.interregnumStartDay);

            bool enteredAgain = SuccessionSystem.TryEnterInterregnum(realm, manager, 501, out int regentAgain);
            Assert.IsFalse(enteredAgain, "已在摄政期时重复进入应返回 false");
            Assert.AreEqual(-1, regentAgain, "重复进入不应改写摄政者（out 保持默认）");
            Assert.AreEqual(1301, realm.regentCharacterId);

            // 外部指定合法继承人（heirId>=0 且在世）→ TickInterregnum 直接加冕并结束摄政
            CharacterData designatedHeir = NewCharacter(1302, 40, true, 13);
            RegisterCharacter(manager, designatedHeir);
            realm.heirId = 1302;
            SuccessionSystem.InterregnumTickResult crownedTick = SuccessionSystem.TickInterregnum(realm, manager, 510);
            Assert.AreEqual(1302, crownedTick.crownedRulerId, "heirId>=0 的合法继承人应直接被加冕");
            Assert.IsFalse(realm.inInterregnum);
            Assert.AreEqual(1302, realm.monarchId);
            Assert.AreEqual(-1, realm.regentCharacterId);
            Assert.AreEqual(-1, realm.heirId);

            // 无继承人的政权：摄政者 -1，GetSupremeRulerId 回退死君主（兼容）
            CharacterManager isolatedManager = new CharacterManager();
            RealmData isolatedRealm = CreateMonarchyRealm(31, 1310);
            CharacterData orphanRuler = NewCharacter(1310, 70, true, 31);
            RegisterCharacter(isolatedManager, orphanRuler);
            Kill(orphanRuler, 500);
            SuccessionResult isolatedResult = SuccessionSystem.ExecuteSuccession(isolatedRealm, isolatedManager, 500);
            Assert.IsTrue(isolatedResult.disputed);
            bool isolatedEntered = SuccessionSystem.TryEnterInterregnum(isolatedRealm, isolatedManager, 500, out int noRegentId);
            Assert.IsTrue(isolatedEntered);
            Assert.AreEqual(-1, noRegentId);
            Assert.AreEqual(-1, isolatedRealm.regentCharacterId);
            Assert.AreEqual(1310, isolatedRealm.GetSupremeRulerId(), "无摄政者时回退死君主（兼容既有调用）");
            Assert.AreEqual(1310, isolatedRealm.GetEffectiveRulerId());
        }

        // ===================== 6. RealmDTO 存档往返保留摄政状态 =====================

        [Test]
        public void RealmDTO_RoundTrip_PreservesInterregnumState()
        {
            RealmData realm = CreateMonarchyRealm(14, -1);
            realm.inInterregnum = true;
            realm.regentCharacterId = 77;
            realm.interregnumStartDay = 123;
            realm.interregnumMonths = 4;

            RealmDTO dto = RealmDTO.FromRealmData(realm);
            Assert.IsTrue(dto.inInterregnum);
            Assert.AreEqual(77, dto.regentCharacterId);
            Assert.AreEqual(123, dto.interregnumStartDay);
            Assert.AreEqual(4, dto.interregnumMonths);

            string json = JsonUtility.ToJson(dto);
            RealmDTO reloadedDto = JsonUtility.FromJson<RealmDTO>(json);
            Assert.IsNotNull(reloadedDto, "RealmDTO JSON 反序列化不应失败");

            RealmData restored = reloadedDto.ToRealmData();
            Assert.IsTrue(restored.inInterregnum, "存档往返应保留摄政期标记");
            Assert.AreEqual(77, restored.regentCharacterId, "存档往返应保留摄政者 id");
            Assert.AreEqual(123, restored.interregnumStartDay);
            Assert.AreEqual(4, restored.interregnumMonths);
            Assert.IsTrue(restored.IsInterregnum);
            Assert.AreEqual(77, restored.GetSupremeRulerId(), "读档后摄政者仍是最高统治者");

            // 旧档兼容：缺失新字段 → 默认不在摄政、regent=-1
            RealmData legacy = new RealmDTO { realmId = 99 }.ToRealmData();
            Assert.IsFalse(legacy.inInterregnum);
            Assert.AreEqual(-1, legacy.regentCharacterId);
            Assert.AreEqual(0, legacy.interregnumStartDay);
            Assert.AreEqual(0, legacy.interregnumMonths);
        }

        // ===================== 7. 外部产生新君（读档/事件加冕）→ 结束摄政期 =====================

        [Test]
        public void Interregnum_ExternalRulerAppears_EndsRegencyAndChronicles()
        {
            CharacterManager manager = new CharacterManager();
            RealmData realm = CreateMonarchyRealm(15, 1500);
            realm.laws.supremeEligibility = new EligibilityRules { gender = InheritanceGender.MaleOnly };

            CharacterData deadRuler = NewCharacter(1500, 70, true, 15);
            deadRuler.epithetEvaluated = true;
            CharacterData regentCandidate = NewCharacter(1501, 30, false, 15);
            RegisterCharacter(manager, deadRuler);
            RegisterCharacter(manager, regentCandidate);
            CreateFamilyFor(manager, 15, deadRuler, regentCandidate);
            Kill(deadRuler, 600);

            GameObject worldObject = new GameObject("RegencyTestWorld_External");
            try
            {
                GameWorld world = worldObject.AddComponent<GameWorld>();
                world.realms[realm.realmId] = realm;
                Chronicle chronicle = new Chronicle();
                AttachWorldState(world, manager, chronicle, 600);

                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.IsTrue(realm.inInterregnum);
                Assert.AreEqual(1501, realm.regentCharacterId);

                // 外部系统直接产生新君（例如读档/事件加冕）→ 摄政期应在下一 tick 结束
                CharacterData externalRuler = NewCharacter(1502, 40, true, 15);
                RegisterCharacter(manager, externalRuler);
                realm.monarchId = 1502;
                world.currentDay = 601;
                InvokePrivate(world, "CheckRulerSuccessions");

                Assert.IsFalse(realm.inInterregnum, "外部已有在世君主 → 摄政期结束");
                Assert.AreEqual(-1, realm.regentCharacterId);
                Assert.AreEqual(1502, realm.GetSupremeRulerId());
                Assert.AreEqual(1502, realm.GetLegalRulerId());
                Assert.AreEqual(1, CountChronicleEntries(chronicle, "regency_end"), "结束摄政写一条记录");

                int totalAfterEnd = chronicle.Count;
                world.currentDay = 602;
                InvokePrivate(world, "CheckRulerSuccessions");
                Assert.AreEqual(totalAfterEnd, chronicle.Count, "新君在世后不应重复写摄政结束记录");
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
                // 关闭文化继承法（否则 GetEffectiveSuccessionLaw 会查 ContentRegistry）
                successionLawFromCulture = false,
                monarchId = monarchId,
                consulId = -1,
                heirId = -1
            };
            realm.laws.supremeSuccession = new ComponentChoice((int)SupremeSuccession.Hereditary);
            realm.laws.successionLaw = InheritanceLaw.Primogeniture();
            return realm;
        }

        private static CharacterData NewCharacter(int characterId, int age, bool isMale, int realmId,
            int fatherId = -1, int motherId = -1)
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
                socialClass = GameEnums.SocialClass.Royalty
            };
        }

        private static void RegisterCharacter(CharacterManager manager, CharacterData character)
        {
            FieldInfo charactersField = typeof(CharacterManager).GetField(
                "_characters", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(charactersField, "测试脚手架：CharacterManager._characters 字段应存在");
            var characters = charactersField.GetValue(manager) as Dictionary<int, CharacterData>;
            Assert.IsNotNull(characters, "测试脚手架：CharacterManager._characters 应为角色字典");
            characters[character.characterId] = character;
        }

        /// <summary>建立家族（公开 CreateFamily 注册并回填 founder 的 familyId），其余成员加入 memberIds。</summary>
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
            Assert.IsFalse(character.isAlive);
        }

        // ===================== GameWorld 私有状态注入（仅测试用） =====================

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
    }
}
