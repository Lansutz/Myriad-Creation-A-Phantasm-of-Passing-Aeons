using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
 /// GovernmentConstraints.SubOptions —— 条件子选项组定义与活跃过滤（各维度的下拉选项数据）（partial static class）    public static partial class GovernmentConstraints
    {

 /// <summary>获取某维度的所有条件子选项组</summary>        public static List<SubOptionGroup> GetSubOptionGroups(GovernmentDimension dimension)
        {
            var groups = new List<SubOptionGroup>();

            switch (dimension)
            {
 // A1 最高权力交接的子选项                case GovernmentDimension.SupremeSuccession:
 // 世袭 → 继承法四轴（多轴并行，不是单选）                    groups.Add(new SubOptionGroup
                    {
                        groupName = "继承法（四轴）",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.Hereditary,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "范围轴", value = 0, description = "限于本族 / 血亲不论姓氏" },
                            new SubOption { name = "支系轴", value = 1, description = "长支优先 / 兄终弟及" },
                            new SubOption { name = "性别轴", value = 2, description = "仅男/男优/平等/女优/仅女" },
                            new SubOption { name = "长幼轴", value = 3, description = "年长者先 / 年幼者先" }
                        }
                    });
 // 选举君主 → 两轴：身份范围 + 性别（区别于世袭的血缘范围四轴）                    groups.Add(new SubOptionGroup
                    {
                        groupName = "选举范围（身份）",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveDirect,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "全体公民", value = 0, description = "所有公民直接投票（雅典式）" },
                            new SubOption { name = "公民大会", value = 1, description = "公民大会代表选举" },
                            new SubOption { name = "部落联盟", value = 2, description = "各部落代表选举" },
                            new SubOption { name = "自由民", value = 3, description = "全体自由民选举" }
                        }
                    });
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "性别资格",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveDirect,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "仅男性", value = 0, description = "只有男性可被选举" },
                            new SubOption { name = "男性优先", value = 1, description = "男性优先，女性可被选举" },
                            new SubOption { name = "男女平等", value = 2, description = "男女平等选举权" },
                            new SubOption { name = "女性优先", value = 3, description = "女性优先" },
                            new SubOption { name = "仅女性", value = 4, description = "只有女性可被选举" }
                        }
                    });
 // 推举君主 → 两轴：推举主体身份 + 性别                    groups.Add(new SubOptionGroup
                    {
                        groupName = "推举主体（身份）",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveRepresentative,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "贵族推举", value = 0, description = "贵族阶层推举（波兰式）" },
                            new SubOption { name = "元老院推举", value = 1, description = "元老院推举（罗马式）" },
                            new SubOption { name = "选帝侯", value = 2, description = "选帝侯推举（神罗式）" },
                            new SubOption { name = "军队推举", value = 3, description = "军队推举（罗马禁卫军式）" },
                            new SubOption { name = "教会推举", value = 4, description = "教阶推举（教皇选举式）" }
                        }
                    });
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "性别资格",
                        parentDimension = "SupremeSuccession",
                        parentValue = (int)SupremeSuccession.ElectiveRepresentative,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "仅男性", value = 0, description = "只有男性可被推举" },
                            new SubOption { name = "男性优先", value = 1, description = "男性优先，女性可被推举" },
                            new SubOption { name = "男女平等", value = 2, description = "男女平等被推举权" },
                            new SubOption { name = "女性优先", value = 3, description = "女性优先" },
                            new SubOption { name = "仅女性", value = 4, description = "只有女性可被推举" }
                        }
                    });
                    break;

 // A2 最高权力分配的子选项（头衔分配+领地分配，双轴并行） // 头衔分配根据A0最高权力归属（君主制/共和制）显示不同选项                case GovernmentDimension.SupremeScope:
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "头衔分配",
                        parentDimension = "SupremeScope",
                        parentValue = -1,  // 始终显示，但选项根据A0过滤
                        options = new List<SubOption>
                        {
 // 君主制选项                            new SubOption { name = "独享（君主制）", value = 0, description = "一人独占最高头衔——君主制" },
                            new SubOption { name = "家族共享（君主制）", value = 1, description = "法兰克人式，家族共享最高头衔——君主制" },
 // 共和制选项                            new SubOption { name = "有最高头衔（共和制）", value = 2, description = "有最高头衔如执政官/独裁官——共和制" },
                            new SubOption { name = "无最高头衔（共和制）", value = 3, description = "无最高头衔，中央机构为主导如元老院——共和制" }
                        }
                    });
                    groups.Add(new SubOptionGroup
                    {
                        groupName = "领地分配",
                        parentDimension = "SupremeScope",
                        parentValue = -1,  // 始终显示
                        options = new List<SubOption>
                        {
                            new SubOption { name = "独享", value = 0, description = "一人独占所有领地" },
                            new SubOption { name = "均分", value = 1, description = "诸子均分领地" },
                            new SubOption { name = "采邑", value = 2, description = "嫡长子继承核心，其余分封采邑" }
                        }
                    });
                    break;

 // B2 中央权力机构的子选项（只有B0=有常设时才显示）                case GovernmentDimension.CentralInstitution:
 // 议会 → 议会构成                    groups.Add(new SubOptionGroup
                    {
                        groupName = "议会构成",
                        parentDimension = "CentralInstitution",
                        parentValue = (int)CentralInstitution.Assembly,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "一院制", value = 0, description = "单一代表院（雅典公民大会/罗马元老院）" },
                            new SubOption { name = "两院制", value = 1, description = "贵族院+平民院（英国上下院）" },
                            new SubOption { name = "等级会议", value = 2, description = "按等级分庭（法国三级会议/神罗帝国议会）" }
                        }
                    });
 // 长老议事会 → 长老构成                    groups.Add(new SubOptionGroup
                    {
                        groupName = "长老构成",
                        parentDimension = "CentralInstitution",
                        parentValue = (int)CentralInstitution.EldersCouncil,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "氏族长老", value = 0, description = "各氏族长老组成" },
                            new SubOption { name = "贵族长老", value = 1, description = "贵族阶层长老组成" },
                            new SubOption { name = "功勋长老", value = 2, description = "按功勋选拔的长老" }
                        }
                    });
 // 官僚中枢 → 官僚体系                    groups.Add(new SubOptionGroup
                    {
                        groupName = "官僚体系",
                        parentDimension = "CentralInstitution",
                        parentValue = (int)CentralInstitution.BureaucraticCore,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "三省六部", value = 0, description = "中式三省六部制" },
                            new SubOption { name = "三公九卿", value = 1, description = "中式三公九卿制" },
                            new SubOption { name = "部院制", value = 2, description = "近代部院制" }
                        }
                    });
                    break;

 // C1 地方权力交接的子选项（核心：封建vs官僚的子选项组互斥）                case GovernmentDimension.LocalSuccession:
 // 任命 → 任命主体（官僚体系的子选项）                    groups.Add(new SubOptionGroup
                    {
                        groupName = "任命主体",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Appointed,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "中央派任", value = 0, description = "中央派任流官/总督（郡县/行省）" },
                            new SubOption { name = "教区委任", value = 1, description = "教区委任（教区体系）" },
                            new SubOption { name = "军事任免", value = 2, description = "军事上级任免（军管区）" }
                        }
                    });
 // 世袭 → 领有身份（封建体系的子选项）                    groups.Add(new SubOptionGroup
                    {
                        groupName = "领有身份",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Hereditary,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "世袭封臣", value = 0, description = "封建契约——异姓功臣" },
                            new SubOption { name = "宗室采邑", value = 1, description = "分封宗亲——西周/阿拔斯" },
                            new SubOption { name = "军功领邑", value = 2, description = "战功封赏" }
                        }
                    });
 // 选举 → 选举范围                    groups.Add(new SubOptionGroup
                    {
                        groupName = "选举范围",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Elected,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "本地公民", value = 0, description = "本地公民选举" },
                            new SubOption { name = "部落推举", value = 1, description = "部落长老推举" },
                            new SubOption { name = "自治市议会", value = 2, description = "自治市议会选举" }
                        }
                    });
 // 考试 → 考试体系（官僚体系精英选拔）                    groups.Add(new SubOptionGroup
                    {
                        groupName = "考试体系",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.Examination,
                        options = new List<SubOption>
                        {
                            new SubOption { name = "科举制", value = 0, description = "中式科举——分科考试选拔文官" },
                            new SubOption { name = "文官考试", value = 1, description = "近代文官考试制度" },
                            new SubOption { name = "荐举制", value = 2, description = "地方荐举+中央考核（察举制）" },
                            new SubOption { name = "九品中正", value = 3, description = "中正官评定品级（魏晋式）" }
                        }
                    });
 // 城市特许 → 特许类型（可选项）                    groups.Add(new SubOptionGroup
                    {
                        groupName = "特许类型",
                        parentDimension = "LocalSuccession",
                        parentValue = (int)LocalSuccession.CityCharter,
                        isOptional = true,  // 可选项
                        options = new List<SubOption>
                        {
                            new SubOption { name = "自由市", value = 0, description = "完全自治自由市" },
                            new SubOption { name = "帝国自由市", value = 1, description = "直属于最高权力的自由市" },
                            new SubOption { name = "特许市镇", value = 2, description = "有限自治特许市镇" }
                        }
                    });
                    break;
            }

            return groups;
        }


 /// 获取当前激活的子选项组（基于当前政体组合） /// 同一维度内，只有选中的父选项对应的子选项组被激活 /// 不同父选项的子选项组互斥（不能同时激活） /// B2中央机构只有B0=有常设时才显示子选项        public static List<SubOptionGroup> GetActiveSubOptionGroups(
            GovernmentDimension dimension, GovernmentComposition comp)
        {
            var allGroups = GetSubOptionGroups(dimension);
            var currentPrimary = GetCurrentPrimary(dimension, comp);

 // B2中央机构：只有B0=有常设时才显示            if (dimension == GovernmentDimension.CentralInstitution)
            {
                if (comp.centralExistence != CentralExistence.Established)
                    return new List<SubOptionGroup>();  // 无常设中央机构，不显示子选项
            }

            var activeGroups = new List<SubOptionGroup>();
            foreach (var group in allGroups)
            {
 // parentValue=-1表示始终显示（如A2的头衔分配和领地分配）                if (group.parentValue == -1)
                {
                    group.isActive = true;
                    activeGroups.Add(group);
                    continue;
                }

 // 只有父选项被选中时，子选项组才激活                group.isActive = (group.parentValue == currentPrimary);
                if (group.isActive)
                    activeGroups.Add(group);
            }

            return activeGroups;
        }

    }
}
