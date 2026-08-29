using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CivilizationEvolution.Race;
using CivilizationEvolution.Role;

namespace CivilizationEvolution.Tests
{
    /// <summary>
    /// DNA 与遗传系统 EditMode 测试
    /// 覆盖：生成完整性 / 孟德尔比例 / 表达方向 / 近亲系数 / 突变 / 近亲纯合修正
    /// 运行：Unity -batchmode -runTests -testPlatform EditMode
    /// </summary>
    public class DnaSystemTests
    {
        // ===== 生成 =====

        [Test]
        public void GenerateRandom_ProducesAllActiveLoci()
        {
            var dna = DnaSystem.GenerateRandom(null);
            Assert.AreEqual(DnaSystem.ActiveLoci.Length, dna.loci.Count, "应生成全部活跃基因座（变革性为种族设定，不设个体基因座）");
        }

        [Test]
        public void GenerateRandom_RespectsRaceFrequency()
        {
            // 高勇武频率种族：勇武座显性占比应显著高于 0.5
            var race = new RaceData
            {
                raceId = 99,
                locusFrequencies = new List<LocusFrequency>
                {
                    new LocusFrequency { locus = DnaLocus.Martial, dominantFrequency = 0.9f }
                }
            };
            int dominantCount = 0;
            const int N = 2000;
            for (int i = 0; i < N; i++)
            {
                var pair = DnaSystem.GenerateRandom(race).GetLocus(DnaLocus.Martial);
                if (pair.paternal == Allele.Dominant) dominantCount++;
                if (pair.maternal == Allele.Dominant) dominantCount++;
            }
            float ratio = dominantCount / (float)(N * 2);
            Assert.That(ratio, Is.GreaterThan(0.8f), $"频率 0.9 的显性占比应接近 0.9，实际 {ratio:F3}");
        }

        // ===== 孟德尔遗传 =====

        [Test]
        public void Inherit_AaCrossAa_ProducesExpectedRatio()
        {
            // Aa × Aa → AA 25% / Aa 50% / aa 25%（统计验证，±3% 容差）
            var father = MakeHeterozygous();
            var mother = MakeHeterozygous();

            int aa = 0, aA = 0, AA = 0;
            const int N = 20000;
            for (int i = 0; i < N; i++)
            {
                var pair = DnaSystem.Inherit(father, mother, null, 0f).GetLocus(DnaLocus.Longevity);
                if (pair.IsHomozygousRecessive) aa++;
                else if (pair.IsHeterozygous) aA++;
                else AA++;
            }
            Assert.That(aa / (float)N, Is.InRange(0.22f, 0.28f), $"aa 应约 25%，实际 {aa / (float)N:F4}");
            Assert.That(aA / (float)N, Is.InRange(0.47f, 0.53f), $"Aa 应约 50%，实际 {aA / (float)N:F4}");
            Assert.That(AA / (float)N, Is.InRange(0.22f, 0.28f), $"AA 应约 25%，实际 {AA / (float)N:F4}");
        }

        [Test]
        public void Inherit_AAxAa_NeverProducesRecessiveHomozygote()
        {
            // AA × Aa 不可能产生 aa（确定性断言，关闭突变）
            var father = new DnaData();
            var mother = MakeHeterozygous();
            foreach (DnaLocus l in System.Enum.GetValues(typeof(DnaLocus)))
                father.SetLocus(l, new LocusPair(Allele.Dominant, Allele.Dominant));

            for (int i = 0; i < 2000; i++)
            {
                var pair = DnaSystem.Inherit(father, mother, null, 0f, false).GetLocus(DnaLocus.Longevity);
                Assert.IsFalse(pair.IsHomozygousRecessive, "AA × Aa 后代不应出现 aa");
            }
        }

        [Test]
        public void Inherit_ParentAlleles_AreTransmitted()
        {
            // 纯合父母：后代该基因座必然纯合相同基因（关闭突变）
            var father = new DnaData();
            var mother = new DnaData();
            foreach (DnaLocus l in System.Enum.GetValues(typeof(DnaLocus)))
            {
                father.SetLocus(l, new LocusPair(Allele.Dominant, Allele.Dominant));
                mother.SetLocus(l, new LocusPair(Allele.Recessive, Allele.Recessive));
            }
            for (int i = 0; i < 500; i++)
            {
                var pair = DnaSystem.Inherit(father, mother, null, 0f, false).GetLocus(DnaLocus.Longevity);
                Assert.IsTrue(pair.IsHeterozygous, "AA × aa 后代应恒为 Aa");
            }
        }

