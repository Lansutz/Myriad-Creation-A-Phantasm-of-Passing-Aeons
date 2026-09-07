using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Thought;
using CivilizationEvolution.War;

namespace CivilizationEvolution.Character
{
    /// <summary>
    /// CharacterManager.Family —— 家族与生育（生育/婚姻/亲属查询/家族树/自动生育/纽带）（partial class，与 CharacterSystem.cs 共享字段）
    /// </summary>
    public partial class CharacterManager
    {

        // ===== 生育机制（最小实现，DNA 孟德尔遗传入口） =====

        /// <summary>
        /// 生育：父+母 → 孟德尔遗传 DNA → 后代角色
        /// 校验：双方存活、异性、成年（≥16）、非同一人、非直系亲子
        /// 近亲允许（文化/法律层面决策），但近亲系数进入遗传（纯合隐性风险上升）
        /// 混血（父母不同种族）：后代种族取父系，表达基准取双亲种族平均
        /// </summary>
        public CharacterData Procreate(int fatherId, int motherId, int birthYear)
        {
            if (fatherId < 0 || motherId < 0) return null;
            var father = GetCharacter(fatherId);
            var mother = GetCharacter(motherId);
            if (father == null || mother == null) return null;
            if (!father.isAlive || !mother.isAlive) return null;
            if (father.isMale == mother.isMale) return null;
            if (father.characterId == mother.characterId) return null;
            if (father.age < 16 || mother.age < 16) return null;
            // 直系亲子排除
            if (IsDirectLineage(father, mother)) return null;

            // 后代身份：父系传承（最小规则）
            int childRaceId = father.raceId;
            int childCultureId = father.cultureId;
            int childFaithId = father.faithId;

            // 混血：表达基准取双亲种族平均（基因频率仍用父种族）
            var fatherRace = ResolveRace(father.raceId);
            var motherRace = ResolveRace(mother.raceId);
            RaceData expressionRace = fatherRace ?? motherRace;
            if (fatherRace != null && motherRace != null && fatherRace.raceId != motherRace.raceId)
                expressionRace = AverageRaceBaselines(fatherRace, motherRace);

            float inbreeding = DnaSystem.CalculateInbreeding(father, mother, _characters);
            var dna = DnaSystem.Inherit(father.dna, mother.dna, fatherRace ?? motherRace, inbreeding);

            bool isMale = UnityEngine.Random.value < 0.5f;
            string firstName = GenerateName(childCultureId, isMale ? 0 : 1);

            var child = CreateCharacter(firstName, father.lastName, 0, isMale,
                childCultureId, childRaceId, childFaithId, CharacterRole.Commoner,
                dna, fatherId, motherId, expressionRace);

            // 挂入父系家族
            if (father.familyId >= 0)
            {
                child.familyId = father.familyId;
                if (_families.TryGetValue(father.familyId, out var fam))
                    fam.AddMember(child.characterId);
            }

            if (inbreeding > 0.05f)
                Debug.Log($"[Character] {father.fullName} × {mother.fullName} 产子 {child.fullName}（近亲系数 {inbreeding:F3}）");
            return child;
        }


        /// <summary>统计角色的子女人数</summary>
        public int CountChildren(int characterId)
        {
            int count = 0;
            foreach (var c in _characters.Values)
                if (c.fatherId == characterId || c.motherId == characterId) count++;
            return count;
        }


        // ===== 婚姻与家族树（2026-09-01：配偶/子女遍历——家族树与生育衔接） =====

        /// <summary>婚姻：双向设置配偶（异性/成年/存活/非直系——与 Procreate 同检查）</summary>
        public bool Marry(int aId, int bId)
        {
            var a = GetCharacter(aId);
            var b = GetCharacter(bId);
            if (a == null || b == null) return false;
            if (!a.isAlive || !b.isAlive) return false;
            if (a.isMale == b.isMale) return false;
            if (a.characterId == b.characterId) return false;
            if (a.age < 16 || b.age < 16) return false;
            if (IsDirectLineage(a, b)) return false;
            if (a.spouseId >= 0 || b.spouseId >= 0) return false; // 已有配偶不重婚

            a.spouseId = b.characterId;
            b.spouseId = a.characterId;
            return true;
        }


