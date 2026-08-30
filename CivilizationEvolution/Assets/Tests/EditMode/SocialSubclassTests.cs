using System.Collections.Generic;
using NUnit.Framework;
using CivilizationEvolution.Core;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// 社会阶层细分 EditMode 测试（用户定稿：农民/自由民/奴隶三阶层细分）
    /// 主枚举不动（classRelations 字典键/存档兼容），亚阶层 SocialSubclass 12 个
    /// </summary>
    public class SocialSubclassTests
    {
        // ===== 亚阶层 ↔ 主阶层 映射完整性 =====

        [Test]
        public void Hierarchy_SubclassToClass_Mapping()
        {
            // 农民四层
            Assert.AreEqual(GameEnums.SocialClass.Peasant, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Freeholder));
            Assert.AreEqual(GameEnums.SocialClass.Peasant, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Tenant));
            Assert.AreEqual(GameEnums.SocialClass.Peasant, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Serf));
            Assert.AreEqual(GameEnums.SocialClass.Peasant, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.HiredLaborer));

            // 自由民四民（士农工商）
            Assert.AreEqual(GameEnums.SocialClass.MerchantFreeman, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Citizen));
            Assert.AreEqual(GameEnums.SocialClass.MerchantFreeman, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Merchant));
            Assert.AreEqual(GameEnums.SocialClass.MerchantFreeman, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Artisan));
            Assert.AreEqual(GameEnums.SocialClass.MerchantFreeman, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.Scholar));

            // 奴隶四源
            Assert.AreEqual(GameEnums.SocialClass.Slave, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.DomesticSlave));
            Assert.AreEqual(GameEnums.SocialClass.Slave, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.StateSlave));
            Assert.AreEqual(GameEnums.SocialClass.Slave, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.DebtSlave));
            Assert.AreEqual(GameEnums.SocialClass.Slave, GameEnums.SocialClassHierarchy.GetClass(GameEnums.SocialSubclass.WarCaptiveSlave));
        }

        [Test]
        public void Hierarchy_ClassToSubclasses_AllFour()
        {
            // 每个细分的阶层都有 4 个亚类
            Assert.AreEqual(4, GameEnums.SocialClassHierarchy.GetSubclasses(GameEnums.SocialClass.Peasant).Count, "农民四层");
            Assert.AreEqual(4, GameEnums.SocialClassHierarchy.GetSubclasses(GameEnums.SocialClass.MerchantFreeman).Count, "自由民四民");
            Assert.AreEqual(4, GameEnums.SocialClassHierarchy.GetSubclasses(GameEnums.SocialClass.Slave).Count, "奴隶四源");
            // 未细分阶层无亚类
            Assert.AreEqual(0, GameEnums.SocialClassHierarchy.GetSubclasses(GameEnums.SocialClass.Royalty).Count);
            Assert.AreEqual(0, GameEnums.SocialClassHierarchy.GetSubclasses(GameEnums.SocialClass.NobilityClergy).Count);
        }

        [Test]
        public void Hierarchy_DefaultSubclass()
        {
            Assert.AreEqual(GameEnums.SocialSubclass.Freeholder,
                GameEnums.SocialClassHierarchy.GetDefaultSubclass(GameEnums.SocialClass.Peasant));
            Assert.AreEqual(GameEnums.SocialSubclass.Citizen,
                GameEnums.SocialClassHierarchy.GetDefaultSubclass(GameEnums.SocialClass.MerchantFreeman));
            Assert.AreEqual(GameEnums.SocialSubclass.DomesticSlave,
                GameEnums.SocialClassHierarchy.GetDefaultSubclass(GameEnums.SocialClass.Slave));
            Assert.IsNull(GameEnums.SocialClassHierarchy.GetDefaultSubclass(GameEnums.SocialClass.Royalty),
                "未细分阶层无默认亚类");
        }

        // ===== CharacterData 对接 =====

        [Test]
        public void Character_SetSocialClass_SyncsSubclass()
        {
            var c = new CharacterData { firstName = "甲", lastName = "氏", age = 30, isMale = true };

            c.SetSocialClass(GameEnums.SocialClass.Slave);
            Assert.AreEqual(GameEnums.SocialClass.Slave, c.socialClass);
            Assert.AreEqual(GameEnums.SocialSubclass.DomesticSlave, c.socialSubclass, "设置主类自动同步默认亚类");

            c.SetSocialClass(GameEnums.SocialClass.MerchantFreeman);
            Assert.AreEqual(GameEnums.SocialSubclass.Citizen, c.socialSubclass);

            // 亚类手动细分：农奴/债务奴
            c.SetSocialClass(GameEnums.SocialClass.Peasant);
            c.socialSubclass = GameEnums.SocialSubclass.Serf;
            Assert.AreEqual(GameEnums.SocialSubclass.Serf, c.socialSubclass);
            Assert.AreEqual(GameEnums.SocialClass.Peasant, GameEnums.SocialClassHierarchy.GetClass(c.socialSubclass),
                "亚类与主类一致");

            // 未细分阶层：保留原亚类（设置 Royalty 不动 subclass）
            c.SetSocialClass(GameEnums.SocialClass.Royalty);
            Assert.AreEqual(GameEnums.SocialClass.Royalty, c.socialClass);
            Assert.AreEqual(GameEnums.SocialSubclass.Serf, c.socialSubclass, "未细分阶层保留原亚类");
        }
    }
}