        // ===== 表达 =====

        [Test]
        public void ComputeExpression_OffsetDirection_MatchesGenotype()
        {
            var race = new RaceData { lifespanRangeYears = 15f };
            // AA → 正向偏移
            var aa = new DnaData();
            foreach (DnaLocus l in System.Enum.GetValues(typeof(DnaLocus)))
                aa.SetLocus(l, new LocusPair(Allele.Dominant, Allele.Dominant));
            var exprAA = DnaSystem.ComputeExpression(aa, race);
            Assert.That(exprAA.longevityOffsetYears, Is.GreaterThan(0f), "AA 寿命偏移应为正");
            Assert.That(exprAA.intelligenceOffset, Is.GreaterThan(0f), "AA 智慧偏移应为正");

            // aa → 负向偏移
            var rr = new DnaData();
            foreach (DnaLocus l in System.Enum.GetValues(typeof(DnaLocus)))
                rr.SetLocus(l, new LocusPair(Allele.Recessive, Allele.Recessive));
            var exprRR = DnaSystem.ComputeExpression(rr, race);
            Assert.That(exprRR.longevityOffsetYears, Is.LessThan(0f), "aa 寿命偏移应为负");
            Assert.That(exprRR.intelligenceOffset, Is.LessThan(0f), "aa 智慧偏移应为负");

            // Aa → 正向且幅度小于 AA（取多次均值对比）
            float sumAA = 0f, sumAa = 0f;
            const int N = 500;
            for (int i = 0; i < N; i++)
            {
                sumAA += DnaSystem.ComputeExpression(aa, race).martialOffset;
                sumAa += DnaSystem.ComputeExpression(MakeHeterozygous(), race).martialOffset;
            }
            Assert.That(sumAa / N, Is.GreaterThan(0f), "Aa 勇武偏移均值应为正");
            Assert.That(sumAa / N, Is.LessThan(sumAA / N), "Aa 偏移均值应小于 AA");
        }

        [Test]
        public void ComputeExpression_Appearance_AlwaysTagged()
        {
            var dna = DnaSystem.GenerateRandom(null);
            var expr = DnaSystem.ComputeExpression(dna, null);
            Assert.IsFalse(string.IsNullOrEmpty(expr.appearanceTag), "外观表达不应为空");
        }

        [Test]
        public void ComputeExpression_TalentDefect_TableResolves()
        {
            // AA 触发天赋时 id 可查表；aa 触发遗传病时 id 可查表
            for (int i = 0; i < 300; i++)
            {
                var expr = DnaSystem.ComputeExpression(DnaSystem.GenerateRandom(null), null);
                if (!string.IsNullOrEmpty(expr.talentId))
                    Assert.IsNotNull(DnaSystem.FindDef(expr.talentId), $"未知天赋 id: {expr.talentId}");
                if (!string.IsNullOrEmpty(expr.defectId))
                    Assert.IsNotNull(DnaSystem.FindDef(expr.defectId), $"未知遗传病 id: {expr.defectId}");
            }
        }

        // ===== 近亲系数 =====