        /// <summary>获取角色配偶（无返回 null）</summary>
        public CharacterData GetSpouse(int characterId)
        {
            var c = GetCharacter(characterId);
            return c != null && c.spouseId >= 0 ? GetCharacter(c.spouseId) : null;
        }


        /// <summary>获取子女（父或母=指定角色——反查）</summary>
        public List<CharacterData> GetChildren(int characterId)
        {
            var result = new List<CharacterData>();
            foreach (var c in _characters.Values)
                if (c.fatherId == characterId || c.motherId == characterId)
                    result.Add(c);
            return result;
        }


        /// <summary>获取兄弟姐妹（共享任一父母，排除自身）</summary>
        public List<CharacterData> GetSiblings(int characterId)
        {
            var c = GetCharacter(characterId);
            var result = new List<CharacterData>();
            if (c == null) return result;
            foreach (var other in _characters.Values)
            {
                if (other.characterId == characterId) continue;
                if ((c.fatherId >= 0 && other.fatherId == c.fatherId)
                    || (c.motherId >= 0 && other.motherId == c.motherId))
                    result.Add(other);
            }
            return result;
        }


        /// <summary>获取祖先链（父系优先递归，含父母/祖父母…）</summary>
        public List<CharacterData> GetAncestors(int characterId, int maxDepth = 4)
        {
            var result = new List<CharacterData>();
            var c = GetCharacter(characterId);
            int depth = 0;
            while (c != null && depth < maxDepth)
            {
                var parent = c.fatherId >= 0 ? GetCharacter(c.fatherId) : null;
                if (parent == null && c.motherId >= 0) parent = GetCharacter(c.motherId);
                if (parent == null) break;
                result.Add(parent);
                c = parent;
                depth++;
            }
            return result;
        }


        /// <summary>获取孙辈及以下（子女的子女——深度 2）</summary>
        public List<CharacterData> GetGrandchildren(int characterId)
        {
            var result = new List<CharacterData>();
            foreach (var child in GetChildren(characterId))
                result.AddRange(GetChildren(child.characterId));
            return result;
        }


        /// <summary>家族树文本（分代缩进：配偶/祖辈/本人/子女/孙辈——家族树面板用）</summary>
        public string BuildFamilyTreeText(int characterId)
        {
            var sb = new System.Text.StringBuilder();
            var c = GetCharacter(characterId);
            if (c == null) return "（无角色）";

            sb.AppendLine($"◆ {c.firstName} {c.lastName}（{c.age}岁，{(c.isMale ? "男" : "女")}）");

            // 配偶
            var spouse = GetSpouse(characterId);
            sb.AppendLine(spouse != null ? $"  配偶：{spouse.firstName} {spouse.lastName}（{spouse.age}岁）" : "  配偶：无");

            // 祖辈
            var ancestors = GetAncestors(characterId);
            if (ancestors.Count > 0)
            {
                sb.AppendLine("  祖辈：");
                foreach (var a in ancestors)
                    sb.AppendLine($"    - {a.firstName} {a.lastName}（{a.age}岁）");
            }

            // 父母（双亲显式列出——家族树含父系+母系）
            if (c.fatherId >= 0 || c.motherId >= 0)
            {
                sb.AppendLine("  父母：");
                if (c.fatherId >= 0)
                {
                    var f = GetCharacter(c.fatherId);
                    if (f != null) sb.AppendLine($"    父 - {f.firstName} {f.lastName}（{f.age}岁）");
                }
                if (c.motherId >= 0)
                {
                    var m = GetCharacter(c.motherId);
                    if (m != null) sb.AppendLine($"    母 - {m.firstName} {m.lastName}（{m.age}岁）");
                }
            }

            // 子女
            var children = GetChildren(characterId);
            if (children.Count > 0)
            {
                sb.AppendLine($"  子女（{children.Count}）：");
                foreach (var ch in children)
                    sb.AppendLine($"    - {ch.firstName} {ch.lastName}（{ch.age}岁，{(ch.isMale ? "男" : "女")}）");
            }

            // 孙辈
            var grands = GetGrandchildren(characterId);
            if (grands.Count > 0)
            {
                sb.AppendLine($"  孙辈（{grands.Count}）：");
                foreach (var g in grands)
                    sb.AppendLine($"    - {g.firstName} {g.lastName}（{g.age}岁）");
            }
            return sb.ToString();
        }


