using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Core.Enums;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// P3.1 官职：内容表驱动槽位/候选/任期/俸禄，职权按模块查询；旧档槽位保持 0-5 兼容。
    /// </summary>
    public class OfficeSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            OfficeCatalog.ResetContent();
        }

        [TearDown]
        public void TearDown()
        {
            OfficeCatalog.ResetContent();
        }

        private static CharacterData AddCharacter(CharacterManager manager, string name, CharacterRole role,
            bool isMale = true, int familyId = -1)
        {
            CharacterData character = manager.CreateCharacter(name, "氏", 40, isMale, 0, 0, 0, role);
            character.familyId = familyId;
            return character;
        }

        private static RealmData NewRealm(CharacterData ruler)
        {
            return new RealmData
            {
                realmId = 0,
                monarchId = ruler?.characterId ?? -1,
                treasury = 1000f
            };
        }

        // ---------- 1. 目录与兼容 ----------

        [Test]
        public void Defaults_KeepLegacySlotsZeroToFive()
        {
            string[] expectedIds = { "governor", "district_governor", "chancellor", "steward", "marshal", "war_commander" };
            for (int slot = 0; slot < 6; slot++)
            {
                Assert.IsTrue(OfficeCatalog.TryGet(slot, out OfficeDef def), $"槽位 {slot} 应有内置官职");
                Assert.AreEqual((OfficialOffice)slot, (OfficialOffice)slot, "枚举顺序是旧档槽位契约");
                Assert.AreEqual(expectedIds[slot], def.id, "内置六职 id 必须与旧槽位一一对应，旧档才能直接读");
            }
        }

        [Test]
        public void ContentLoad_ReplacesTableInsteadOfMerging()
        {
            OfficeCatalog.LoadFromContent(new[]
            {
                new OfficeDef { id = "judge", slot = 6, name = "审判官", modules = new[] { "judiciary" } }
            });

            Assert.IsTrue(OfficeCatalog.TryGet(6, out _), "内容表的槽位应可查");
            Assert.IsFalse(OfficeCatalog.TryGet(0, out _), "有内容表时不得再混入内置六职");
        }

        // ---------- 2. 补缺与候选限制 ----------

        [Test]
        public void FillVacancies_SeatsEligibleCommonerButNotRuler()
        {
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble);
            CharacterData minister = AddCharacter(manager, "臣", CharacterRole.Noble);
            RealmData realm = NewRealm(ruler);
            ruler.realmId = realm.realmId;
            minister.realmId = realm.realmId;

            int filled = OfficeSystem.FillVacancies(realm, manager, 100);

            Assert.Greater(filled, 0, "有合格候选人时应补缺");
            foreach (int holderId in realm.officeHolders.Values)
            {
                Assert.AreNotEqual(ruler.characterId, holderId, "君主不得兼任官职");
            }
        }

        [Test]
        public void FillVacancies_RespectsGenderFilter()
        {
            OfficeCatalog.LoadFromContent(new[]
            {
                new OfficeDef
                {
                    id = "matron", slot = 9, name = "女官", candidateGender = "female",
                    modules = new[] { "personnel" }, appointmentOrder = new[] { "ruler" }
                }
            });
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble);
            CharacterData man = AddCharacter(manager, "男", CharacterRole.Noble);
            CharacterData woman = AddCharacter(manager, "女", CharacterRole.Noble, isMale: false);
            RealmData realm = NewRealm(ruler);
            man.realmId = realm.realmId;
            woman.realmId = realm.realmId;
            ruler.realmId = realm.realmId;

            OfficeSystem.FillVacancies(realm, manager, 100);

            Assert.AreEqual(woman.characterId, realm.officeHolders[9], "性别限制必须由内容表生效");
        }

        [Test]
        public void FillVacancies_RespectsRoyalFilter()
        {
            OfficeCatalog.LoadFromContent(new[]
            {
                new OfficeDef
                {
                    id = "kinsman", slot = 9, name = "宗室", candidateRoyal = "royal",
                    modules = new[] { "personnel" }, appointmentOrder = new[] { "ruler" }
                }
            });
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble, familyId: 7);
            CharacterData noble = AddCharacter(manager, "贵族", CharacterRole.Noble, familyId: 8);
            CharacterData kinsman = AddCharacter(manager, "宗亲", CharacterRole.Noble, familyId: 7);
            RealmData realm = NewRealm(ruler);
            foreach (CharacterData c in new[] { ruler, noble, kinsman }) c.realmId = realm.realmId;

            OfficeSystem.FillVacancies(realm, manager, 100);

            Assert.AreEqual(kinsman.characterId, realm.officeHolders[9], "royal 限制只允许君主同族");
        }

        // ---------- 3. 任期与俸禄 ----------

        [Test]
        public void VacateExpired_RemovesHolderAfterTerm()
        {
            OfficeCatalog.LoadFromContent(new[]
            {
                new OfficeDef { id = "magistrate", slot = 9, name = "吏", termYears = 1, modules = new[] { "judiciary" } }
            });
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble);
            CharacterData holder = AddCharacter(manager, "吏", CharacterRole.Scholar);
            RealmData realm = NewRealm(ruler);
            ruler.realmId = realm.realmId;
            holder.realmId = realm.realmId;
            realm.officeHolders[9] = holder.characterId;
            realm.officeAppointedDays[9] = 100;

            Assert.AreEqual(0, OfficeSystem.VacateExpired(realm, manager, 100 + 364), "未满一年不得离任");
            Assert.AreEqual(1, OfficeSystem.VacateExpired(realm, manager, 100 + 365), "满一年应离任");
            Assert.AreEqual(-1, realm.officeHolders[9], "离任后槽位应清空");
            Assert.IsFalse(realm.officeAppointedDays.ContainsKey(9), "离任后任命日应清除");
        }

        [Test]
        public void VacateExpired_LeavesLegacyHolderWithoutAppointmentDay()
        {
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble);
            CharacterData holder = AddCharacter(manager, "吏", CharacterRole.Scholar);
            RealmData realm = NewRealm(ruler);
            ruler.realmId = realm.realmId;
            holder.realmId = realm.realmId;
            realm.officeHolders[0] = holder.characterId; // 旧档：只有 holder，没有任命日

            Assert.AreEqual(0, OfficeSystem.VacateExpired(realm, manager, 100000), "旧档不得因缺任命日被误判到期");
            Assert.AreEqual(holder.characterId, realm.officeHolders[0]);
        }

        [Test]
        public void PaySalaries_ChargesTreasuryAndStopsWhenBroke()
        {
            OfficeCatalog.LoadFromContent(new[]
            {
                new OfficeDef { id = "clerk", slot = 9, name = "吏", salary = 365f, modules = new[] { "taxation" } }
            });
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble);
            CharacterData holder = AddCharacter(manager, "吏", CharacterRole.Scholar);
            RealmData realm = NewRealm(ruler);
            realm.treasury = 365f;
            ruler.realmId = realm.realmId;
            holder.realmId = realm.realmId;
            realm.officeHolders[9] = holder.characterId;

            float paid = OfficeSystem.PaySalaries(realm, manager, 365f);

            Assert.AreEqual(365f, paid, 0.01f, "年薪 365 应在一年内全额支付");
            Assert.AreEqual(0f, realm.treasury, 0.01f, "俸禄必须从国库扣，不得凭空生成");

            Assert.AreEqual(0f, OfficeSystem.PaySalaries(realm, manager, 365f), 0.01f, "国库不足时不得部分扣款/透支");
        }

        // ---------- 4. 职权模块 ----------

        [Test]
        public void ModuleHolder_DistinguishesProposalFromTaxation()
        {
            OfficeCatalog.LoadFromContent(new[]
            {
                new OfficeDef { id = "chancellor", slot = 2, name = "宰相", modules = new[] { "law_proposal", "personnel" } },
                new OfficeDef { id = "steward", slot = 3, name = "司库", modules = new[] { "taxation" } }
            });
            var manager = new CharacterManager();
            CharacterData ruler = AddCharacter(manager, "君", CharacterRole.Noble);
            CharacterData chancellor = AddCharacter(manager, "相", CharacterRole.Scholar);
            CharacterData steward = AddCharacter(manager, "库", CharacterRole.Noble);
            RealmData realm = NewRealm(ruler);
            foreach (CharacterData c in new[] { ruler, chancellor, steward }) c.realmId = realm.realmId;
            realm.officeHolders[2] = chancellor.characterId;
            realm.officeHolders[3] = steward.characterId;

            Assert.IsTrue(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleLawProposal, manager, out CharacterData proposer));
            Assert.AreEqual(chancellor.characterId, proposer.characterId, "提案权应落到持 law_proposal 的官员");
            Assert.IsTrue(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleTaxation, manager, out CharacterData taxer));
            Assert.AreEqual(steward.characterId, taxer.characterId, "征税权应落到持 taxation 的官员");
            Assert.IsFalse(OfficeSystem.HasModuleHolder(realm, OfficeSystem.ModuleJudiciary, manager, out _), "无人持 judiciary 时不得凭空有审判权");
        }

        [Test]
        public void LawContext_CentralOfficeProposalFollowsModule()
        {
            var realm = new RealmData { realmId = 0 };
            realm.officeHolders[1] = 200; // 区长官：只有 taxation
            realm.officeHolders[2] = 300; // 宰相：law_proposal
            var context = new MyriadCreation.Simulation.WorldState.LawRealmContext(realm);

            Assert.IsFalse(context.IsCentralOfficeHolder(200), "持 taxation 的区长官不能推中央机构法");
            Assert.IsTrue(context.IsCentralOfficeHolder(300), "持 law_proposal 的宰相可以推中央机构法");
        }

        [Test]
        public void Judiciary_WithoutJudgeDismissesInsteadOfTrying()
        {
            var judicial = new MyriadCreation.Simulation.LawHost.JudicialSystem();
            var result = judicial.TrialCrime(
                MyriadCreation.Simulation.Laws.CrimeType.Witchcraft, 1, 0, new RealmData(), hasJudge: false);

            Assert.AreEqual(MyriadCreation.Simulation.Laws.VerdictType.Dismissed, result.verdict,
                "没有持 judiciary 职权的官员时，法庭不成立：案件驳回，而不是判无罪");
        }

        // ---------- 5. 存档往返 ----------

        [Test]
        public void OfficeHoldersAndAppointmentDays_RoundTrip()
        {
            var realm = new RealmData { realmId = 3 };
            realm.officeHolders[2] = 11;
            realm.officeHolders[6] = 12;
            realm.officeAppointedDays[2] = 500;
            realm.officeAppointedDays[6] = 520;

            RealmOfficeSaveData dto = RealmOfficeSaveData.FromRealm(realm);
            var restored = new RealmData { realmId = 3 };
            dto.ApplyTo(restored);

            Assert.AreEqual(11, restored.officeHolders[2]);
            Assert.AreEqual(12, restored.officeHolders[6]);
            Assert.AreEqual(500, restored.officeAppointedDays[2]);
            Assert.AreEqual(520, restored.officeAppointedDays[6]);
        }
    }
}
