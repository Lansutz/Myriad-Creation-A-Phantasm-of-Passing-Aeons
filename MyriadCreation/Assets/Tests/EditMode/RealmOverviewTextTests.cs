using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.UI.Presentation;

namespace MyriadCreation.Tests
{
    /// <summary>
    /// 政权总览文本 4d 契约回归：Build(IWorldReadModel, realmId) 只使用聚合 DTO。
    /// 旧 RealmData/RealmSociety 重载随 Legacy 删除；原社会/法律/宗教细节断言迁移到新契约可表达字段。
    /// </summary>
    public class RealmOverviewTextTests
    {
        [Test]
        public void Overview_Build_UsesWorldReadModel()
        {
            var go = new GameObject("RealmOverviewTextTestWorld");
            var world = go.AddComponent<GameWorld>();
            world.realms = new Dictionary<int, RealmData>
            {
                { 1, new RealmData { realmId = 1, realmName = "测试国", treasury = 5000f, stability = 70f, centralization = 0.6f } }
            };
            var view = new WorldReadModel(world);

            var offices = new Dictionary<int, string> { { 0, "1. 总督：张三" } };
            var text = RealmOverviewText.Build(view, 1, offices, true);

            Assert.IsTrue(text.Contains("测试国（本家）"), "政权名+本家标记");
            Assert.IsTrue(text.Contains("国库：5000"), "国库字段来自 RealmSnapshotDto");
            Assert.IsTrue(text.Contains("── 官职体系 ──"), "官职区");
            Assert.IsTrue(text.Contains("总督：张三"), "官职内容");

            var empty = RealmOverviewText.Build((IWorldReadModel)null, 1);
            Assert.IsTrue(empty.Contains("未选中政权"), "空 view 友好提示");

            Object.DestroyImmediate(go);
        }
    }
}
