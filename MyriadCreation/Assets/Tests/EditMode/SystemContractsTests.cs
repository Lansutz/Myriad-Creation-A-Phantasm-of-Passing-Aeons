using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Laws;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.WorldState;
using UnityEngine;
using MyriadCreation.Simulation.LawHost;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// B4/B5/B6-part1 系统契约测试：
    /// 1) Core.Contracts 接口/参数字面量只含 Core 类型（无 Simulation 反向依赖）；
    /// 2) GameWorld facade 返回契约类型，且 Query/Command 指向同一适配器；
    /// 3) 适配器委托与具体 API 结果一致（Law/Faith/Culture 查询 + 命令改状态）。
    /// </summary>
    public class SystemContractsTests
    {
        private GameObject _go;
        private GameWorld _world;
        private RealmData _realm;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("SystemContractsTestWorld");
            _world = _go.AddComponent<GameWorld>();
            if (_world.realms == null) _world.realms = new Dictionary<int, RealmData>();
            if (_world.cultures == null) _world.cultures = new Dictionary<int, CultureData>();
            _world.realms.Clear();
            _world.cultures.Clear();

            _realm = new RealmData
            {
                realmId = 1,
                stateReligionId = 5,
                primaryCultureId = 7,
                treasury = 1000f
            };
            _realm.lawState.knownLawIds.Add("tax_light");
            _realm.lawState.knownLawIds.Add("tax_standard");
            _realm.lawState.knownLawIds.Add("tax_heavy");
            _realm.lawState.SetActiveLaw("taxation", "tax_light", 1);
            _realm.lawState.legitimacy = 60f;
            _realm.lawState.authority = 100f;
            _world.realms[1] = _realm;
            _world.currentDay = 1;
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        private List<FaithSystem> FaithList()
        {
            FieldInfo field = typeof(GameWorld).GetField("_faithSystems", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "GameWorld._faithSystems 字段应存在");
            List<FaithSystem> list = field.GetValue(_world) as List<FaithSystem>;
            Assert.IsNotNull(list, "GameWorld._faithSystems 应为已初始化 List");
            return list;
        }

        private FaithSystem AddFaith(int faithId, string faithName, float fervor)
        {
            FaithSystem faith = new FaithSystem
            {
                faithId = faithId,
                faithName = faithName,
                fervor = fervor
            };
            faith.holySiteTileIndices.Add(11);
            faith.holySiteTileIndices.Add(22);
            List<FaithSystem> list = FaithList();
            list.Clear();
            list.Add(faith);
            return faith;
        }

        // ===================== 1. 契约无 Simulation 反向依赖 =====================

        [Test]
        public void Contracts_OnlyUseCoreAndSimpleTypes()
        {
            Assembly coreAssembly = typeof(ILawQuery).Assembly;
            Assert.AreEqual("MyriadCreation.Core", coreAssembly.GetName().Name, "契约应位于 Core 程序集");
            Assembly simulationAssembly = typeof(GameWorld).Assembly;
            foreach (AssemblyName referenced in coreAssembly.GetReferencedAssemblies())
            {
                Assert.AreNotEqual(simulationAssembly.GetName().Name, referenced.Name, "Core 不得引用 Simulation");
            }

            Type[] interfaces =
            {
                typeof(IFaithQuery), typeof(IFaithCommand),
                typeof(ICultureQuery), typeof(ICultureCommand),
                typeof(ILawQuery), typeof(ILawCommand)
            };
            foreach (Type iface in interfaces)
            {
                Assert.AreSame(coreAssembly, iface.Assembly, iface.Name + " 必须在 Core");
                foreach (MethodInfo method in iface.GetMethods())
                {
                    AssertSafeType(method.ReturnType, iface.Name + "." + method.Name + " 返回类型");
                    foreach (ParameterInfo parameter in method.GetParameters())
                    {
                        AssertSafeType(parameter.ParameterType, iface.Name + "." + method.Name + " 参数 " + parameter.Name);
                    }
                }
            }
        }

        private static void AssertSafeType(Type type, string context)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }
            Assert.IsFalse(type.Assembly == typeof(GameWorld).Assembly, context + " 不得是 Simulation 类型：" + type);
            if (type == typeof(void) || type.IsPrimitive || type == typeof(string) || type.IsEnum)
            {
                return;
            }
            if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                {
                    AssertSafeType(argument, context + " 泛型参数");
                }
                return;
            }
            Assert.AreEqual("MyriadCreation.Core", type.Assembly.GetName().Name, context + " 只能是 Core 类型：" + type);
        }

        // ===================== 2. facade 暴露接口 =====================

        [Test]
        public void GameWorld_ExposesContractFacades()
        {
            Assert.AreEqual(typeof(ILawQuery), typeof(GameWorld).GetProperty("LawQuery").PropertyType);
            Assert.AreEqual(typeof(ILawCommand), typeof(GameWorld).GetProperty("LawCommand").PropertyType);
            Assert.AreEqual(typeof(IFaithQuery), typeof(GameWorld).GetProperty("FaithQuery").PropertyType);
            Assert.AreEqual(typeof(IFaithCommand), typeof(GameWorld).GetProperty("FaithCommand").PropertyType);
            Assert.AreEqual(typeof(ICultureQuery), typeof(GameWorld).GetProperty("CultureQuery").PropertyType);
            Assert.AreEqual(typeof(ICultureCommand), typeof(GameWorld).GetProperty("CultureCommand").PropertyType);

            Assert.AreSame(_world.LawQuery, _world.LawCommand, "Law 查询/命令应共享同一适配器实例");
            Assert.AreSame(_world.FaithQuery, _world.FaithCommand, "Faith 查询/命令应共享同一适配器实例");
            Assert.AreSame(_world.CultureQuery, _world.CultureCommand, "Culture 查询/命令应共享同一适配器实例");
            Assert.AreSame(_world.LawQuery, _world.GetLawContracts());
            Assert.AreSame(_world.FaithQuery, _world.GetFaithContracts());
            Assert.AreSame(_world.CultureQuery, _world.GetCultureContracts());
        }

        // ===================== 3. Law 适配器委托一致 =====================

        [Test]
        public void LawContracts_DelegatesToLawsApi()
        {
            ILawQuery query = _world.LawQuery;
            Assert.AreEqual("tax_light", query.GetActiveLaw(1, "taxation"));
            Assert.AreEqual(_realm.lawState.GetActiveLaw("taxation"), query.GetActiveLaw(1, "taxation"));
            CollectionAssert.Contains(query.GetKnownLaws(1).ToList(), "tax_light");
            CollectionAssert.IsEmpty(query.GetPendingLaws(1).ToList());

            // 视图与直接调用 LawDecreeViewBuilder 一致
            LawDecreeView expected = LawDecreeViewBuilder.Build(
                _realm, DefaultLawCatalog.CreateGroups(), DefaultLawCatalog.CreateDecrees(), _world.currentDay, null);
            LawGovernmentViewDto view = query.GetGovernmentView(1, _world.currentDay);
            Assert.AreEqual(expected.enactedLaws.Count, view.enactedLaws.Count);
            Assert.AreEqual(expected.proposableLaws.Count, view.proposableLaws.Count);
            Assert.AreEqual(expected.enactableDecrees.Count, view.enactableDecrees.Count);
            for (int i = 0; i < expected.enactedLaws.Count; i++)
            {
                Assert.AreEqual(expected.enactedLaws[i].label, view.enactedLaws[i].label);
                Assert.AreEqual(expected.enactedLaws[i].law.lawId, view.enactedLaws[i].lawId);
                Assert.AreEqual(expected.enactedLaws[i].law.groupId, view.enactedLaws[i].groupId);
            }

            // 命令：ForceSetLaw 直改 → 查询读回一致
            string reason;
            Assert.IsTrue(_world.LawCommand.ForceSetLaw(1, "taxation", "tax_standard", _world.currentDay, out reason), reason);
            Assert.AreEqual("tax_standard", _realm.lawState.GetActiveLaw("taxation"));
            Assert.AreEqual("tax_standard", query.GetActiveLaw(1, "taxation"));
            Assert.IsTrue(_realm.lawState.IsLawKnown("tax_standard"));

            // 命令：法令颁布/撤销委托
            Assert.IsTrue(_world.LawCommand.EnactDecree(1, "decree_tax_relief", _world.currentDay, out reason), "颁布法令应成功：" + reason);
            Assert.AreEqual(1, _world.LawQuery.GetGovernmentView(1, _world.currentDay).activeDecrees.Count);
            Assert.IsTrue(_world.LawCommand.RevokeDecree(1, "decree_tax_relief", out reason), "撤销法令应成功：" + reason);
            Assert.AreEqual(0, _world.LawQuery.GetGovernmentView(1, _world.currentDay).activeDecrees.Count);

            // EnsureKnownLaws/Tick 契约命令不抛且与底层状态兼容
            _world.LawCommand.EnsureKnownLaws(1);
            Assert.AreEqual(0, _world.LawCommand.TickPendingLaws(1, _world.currentDay + 30));
            Assert.IsFalse(_world.LawCommand.BeginEnactLaw(999, "tax_light", -1, _world.currentDay, out reason), "不存在的政权应失败");
        }

        // ===================== 4. Faith 适配器委托一致 =====================

        [Test]
        public void FaithContracts_DelegatesToFaithApi()
        {
            AddFaith(5, "测试信仰", 42f);
            IFaithQuery query = _world.FaithQuery;
            Assert.AreEqual(5, query.GetStateReligionId(1));
            Assert.AreEqual(_realm.stateReligionId, query.GetStateReligionId(1));
            Assert.IsTrue(query.HasFaith(5));
            Assert.IsFalse(query.HasFaith(999));
            Assert.AreEqual("测试信仰", query.GetFaithName(5));
            Assert.AreEqual(_world.GetFaithSystem(5).faithName, query.GetFaithName(5));
            Assert.AreEqual(42f, query.GetFervor(5), 0.0001f);
            Assert.AreEqual(2, query.GetHolySiteTiles(5).Count);
            Assert.IsTrue(query.TryGetFaithSnapshot(5, out FaithSnapshotDto snapshot));
            Assert.AreEqual("测试信仰", snapshot.faithName);
            Assert.AreEqual(2, snapshot.holySiteCount);
            Assert.IsFalse(query.TryGetFaithSnapshot(999, out _));

            // 命令委托：改国教 / 加虔诚
            string reason;
            Assert.IsTrue(_world.FaithCommand.SetStateReligion(1, 5, out reason), reason);
            Assert.AreEqual(5, _realm.stateReligionId);
            Assert.IsTrue(_world.FaithCommand.AddFervor(5, 8f, out reason), reason);
            Assert.AreEqual(50f, _world.GetFaithSystem(5).fervor, 0.0001f);
            Assert.IsFalse(_world.FaithCommand.AddFervor(999, 8f, out reason));
            Assert.IsFalse(_world.FaithCommand.SetStateReligion(999, 5, out reason));
        }

        // ===================== 5. Culture 适配器委托一致 =====================

        [Test]
        public void CultureContracts_DelegatesToCultureApi()
        {
            _world.cultures[7] = new CultureData
            {
                cultureId = 7,
                cultureName = "测试文化",
                stage = GameEnums.CultureStage.Tribe
            };
            ICultureQuery query = _world.CultureQuery;
            Assert.AreEqual(7, query.GetPrimaryCultureId(1));
            Assert.AreEqual(_realm.primaryCultureId, query.GetPrimaryCultureId(1));
            Assert.AreEqual("测试文化", query.GetCultureName(7));
            Assert.AreEqual(_world.cultures[7].cultureName, query.GetCultureName(7));
            Assert.AreEqual((int)GameEnums.CultureStage.Tribe, query.GetCultureStage(7));
            Assert.AreEqual("Tribe", query.GetCultureStageKey((int)GameEnums.CultureStage.Tribe));
            Assert.AreEqual("Unknown", query.GetCultureStageKey(99));
            Assert.AreEqual(-1, query.GetCultureStage(999));

            // 命令委托：阶段 / 主体文化
            string reason;
            Assert.IsTrue(_world.CultureCommand.SetCultureStage(7, (int)GameEnums.CultureStage.Chiefdom, out reason), reason);
            Assert.AreEqual(GameEnums.CultureStage.Chiefdom, _world.cultures[7].stage);
            Assert.AreEqual((int)GameEnums.CultureStage.Chiefdom, query.GetCultureStage(7));
            Assert.IsFalse(_world.CultureCommand.SetCultureStage(7, 99, out reason));
            Assert.IsTrue(_world.CultureCommand.SetPrimaryCulture(1, 7, out reason), reason);
            Assert.AreEqual(7, _realm.primaryCultureId);
            Assert.IsFalse(_world.CultureCommand.SetPrimaryCulture(1, 999, out reason));
        }
    }
}
