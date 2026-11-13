using System.Collections.Generic;
using NUnit.Framework;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.ContentHost;
using MyriadCreation.Content.Modding;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>革新解锁引用必须指向存在的物品/单位/物资，不能留悬空 id。</summary>
    public class InnovationUnlockRefTests
    {
        // 当前基础物资/单位 id；新增时同步维护，避免解锁引用指向不存在的定义。
        private static readonly HashSet<int> Goods = new HashSet<int>
        {
            0,1,2,3,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,
            30,31,40,50,51,52,53,54,55,56,57,58,59,60,61,70,71,80,81,90,
            100,101,110,111,112,120,121,122,130,131,140,141,142,143,
        };
        private static readonly HashSet<int> Units = new HashSet<int> { 100,101,102,110,111,200,201,202,300,301,302,303 };
        private static readonly HashSet<string> Laws = new HashSet<string>
        {
            "tax_light","tax_standard","tax_heavy","militia","standing_army",
            "charter_free_city","charter_guild","succ_hereditary","succ_elective","succ_usurpation",
            "succ_rotation","scope_absolute","scope_legally_bound","scope_custom_bound",
            "scope_consensual","scope_divinely_bound","cent_succ_appointed","cent_succ_elected",
            "cent_succ_examination","cent_succ_hereditary","local_succ_appointed","local_succ_elected",
            "local_succ_hereditary","local_succ_charter","local_succ_fully_autonomy",
            "local_succ_full_autonomy","local_scope_full_autonomy","local_scope_fiscal_judicial",
            "local_scope_military_only","local_scope_none","spatial_unitary","spatial_federal","spatial_confederal",
            "feudal_host","mercenary_army","military_discipline","military_logistics","military_justice",
            "command_hierarchy","military_training","border_garrison","frontier_march","border_trade",
            "border_pass","border_patrol",
            "national_army","city_army","lords_host","central_command","theater_autonomy","frontier_discretion",
        };
        private static readonly HashSet<int> Buildings = new HashSet<int>
        {
            100,101,102,103,200,201,202,203,204,300,301,302,303,400,401,402,403,404,
            500,501,502,503,504,600,601,602,603,604,605,
        };

        [SetUp]
        public void Setup() { ContentRegistry.Reset(); ContentRegistry.Initialize(); }

        [Test]
        public void UnlockRefs_Resolve()
        {
            foreach (var pair in ContentRegistry.Innovations)
            {
                var def = pair.Value;
                if (def.unlockRefs == null) continue;
                foreach (var r in def.unlockRefs)
                {
                    if (r == null) continue;
                    switch (r.kind)
                    {
                        case InnovationUnlockKind.Good:
                            Assert.IsTrue(Goods.Contains(r.id), $"革新 {pair.Key} 解锁物资 {r.id} 不存在");
                            break;
                        case InnovationUnlockKind.Unit:
                            Assert.IsTrue(Units.Contains(r.id), $"革新 {pair.Key} 解锁单位 {r.id} 不存在");
                            break;
                        case InnovationUnlockKind.Equipment:
                            Assert.IsTrue(ContentRegistry.Equipment.ContainsKey(r.id), $"革新 {pair.Key} 解锁物品 {r.id} 不存在");
                            break;
                        case InnovationUnlockKind.Building:
                            Assert.IsTrue(Buildings.Contains(r.id), $"革新 {pair.Key} 解锁建筑 {r.id} 不存在");
                            break;
                        case InnovationUnlockKind.Recipe:
                            Assert.IsTrue(RecipeCatalog.TryGet(r.key, out _), $"革新 {pair.Key} 解锁配方 {r.key} 不存在");
                            break;
                        case InnovationUnlockKind.Law:
                            Assert.IsTrue(Laws.Contains(r.key), $"革新 {pair.Key} 解锁法律 {r.key} 不存在");
                            break;
                    }
                }
            }
        }
    }
}
