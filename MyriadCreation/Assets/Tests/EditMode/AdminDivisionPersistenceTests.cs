using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>存读档 v12：行政区划树与治理者绑定入档（读档后法令下发路径不断）。</summary>
    public class AdminDivisionPersistenceTests
    {
        private static RealmData BuildRealm()
        {
            var realm = new RealmData { realmId = 3, realmName = "测试国" };

            var root = new AdminDivision
            {
                divisionId = 1,
                realmId = 3,
                level = 1,
                name = "测试国",
                titleId = "title_ruler",
                holderCharacterId = 11
            };
            var jun = new AdminDivision
            {
                divisionId = 2,
                realmId = 3,
                level = 2,
                name = "郡1",
                parentDivisionId = 1,
                titleId = "title_gov_general",
                holderCharacterId = 12
            };
            var xian = new AdminDivision
            {
                divisionId = 3,
                realmId = 3,
                level = 3,
                name = "县1",
                parentDivisionId = 2,
                titleId = "title_magistrate",
                holderCharacterId = 13
            };

            root.childIds.Add(2);
            jun.childIds.Add(3);
            root.tiles.Add(0);
            jun.tiles.Add(0);
            jun.tiles.Add(1);
            xian.tiles.Add(1);

            realm.adminDivisions.AddRange(new[] { root, jun, xian });
            return realm;
        }

        [Test]
        public void RealmDto_JsonRoundTrip_PreservesDivisionTreeAndHolders()
        {
            var realm = BuildRealm();

            var dto = RealmDTO.FromRealmData(realm);
            Assert.AreEqual(3, dto.adminDivisions.Count, "区划应入 DTO");

            var json = JsonUtility.ToJson(dto);
            var reloaded = JsonUtility.FromJson<RealmDTO>(json);
            var back = reloaded.ToRealmData();

            Assert.AreEqual(3, back.adminDivisions.Count, "区划树应还原");

            var byId = new Dictionary<int, AdminDivision>();
            foreach (var division in back.adminDivisions) byId[division.divisionId] = division;

            Assert.AreEqual("测试国", byId[1].name);
            Assert.AreEqual("title_ruler", byId[1].titleId);
            Assert.AreEqual(11, byId[1].holderCharacterId);
            CollectionAssert.AreEqual(new[] { 2 }, byId[1].childIds);

            Assert.AreEqual(1, byId[2].parentDivisionId, "父子关系");
            Assert.AreEqual(2, byId[2].level);
            Assert.AreEqual("title_gov_general", byId[2].titleId);
            Assert.AreEqual(12, byId[2].holderCharacterId);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, byId[2].tiles, "辖境地块");

            Assert.AreEqual(2, byId[3].parentDivisionId);
            Assert.AreEqual(13, byId[3].holderCharacterId);
        }

        [Test]
        public void LegacyRealmDto_WithoutDivisions_ProducesEmptyTree()
        {
            // v11 之前的档没有 adminDivisions 段：读档后应得到空表，由 EnsureAdminDivisions 懒生成
            var json = "{\"realmId\":3,\"realmName\":\"旧国\"}";
            var reloaded = JsonUtility.FromJson<RealmDTO>(json);
            var realm = reloaded.ToRealmData();

            Assert.AreEqual(3, realm.realmId);
            Assert.IsNotNull(realm.adminDivisions);
            Assert.AreEqual(0, realm.adminDivisions.Count);
        }
    }
}