        /// <summary>自主生育（已婚夫妇优先——配偶生育；未婚保留随机配对，保证 DNA 遗传持续发生）</summary>
        private void AutoProcreate(int currentYear)
        {
            var males = new List<CharacterData>();
            var females = new List<CharacterData>();
            foreach (var c in _characters.Values)
            {
                if (!c.isAlive || c.age < 16 || c.age > 45) continue; // 育龄 16-45
                if (c.isMale) males.Add(c); else females.Add(c);
            }
            if (males.Count == 0 || females.Count == 0) return;

            // 已婚夫妇优先生育（spouseId 双向配对——婚姻制度的生育）
            foreach (var male in males)
            {
                if (male.spouseId < 0) continue;
                var wife = GetCharacter(male.spouseId);
                if (wife == null || !wife.isAlive) continue;
                if (wife.age < 16 || wife.age > 45) continue;
                if (CountChildren(male.characterId) >= 8) continue;
                if (CountChildren(wife.characterId) >= 8) continue;
                if (IsDirectLineage(male, wife)) continue;

                // 已婚夫妇每年约 25% 生育概率（≈0.0007/天——比未婚配对高）
                if (UnityEngine.Random.value < 0.0007f)
                {
                    var child = Procreate(male.characterId, wife.characterId, currentYear);
                    if (child != null)
                        Debug.Log($"[Character] 已婚生育：{male.firstName}×{wife.firstName} → {child.firstName} {child.lastName}");
                }
            }

            // 未婚随机配对（原有机制保留——简化社会未婚生育）
            foreach (var male in males)
            {
                if (male.spouseId >= 0) continue; // 已婚不再随机配对
                if (CountChildren(male.characterId) >= 8) continue;

                foreach (var female in females)
                {
                    if (female.realmId != male.realmId) continue;
                    if (CountChildren(female.characterId) >= 8) continue;
                    if (IsDirectLineage(male, female)) continue;

                    // 每年约 15% 生育概率（≈0.0004/天）
                    if (UnityEngine.Random.value < 0.0004f)
                    {
                        Procreate(male.characterId, female.characterId, currentYear);
                        break; // 每轮每名男性至多一个孩子
                    }
                }
            }
        }


        /// <summary>创建家族</summary>
        public FamilyNode CreateFamily(string familyName, int founderId, int foundingYear, int realmId = -1)
        {
            var family = new FamilyNode
            {
                familyId = _nextFamilyId++,
                familyName = familyName,
                founderCharacterId = founderId,
                foundingYear = foundingYear,
                holderRealmId = realmId,
                Innovations = Innovations // 传递革新树引用（家族传统解锁前置检查）
            };
            family.AddMember(founderId);
            _families[family.familyId] = family;

            if (_characters.TryGetValue(founderId, out var founder))
                founder.familyId = family.familyId;

            return family;
        }


        /// <summary>创建羁绊</summary>
        public CharacterBond CreateBond(int charAId, int charBId, BondType type)
        {
            var bond = new CharacterBond
            {
                bondId = _nextBondId++,
                characterAId = charAId,
                characterBId = charBId,
                type = type,
                establishedDay = 0
            };
            _bonds.Add(bond);
            return bond;
        }

        public IReadOnlyList<CharacterBond> GetAllBonds() => _bonds;

    }
}
