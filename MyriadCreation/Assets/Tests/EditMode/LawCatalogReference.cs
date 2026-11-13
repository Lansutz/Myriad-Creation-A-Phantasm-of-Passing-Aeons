using System.Collections.Generic;
using MyriadCreation.Content.Definitions;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 法律 JSON 的**测试基准**（task-87）：原生产硬编码完整目录（9 组 / 32 法 / 2 法令）迁移到测试侧，
    /// 仅用于校验 StreamingAssets/Base/Laws/Laws.json 的字段逐项一致；不参与运行时。
    /// 运行时唯一内容来源是 JSON；生产代码 DefaultLawCatalog 只保留最小回退集，不再维护整份目录。
    /// </summary>
    internal static class LawCatalogReference
    {
        public static List<LawGroupDef> CreateGroups()
        {
            return new List<LawGroupDef>
            {
                new LawGroupDef
                {
                    groupId = "taxation",
                    name = "赋税",
                    order = 10,
                    laws = new List<LawDef>
                    {
                        Law("tax_light", "taxation", "轻赋", 1, 50f, 10f, 0.9f, 2f, 1f),
                        Law("tax_standard", "taxation", "常赋", 2, 80f, 20f, 1.1f),
                        Law("tax_heavy", "taxation", "重赋", 3, 120f, 30f, 1.4f, -4f, -2f, initiallyKnown: false, enactmentDays: 60)
                    }
                },
                new LawGroupDef
                {
                    groupId = "military",
                    name = "军制",
                    order = 20,
                    laws = new List<LawDef>
                    {
                        Law("militia", "military", "民兵", 1, 60f, 15f, 1f, 1f, 0f, 1.2f, authority: ProposalAuthority.CentralInstitution),
                        Law("standing_army", "military", "常备军", 2, 150f, 35f, 1f, -2f, 0f, 1.5f, initiallyKnown: false, enactmentDays: 60, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 995 }),
                        Law("feudal_host", "military", "封建征召", 2, 90f, 20f, 1f, 0.5f, 0f, 1.2f, unrest: 0.5f, initiallyKnown: false, enactmentDays: 45, requiredInnovationIds: new List<int> { 501 }),
                        Law("mercenary_army", "military", "雇佣军", 3, 180f, 40f, 1.2f, -1f, 0f, 1.6f, initiallyKnown: false, enactmentDays: 90, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 1003 })
                    }
                },
                new LawGroupDef
                {
                    groupId = "military_organization",
                    name = "军队编制",
                    order = 21,
                    laws = new List<LawDef>
                    {
                        Law("national_army", "military_organization", "国家军", 2, 120f, 25f, 1f, 0f, 0f, 1.15f, initiallyKnown: false, enactmentDays: 60, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 995 }),
                        Law("city_army", "military_organization", "城市军", 2, 100f, 20f, 1f, 0.5f, 0f, 1.05f, initiallyKnown: false, enactmentDays: 45, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 994 }),
                        Law("lords_host", "military_organization", "领主军", 3, 140f, 30f, 1f, -0.5f, 0f, 1.2f, initiallyKnown: false, enactmentDays: 75, requiredInnovationIds: new List<int> { 501 })
                    }
                },
                new LawGroupDef
                {
                    groupId = "privilege",
                    name = "特权",
                    order = 30,
                    laws = new List<LawDef>
                    {
                        Law("charter_free_city", "privilege", "自由市特许", 1, 100f, 10f, 0.95f, 1f, 0f, 1f, description: "授予城市自治与贸易特许。", authority: ProposalAuthority.CharterHolders, enactmentDays: 45),
                        Law("charter_guild", "privilege", "行会特许", 2, 140f, 15f, 1f, 0f, 0f, 1f, description: "授予行会垄断与自治权。", authority: ProposalAuthority.CharterHolders, initiallyKnown: false, enactmentDays: 60),
                        Law("clergy_forum", "privilege", "教士法庭", 1, 80f, 10f, 1f, 0f, 0f, 1f, description: "教士不受世俗法庭审判，由教会法庭处置。", enactmentDays: 30, exemptIdentities: MyriadCreation.Core.Enums.SocialIdentityFlags.Clergy)
                    }
                },
                new LawGroupDef
                {
                    groupId = "succession",
                    name = "继承制度",
                    order = 40,
                    laws = new List<LawDef>
                    {
                        Law("succ_hereditary", "succession", "世袭继承", 1, 60f, 10f, 1f, 2f, 2f, description: "血统世袭，秩序稳定。"),
                        Law("succ_elective", "succession", "选举继承", 2, 80f, 15f, 1f, 0f, 1f, description: "推举产生继承人。"),
                        Law("succ_usurpation", "succession", "僭夺继承", 3, 120f, 25f, 1f, -2f, -2f, initiallyKnown: false, enactmentDays: 60, description: "强者得位，秩序动荡。"),
                        Law("succ_rotation", "succession", "轮座继承", 2, 70f, 10f, 1f, 1f, 0f, initiallyKnown: false, description: "轮替执政。"),
                    }
                },
                new LawGroupDef
                {
                    groupId = "supreme_scope",
                    name = "君权范围",
                    order = 50,
                    laws = new List<LawDef>
                    {
                        Law("scope_absolute", "supreme_scope", "绝对君权", 1, 100f, 20f, 1.05f, -1f, 0f, description: "君权不受约束。"),
                        Law("scope_legally_bound", "supreme_scope", "法理受限", 2, 90f, 15f, 0.95f, 1f, 1f, description: "君权受法律约束。"),
                        Law("scope_custom_bound", "supreme_scope", "惯例约束", 2, 80f, 10f, 0.95f, 2f, 0f, initiallyKnown: false, description: "君权受惯例约束。"),
                        Law("scope_consensual", "supreme_scope", "共议制约", 3, 100f, 15f, 0.9f, 2f, 2f, initiallyKnown: false, description: "重大事务需共议。"),
                        Law("scope_divinely_bound", "supreme_scope", "神意约束", 3, 110f, 20f, 0.9f, 3f, 2f, initiallyKnown: false, description: "君权受神意约束。")
                    }
                },
                new LawGroupDef
                {
                    groupId = "central_succession",
                    name = "中央继承",
                    order = 60,
                    laws = new List<LawDef>
                    {
                        Law("cent_succ_appointed", "central_succession", "上级决断", 1, 70f, 10f, 1f, 1f, 1f, authority: ProposalAuthority.CentralInstitution),
                        Law("cent_succ_elected", "central_succession", "集体选择", 2, 85f, 15f, 1f, 2f, 1f, initiallyKnown: false, authority: ProposalAuthority.CentralInstitution),
                        Law("cent_succ_examination", "central_succession", "客观标准", 2, 90f, 15f, 1.05f, 0f, 2f, authority: ProposalAuthority.CentralInstitution),
                        Law("cent_succ_hereditary", "central_succession", "血缘世袭", 3, 110f, 20f, 1f, 0f, 1f, 1.05f, initiallyKnown: false, authority: ProposalAuthority.CentralInstitution)
                    }
                },
                new LawGroupDef
                {
                    groupId = "local_succession",
                    name = "地方继承",
                    order = 70,
                    laws = new List<LawDef>
                    {
                        Law("local_succ_appointed", "local_succession", "地方任命", 1, 60f, 10f, 1f, 1f, authority: ProposalAuthority.CentralInstitution),
                        Law("local_succ_elected", "local_succession", "地方选举", 2, 70f, 15f, 0.95f, 1f, 1f, initiallyKnown: false, authority: ProposalAuthority.CentralInstitution),
                        Law("local_succ_hereditary", "local_succession", "地方世袭", 2, 80f, 15f, 1f, 0f, 0f, 1.05f, authority: ProposalAuthority.CentralInstitution),
                        Law("local_succ_charter", "local_succession", "城市特许", 2, 90f, 10f, 0.9f, 1f, 1f, initiallyKnown: false, authority: ProposalAuthority.CharterHolders)
                    }
                },
                new LawGroupDef
                {
                    groupId = "local_scope",
                    name = "地方权限",
                    order = 80,
                    laws = new List<LawDef>
                    {
                        Law("local_scope_full_autonomy", "local_scope", "地方自治", 2, 90f, 15f, 0.9f, 1f, 0f, 0.9f, initiallyKnown: false, authority: ProposalAuthority.CentralInstitution),
                        Law("local_scope_fiscal_judicial", "local_scope", "财政司法自治", 2, 100f, 15f, 0.95f, 1f, 0f, 0.95f, authority: ProposalAuthority.CentralInstitution),
                        Law("local_scope_military_only", "local_scope", "仅军事自治", 1, 70f, 10f, 1.05f, 0f, 0f, 1.05f, authority: ProposalAuthority.CentralInstitution),
                        Law("local_scope_none", "local_scope", "中央直辖", 2, 90f, 15f, 1.15f, -1f, 0f, 1.1f, initiallyKnown: false, authority: ProposalAuthority.CentralInstitution)
                    }
                },
                new LawGroupDef
                {
                    groupId = "spatial",
                    name = "领土结构",
                    order = 90,
                    laws = new List<LawDef>
                    {
                        Law("spatial_unitary", "spatial", "单一制", 1, 100f, 20f, 1.05f, 0f, 0f, 1.05f),
                        Law("spatial_federal", "spatial", "联邦制", 2, 120f, 25f, 0.95f, 1f, 1f, initiallyKnown: false, enactmentDays: 60),
                        Law("spatial_confederal", "spatial", "邦联制", 3, 140f, 30f, 0.85f, 1f, 0f, 0.9f, initiallyKnown: false, enactmentDays: 90)
                    }
                },
                new LawGroupDef
                {
                    groupId = "military_admin",
                    name = "军事管理",
                    order = 21,
                    laws = new List<LawDef>
                    {
                        Law("military_discipline", "military_admin", "军纪", 2, 80f, 20f, 1f, 1f, 0f, 1.1f, initiallyKnown: false, enactmentDays: 45, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 995 }),
                        Law("military_logistics", "military_admin", "军需", 2, 100f, 25f, 1f, 0f, 0f, 1.15f, initiallyKnown: false, enactmentDays: 60, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 997 }),
                        Law("military_justice", "military_admin", "军法", 2, 90f, 20f, 1f, 1f, 0.5f, 1.05f, initiallyKnown: false, enactmentDays: 45, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 505 }),
                        Law("command_hierarchy", "military_admin", "指挥体系", 3, 150f, 35f, 1f, 0.5f, 0f, 1.25f, initiallyKnown: false, enactmentDays: 75, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 995 }),
                        Law("military_training", "military_admin", "军训", 2, 70f, 15f, 1f, 0f, 0f, 1.1f, initiallyKnown: true, enactmentDays: 30, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 995 }),
                        Law("central_command", "military_admin", "中央统一调遣", 3, 150f, 35f, 1f, 0.5f, 0f, 1.25f, initiallyKnown: false, enactmentDays: 75, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 995 }),
                        Law("theater_autonomy", "military_admin", "战区自主", 3, 160f, 35f, 1f, 0f, 0f, 1.2f, initiallyKnown: false, enactmentDays: 90, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 960 }),
                        Law("frontier_discretion", "military_admin", "边将便宜行事", 3, 170f, 40f, 1f, -0.5f, 0f, 1.3f, initiallyKnown: false, enactmentDays: 90, requiredInnovationIds: new List<int> { 999 })
                    }
                },
                new LawGroupDef
                {
                    groupId = "border",
                    name = "边境",
                    order = 22,
                    laws = new List<LawDef>
                    {
                        Law("border_garrison", "border", "边防驻军", 2, 110f, 25f, 1f, 0.5f, 0f, 1.15f, initiallyKnown: false, enactmentDays: 60, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 999 }),
                        Law("frontier_march", "border", "边疆军镇", 3, 160f, 35f, 1f, 0f, 0f, 1.3f, initiallyKnown: false, enactmentDays: 90, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 960 }),
                        Law("border_trade", "border", "边市", 2, 90f, 20f, 1.1f, 1f, 0f, 1f, initiallyKnown: false, enactmentDays: 45, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 1008 }),
                        Law("border_pass", "border", "关津", 2, 80f, 20f, 1f, 0.5f, 0f, 1.05f, initiallyKnown: false, enactmentDays: 45, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 985 }),
                        Law("border_patrol", "border", "巡边", 2, 70f, 15f, 1f, 0f, 0f, 1.1f, initiallyKnown: false, enactmentDays: 45, authority: ProposalAuthority.CentralInstitution, requiredInnovationIds: new List<int> { 999 })
                    }
                }
            };
        }

        public static List<DecreeDef> CreateDecrees()
        {
            return new List<DecreeDef>
            {
                new DecreeDef
                {
                    decreeId = "decree_tax_relief",
                    name = "蠲免赋税",
                    description = "临时减轻赋税，安抚民心。",
                    costTreasury = 200f,
                    costAuthority = 10f,
                    durationDays = 180,
                    cooldownDays = 540,
                    requirements = new LawRequirement
                    {
                        minLegitimacy = 40f
                    },
                    effects = new LawEffect
                    {
                        taxMultiplier = 0.7f,
                        unrestPerYear = -3f,
                        stabilityPerYear = 2f
                    }
                },
                new DecreeDef
                {
                    decreeId = "decree_levy",
                    name = "加征",
                    description = "临时加征，充实国库，但激化不满。",
                    costTreasury = 0f,
                    costAuthority = 20f,
                    durationDays = 120,
                    cooldownDays = 720,
                    requirements = new LawRequirement
                    {
                        minLegitimacy = 30f,
                        requiredLawIds = new List<string> { "tax_heavy" }
                    },
                    effects = new LawEffect
                    {
                        taxMultiplier = 1.5f,
                        unrestPerYear = 5f,
                        stabilityPerYear = -3f
                    }
                }
            };
        }

        private static LawDef Law(string id, string groupId, string name, int tier, float costTreasury, float costPrestige, float tax = 1f, float stability = 0f, float legitimacy = 0f, float manpower = 1f, float unrest = 0f, string description = "", bool initiallyKnown = true, int enactmentDays = 30, ProposalAuthority authority = ProposalAuthority.HeadOfState, CentralInstitution requiredInstitution = CentralInstitution.None, List<int> requiredInnovationIds = null, MyriadCreation.Core.Enums.SocialIdentityFlags exemptIdentities = MyriadCreation.Core.Enums.SocialIdentityFlags.None)
        {
            return new LawDef
            {
                lawId = id,
                groupId = groupId,
                name = name,
                description = description,
                tier = tier,
                enactmentCostTreasury = costTreasury,
                enactmentCostPrestige = costPrestige,
                isInitiallyKnown = initiallyKnown,
                enactmentDays = enactmentDays,
                proposalAuthority = authority,
                requiredInstitution = requiredInstitution,
                exemptIdentities = exemptIdentities,
                requirements = new LawRequirement
                {
                    requiredInnovationIds = requiredInnovationIds ?? new List<int>()
                },
                effects = new LawEffect
                {
                    taxMultiplier = tax,
                    stabilityPerYear = stability,
                    legitimacyPerYear = legitimacy,
                    manpowerMultiplier = manpower,
                    unrestPerYear = unrest
                }
            };
        }
    }
}