        [Test]
        public void CalculateInbreeding_ParentChild_IsQuarter()
        {
            var chars = new Dictionary<int, CharacterData>();
            var father = MakeChar(1, -1, -1);
            var child = MakeChar(2, 1, -1);
            chars[1] = father; chars[2] = child;
            Assert.That(DnaSystem.CalculateInbreeding(father, child, chars), Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void CalculateInbreeding_FullSiblings_IsQuarter()
        {
            var chars = new Dictionary<int, CharacterData>();
            var a = MakeChar(1, 10, 11);
            var b = MakeChar(2, 10, 11);
            chars[1] = a; chars[2] = b;
            chars[10] = MakeChar(10, -1, -1); // 父（谱系解析需要）
            chars[11] = MakeChar(11, -1, -1); // 母
            Assert.That(DnaSystem.CalculateInbreeding(a, b, chars), Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void CalculateInbreeding_HalfSiblings_IsOneEighth()
        {
            var chars = new Dictionary<int, CharacterData>();
            var a = MakeChar(1, 10, 11);
            var b = MakeChar(2, 10, 12);
            chars[1] = a; chars[2] = b;
            chars[10] = MakeChar(10, -1, -1); // 共同父亲
            chars[11] = MakeChar(11, -1, -1); // a 之母
            chars[12] = MakeChar(12, -1, -1); // b 之母
            Assert.That(DnaSystem.CalculateInbreeding(a, b, chars), Is.EqualTo(0.125f).Within(0.001f));
        }

        [Test]
        public void CalculateInbreeding_FirstCousins_IsOneSixteenth()
        {
            // 堂表：a 的父 c1、b 的父 c2，c1/c2 为全同胞（共同祖父母 g）
            var chars = new Dictionary<int, CharacterData>();
            var g = MakeChar(100, -1, -1);          // 祖辈
            var c1 = MakeChar(11, 100, 101);         // a 之父（与 c2 全同胞）
            var c2 = MakeChar(12, 100, 101);         // b 之父
            var a = MakeChar(1, 11, -1);
            var b = MakeChar(2, 12, -1);
            chars[100] = g; chars[11] = c1; chars[12] = c2; chars[1] = a; chars[2] = b;
            Assert.That(DnaSystem.CalculateInbreeding(a, b, chars), Is.EqualTo(0.0625f).Within(0.001f));
        }

        [Test]
        public void CalculateInbreeding_Unrelated_IsZero()
        {
            var chars = new Dictionary<int, CharacterData>();
            var a = MakeChar(1, -1, -1);
            var b = MakeChar(2, -1, -1);
            chars[1] = a; chars[2] = b;
            Assert.That(DnaSystem.CalculateInbreeding(a, b, chars), Is.EqualTo(0f));
        }

        // ===== 突变 =====

        [Test]
        public void Inherit_MutationRate_InExpectedBand()
        {
            var father = MakeHeterozygous();
            var mother = MakeHeterozygous();
            const int N = 20000;
            int mutatedLoci = 0;
            for (int i = 0; i < N; i++)
                mutatedLoci += DnaSystem.Inherit(father, mother, null, 0f).mutationCount;

            float avgMutations = mutatedLoci / (float)N;
            // 7 基因座 × 0.75% ≈ 0.0525；容差放宽至 0.02-0.09
            Assert.That(avgMutations, Is.InRange(0.02f, 0.09f), $"每代平均突变数应约 0.05，实际 {avgMutations:F4}");
        }

        // ===== 近亲纯合修正 =====

        [Test]
        public void Inherit_Inbreeding_IncreasesHomozygosity()
        {
            var father = MakeHeterozygous();
            var mother = MakeHeterozygous();

            int homoNormal = 0, homoInbred = 0;
            const int N = 20000;
            for (int i = 0; i < N; i++)
            {
                var p1 = DnaSystem.Inherit(father, mother, null, 0f).GetLocus(DnaLocus.Longevity);
                if (!p1.IsHeterozygous) homoNormal++;
                var p2 = DnaSystem.Inherit(father, mother, null, 0.25f).GetLocus(DnaLocus.Longevity);
                if (!p2.IsHeterozygous) homoInbred++;
            }
            Assert.That(homoInbred / (float)N, Is.GreaterThan(homoNormal / (float)N),
                $"近亲（0.25）后代纯合率应高于无近亲，实际 {homoNormal / (float)N:F4} vs {homoInbred / (float)N:F4}");
        }

        // ===== 辅助 =====

        private static DnaData MakeHeterozygous()
        {
            var dna = new DnaData();
            foreach (DnaLocus l in System.Enum.GetValues(typeof(DnaLocus)))
                dna.SetLocus(l, new LocusPair(Allele.Dominant, Allele.Recessive));
            return dna;
        }

        private static CharacterData MakeChar(int id, int fatherId, int motherId)
        {
            return new CharacterData
            {
                characterId = id,
                firstName = "T",
                lastName = "C",
                fatherId = fatherId,
                motherId = motherId
            };
        }
    }
}
