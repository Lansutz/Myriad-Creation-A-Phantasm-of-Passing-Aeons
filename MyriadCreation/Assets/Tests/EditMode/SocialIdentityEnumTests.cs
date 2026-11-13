using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 社会身份四轴枚举：阶级（排他）/ 阶层（排他）/ 身份（可叠加）/ 职业（可叠加）+ 旧模型迁移映射。
    /// </summary>
    public class SocialIdentityEnumTests
    {
        [Test]
        public void Enums_AreDistinctAxes()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(EconomicClass), EconomicClass.Slave));
            Assert.IsTrue(System.Enum.IsDefined(typeof(SocialStratum), SocialStratum.Upper));
            Assert.IsTrue(System.Enum.IsDefined(typeof(SocialIdentityFlags), SocialIdentityFlags.Noble));
            Assert.IsTrue(System.Enum.IsDefined(typeof(OccupationFlags), OccupationFlags.Merchant));

            // 四个枚举是不同类型，不能互相赋值（编译期事实，这里断言名称不同）
            Assert.AreNotEqual(nameof(EconomicClass), nameof(SocialStratum));
            Assert.AreNotEqual(nameof(SocialIdentityFlags), nameof(OccupationFlags));
        }

        [Test]
        public void ClassSet_HasNoSlaveOwnerClass()
        {
            // 有“奴隶”阶级，但没有“奴隶主”阶级——拥有奴隶是财产标记
            Assert.IsTrue(System.Array.IndexOf(System.Enum.GetValues(typeof(EconomicClass)), EconomicClass.Slave) >= 0);
            foreach (var name in System.Enum.GetNames(typeof(EconomicClass)))
                StringAssert.DoesNotContain("Owner", name);
            StringAssert.DoesNotContain("Master", string.Join(",", System.Enum.GetNames(typeof(EconomicClass))));
        }

        [Test]
        public void IdentityAndOccupation_CanStack()
        {
            var profile = new IdentityProfile
            {
                economicClass = EconomicClass.SelfEmployed,
                stratum = SocialStratum.Middle,
                identity = SocialIdentityFlags.Citizen | SocialIdentityFlags.Noble,
                occupations = OccupationFlags.Merchant | OccupationFlags.Scholar,
                ownsSlaves = true,
            };

            Assert.IsTrue(profile.HasIdentity(SocialIdentityFlags.Citizen));
            Assert.IsTrue(profile.HasIdentity(SocialIdentityFlags.Noble));
            Assert.IsTrue(profile.HasOccupation(OccupationFlags.Merchant));
            Assert.IsTrue(profile.HasOccupation(OccupationFlags.Scholar));
            Assert.IsFalse(profile.HasOccupation(OccupationFlags.Soldier));
            Assert.IsTrue(profile.ownsSlaves, "自雇者也可以占有奴隶——不改变阶级");

            profile.AddOccupation(OccupationFlags.Soldier);
            Assert.IsTrue(profile.HasOccupation(OccupationFlags.Soldier));
            profile.RemoveIdentity(SocialIdentityFlags.Noble);
            Assert.IsFalse(profile.HasIdentity(SocialIdentityFlags.Noble));
        }

        [Test]
        public void ClassChange_DoesNotEraseOccupations()
        {
            var profile = new IdentityProfile
            {
                economicClass = EconomicClass.SelfEmployed,
                occupations = OccupationFlags.Merchant | OccupationFlags.Scholar,
            };
            // 阶级是单值：直接替换，职业保留
            profile.economicClass = EconomicClass.Lord;
            Assert.AreEqual(EconomicClass.Lord, profile.economicClass);
            Assert.IsTrue(profile.HasOccupation(OccupationFlags.Merchant));
        }

        [Test]
        public void LegacyMapping_SplitsClassStatusAndOccupation()
        {
            var serf = LegacyIdentityMapping.FromLegacy(
                GameEnums.SocialClass.Peasant, GameEnums.SocialSubclass.Serf);
            Assert.AreEqual(EconomicClass.Serf, serf.economicClass);
            Assert.IsTrue(serf.HasOccupation(OccupationFlags.Farmer));
            Assert.AreEqual(SocialStratum.LowerMiddle, serf.stratum);

            var merchant = LegacyIdentityMapping.FromLegacy(
                GameEnums.SocialClass.MerchantFreeman, GameEnums.SocialSubclass.Merchant);
            Assert.AreEqual(EconomicClass.SelfEmployed, merchant.economicClass);
            Assert.IsTrue(merchant.HasOccupation(OccupationFlags.Merchant));
            Assert.IsTrue(merchant.HasIdentity(SocialIdentityFlags.Citizen));

            var noble = LegacyIdentityMapping.FromLegacy(GameEnums.SocialClass.NobilityClergy);
            Assert.AreEqual(EconomicClass.Lord, noble.economicClass);
            Assert.AreEqual(SocialStratum.Upper, noble.stratum);
            Assert.IsTrue(noble.HasIdentity(SocialIdentityFlags.Noble));
            Assert.IsTrue(noble.HasIdentity(SocialIdentityFlags.Clergy));

            var slave = LegacyIdentityMapping.FromLegacy(GameEnums.SocialClass.Slave);
            Assert.AreEqual(EconomicClass.Slave, slave.economicClass);
            Assert.AreEqual(SocialStratum.Lower, slave.stratum);
        }

        [Test]
        public void CharacterData_IdentityStaysInSyncWithLegacyFields()
        {
            var character = new CharacterData { socialClass = GameEnums.SocialClass.Peasant };
            character.SyncIdentityFromLegacy();
            Assert.AreEqual(EconomicClass.Freeholder, character.identity.economicClass);

            character.SetSocialClass(GameEnums.SocialClass.Peasant);
            character.socialSubclass = GameEnums.SocialSubclass.Tenant;
            character.SyncIdentityFromLegacy();
            Assert.AreEqual(EconomicClass.Tenant, character.identity.economicClass);
            Assert.IsTrue(character.identity.HasOccupation(OccupationFlags.Farmer));
        }

        [Test]
        public void PopulationBlock_HasFourAxisFields()
        {
            var block = new PopulationBlock
            {
                count = 100f,
                economicClass = EconomicClass.Tenant,
                stratum = SocialStratum.LowerMiddle,
                occupations = OccupationFlags.Farmer | OccupationFlags.Artisan,
            };
            Assert.AreEqual(EconomicClass.Tenant, block.economicClass);
            Assert.IsTrue((block.occupations & OccupationFlags.Artisan) != 0);
            Assert.AreEqual(100f, block.count);
        }
    }
}
