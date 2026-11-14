using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Enums;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.WorldState;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// A1/A2：文化 DTO 与映射器分离后，JSON 往返必须保持字段级一致。
    /// </summary>
    public class CultureSaveMappingTests
    {
        [Test]
        public void CultureSaveData_RoundTripsThroughJson()
        {
            var culture = new CultureData
            {
                cultureId = 7,
                cultureName = "河谷文化",
                cultureColorHex = "#abcdef",
                faithId = 601,
                languageId = "lang_x",
                languageFamilyId = "fam_y",
                ethnicGroupId = "eth_z",
                heritageId = "her_1",
                ethosId = "ethos_2",
                traditionIds = new List<string> { "tr_a", "tr_b" },
                maturity = 0.42f,
                spreadPower = 1.7f,
                parentCultureId = 3,
                childCultureIds = new List<int> { 8, 9 },
                allowsBranching = false,
                heraldryEnabled = true,
                mergedIntoCultureId = 5,
                lastSplitDay = 12345,
                innovationAffinities = new List<string> { "Agriculture", "Craft" }
            };
            culture.color = new Color(0.2f, 0.3f, 0.4f, 1f);
            culture.defaultSuccessionLaw = InheritanceLaw.Matrilineal();
            culture.officialTitles.Add(new OfficeTitleEntry { office = "chancellor", polityKey = "k_1", titleKey = "t_1" });

            CultureSaveData dto = CultureSaveMapping.ToSave(culture);
            string json = JsonUtility.ToJson(dto);
            CultureData restored = CultureSaveMapping.ToRuntime(JsonUtility.FromJson<CultureSaveData>(json));

            Assert.AreEqual(culture.cultureId, restored.cultureId);
            Assert.AreEqual(culture.cultureName, restored.cultureName);
            Assert.AreEqual(culture.cultureColorHex, restored.cultureColorHex);
            Assert.AreEqual(culture.faithId, restored.faithId);
            Assert.AreEqual(culture.languageId, restored.languageId);
            Assert.AreEqual(culture.languageFamilyId, restored.languageFamilyId);
            Assert.AreEqual(culture.ethnicGroupId, restored.ethnicGroupId);
            Assert.AreEqual(culture.heritageId, restored.heritageId);
            Assert.AreEqual(culture.ethosId, restored.ethosId);
            CollectionAssert.AreEqual(culture.traditionIds, restored.traditionIds);
            Assert.AreEqual(culture.maturity, restored.maturity, 1e-5f);
            Assert.AreEqual(culture.spreadPower, restored.spreadPower, 1e-5f);
            Assert.AreEqual(culture.parentCultureId, restored.parentCultureId);
            CollectionAssert.AreEqual(culture.childCultureIds, restored.childCultureIds);
            Assert.AreEqual(culture.allowsBranching, restored.allowsBranching);
            Assert.AreEqual(culture.heraldryEnabled, restored.heraldryEnabled);
            Assert.AreEqual(culture.mergedIntoCultureId, restored.mergedIntoCultureId);
            Assert.AreEqual(culture.lastSplitDay, restored.lastSplitDay);
            Assert.AreEqual(culture.color.r, restored.color.r, 1e-5f);
            CollectionAssert.AreEqual(culture.innovationAffinities, restored.innovationAffinities);
            Assert.AreEqual(culture.defaultSuccessionLaw.gender, restored.defaultSuccessionLaw.gender);
            Assert.AreEqual(culture.defaultSuccessionLaw.landMode, restored.defaultSuccessionLaw.landMode);
            Assert.AreEqual(1, restored.officialTitles.Count);
            Assert.AreEqual("chancellor", restored.officialTitles[0].office);
            Assert.AreEqual("t_1", restored.officialTitles[0].titleKey);
        }
    }
}
