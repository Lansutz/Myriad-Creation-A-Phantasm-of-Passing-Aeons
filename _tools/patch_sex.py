from pathlib import Path

p = Path('MyriadCreation/Assets/Scripts/Simulation/Genetics/GeneticsEngine.cs')
t = p.read_text(encoding='utf-8')
start = t.find('        public static GenomeInstance CreateFounder(GenomeDef genome, GeneticSex sex, Random rng)')
end = t.find('        /// <summary>\n        /// 基因型 → 表型期望值。', start)
block = r'''        public static GenomeInstance CreateFounder(GenomeDef genome, GeneticSex sex, Random rng)
        {
            if (genome == null) throw new ArgumentNullException(nameof(genome));
            rng ??= new Random(1);
            int n = genome.loci.Count;
            var system = genome.sexSystem?.system ?? SexDeterminationSystem.XY;
            bool haploidMale = system == SexDeterminationSystem.Haplodiploid && IsMale(sex);

            var instance = new GenomeInstance
            {
                genomeId = genome.genomeId,
                sex = sex,
                ploidy = haploidMale ? 1 : 2,
                paternalAlleles = new int[n],
                maternalAlleles = new int[n],
            };

            for (int i = 0; i < n; i++)
            {
                var locus = genome.loci[i];
                int a = PickAllele(locus, rng);
                int b = PickAllele(locus, rng);
                ApplyFounderSlots(system, sex, locus.inheritance, ref a, ref b, rng);
                if (haploidMale)
                {
                    instance.paternalAlleles[i] = -1;
                    instance.maternalAlleles[i] = a >= 0 ? a : b;
                }
                else
                {
                    instance.paternalAlleles[i] = a;
                    instance.maternalAlleles[i] = b;
                }
            }
            return instance;
        }

        /// <summary>按物种性别决定机制随机选择表现型性别。</summary>
        public static GenomeInstance CreateFounder(GenomeDef genome, Random rng)
            => CreateFounder(genome, DetermineSex(genome, rng), rng);

        public static GeneticSex DetermineSex(GenomeDef genome, Random rng, float? environmentalBias = null)
        {
            rng ??= new Random(1);
            var system = genome?.sexSystem?.system ?? SexDeterminationSystem.XY;
            switch (system)
            {
                case SexDeterminationSystem.Hermaphroditic:
                    return GeneticSex.Hermaphrodite;
                case SexDeterminationSystem.Environmental:
                    float bias = environmentalBias ?? genome?.sexSystem?.environmentalFemaleBias ?? 0.5f;
                    return rng.NextDouble() < bias ? GeneticSex.Female : GeneticSex.Male;
                case SexDeterminationSystem.Haplodiploid:
                    return rng.Next(2) == 0 ? GeneticSex.Female : GeneticSex.Male;
                default:
                    return rng.Next(2) == 0 ? GeneticSex.Female : GeneticSex.Male;
            }
        }

        private static void ApplyFounderSlots(SexDeterminationSystem system, GeneticSex sex,
            GeneticInheritance inheritance, ref int paternal, ref int maternal, Random rng)
        {
            switch (inheritance)
            {
                case GeneticInheritance.XLinked:
                    if (system == SexDeterminationSystem.XY || system == SexDeterminationSystem.XO)
                    {
                        if (IsMale(sex)) { paternal = -1; }
                    }
                    else if (system == SexDeterminationSystem.ZW)
                    {
                        if (IsFemale(sex)) { maternal = -1; }
                    }
                    break;
                case GeneticInheritance.YLinked:
                    if (system == SexDeterminationSystem.XY)
                    {
                        if (IsMale(sex)) maternal = -1;
                        else { paternal = -1; maternal = -1; }
                    }
                    else if (system == SexDeterminationSystem.ZW)
                    {
                        if (IsFemale(sex)) paternal = -1;
                        else { paternal = -1; maternal = -1; }
                    }
                    break;
                case GeneticInheritance.Mitochondrial:
                    paternal = -1;
                    break;
            }
        }

        /// <summary>
        /// 生育：父本、母本各产生单倍体配子，融合后得到子代。
        /// 子代性别由物种的 SexDeterminationSystem 决定。
        /// </summary>
        public static GenomeInstance Reproduce(GenomeDef genome, GenomeInstance father, GenomeInstance mother,
            Random rng, bool allowMutation = true)
        {
            return Reproduce(genome, father, mother, DetermineSex(genome, rng), rng, allowMutation);
        }

        public static GenomeInstance Reproduce(GenomeDef genome, GenomeInstance father, GenomeInstance mother,
            GeneticSex childSex, Random rng, bool allowMutation = true)
        {
            if (genome == null) throw new ArgumentNullException(nameof(genome));
            if (father == null || mother == null) throw new ArgumentNullException("Parent genome is required.");
            rng ??= new Random(1);

            int n = genome.loci.Count;
            var system = genome.sexSystem?.system ?? SexDeterminationSystem.XY;
            bool haploidMale = system == SexDeterminationSystem.Haplodiploid && IsMale(childSex);

            int[] paternal = haploidMale ? Empty(n) : BuildGamete(genome, father, isFather: true, childSex, rng);
            int[] maternal = BuildGamete(genome, mother, isFather: false, childSex, rng);

            var child = new GenomeInstance
            {
                genomeId = genome.genomeId,
                sex = childSex,
                ploidy = haploidMale ? 1 : 2,
                paternalAlleles = paternal,
                maternalAlleles = maternal,
                inbreedingCoefficient = Clamp01((father.inbreedingCoefficient + mother.inbreedingCoefficient) * 0.5f),
                generation = Math.Max(father.generation, mother.generation) + 1,
            };

            if (allowMutation)
            {
                for (int i = 0; i < n; i++)
                {
                    var locus = genome.loci[i];
                    float rate = Math.Max(0f, locus.mutationRatePerGeneration);
                    if (rng.NextDouble() < rate && child.paternalAlleles[i] >= 0)
                        child.paternalAlleles[i] = MutateAllele(locus, child.paternalAlleles[i], rng, ref child.mutationCount);
                    if (rng.NextDouble() < rate && child.maternalAlleles[i] >= 0)
                        child.maternalAlleles[i] = MutateAllele(locus, child.maternalAlleles[i], rng, ref child.mutationCount);
                }
            }

            return child;
        }

        private static int[] Empty(int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = -1;
            return a;
        }

        /// <summary>产生一个单倍体配子；返回按基因座排列的等位基因索引数组，缺失为 -1。</summary>
        public static int[] BuildGamete(GenomeDef genome, GenomeInstance parent, bool isFather,
            GeneticSex childSex, Random rng)
        {
            int n = genome.loci.Count;
            var gamete = new int[n];
            for (int i = 0; i < n; i++) gamete[i] = -1;
            var system = genome.sexSystem?.system ?? SexDeterminationSystem.XY;

            var byChromosome = new Dictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                var locus = genome.loci[i];
                if (locus.inheritance != GeneticInheritance.Autosomal) continue;
                if (!byChromosome.TryGetValue(locus.chromosome, out var list))
                {
                    list = new List<int>();
                    byChromosome[locus.chromosome] = list;
                }
                list.Add(i);
            }

            foreach (var kv in byChromosome)
            {
                var indices = kv.Value;
                indices.Sort((a, b) => genome.loci[a].mapPositionCm.CompareTo(genome.loci[b].mapPositionCm));
                int currentHomolog = rng.Next(2);
                for (int k = 0; k < indices.Count; k++)
                {
                    int locusIndex = indices[k];
                    if (k > 0)
                    {
                        float distance = genome.loci[indices[k]].mapPositionCm
                                         - genome.loci[indices[k - 1]].mapPositionCm;
                        if (rng.NextDouble() < RecombinationFraction(distance))
                            currentHomolog = 1 - currentHomolog;
                    }
                    gamete[locusIndex] = currentHomolog == 0
                        ? parent.PaternalAt(locusIndex)
                        : parent.MaternalAt(locusIndex);
                }
            }

            for (int i = 0; i < n; i++)
            {
                var locus = genome.loci[i];
                switch (locus.inheritance)
                {
                    case GeneticInheritance.XLinked:
                        gamete[i] = PassSexLinked(genome, parent, isFather, childSex, i, rng);
                        break;
                    case GeneticInheritance.YLinked:
                        gamete[i] = PassHeterogameticLinked(system, parent, isFather, childSex, i);
                        break;
                    case GeneticInheritance.Mitochondrial:
                        gamete[i] = isFather ? -1 : FirstPresent(parent.MaternalAt(i), parent.PaternalAt(i));
                        break;
                }
            }
            return gamete;
        }

        private static int PassSexLinked(GenomeDef genome, GenomeInstance parent, bool isFather,
            GeneticSex childSex, int locus, Random rng)
        {
            var system = genome.sexSystem?.system ?? SexDeterminationSystem.XY;
            switch (system)
            {
                case SexDeterminationSystem.XY:
                case SexDeterminationSystem.XO:
                    if (isFather) return IsFemale(childSex) ? PickHomolog(parent, locus, rng) : -1;
                    return PickHomolog(parent, locus, rng);
                case SexDeterminationSystem.ZW:
                    if (isFather) return PickHomolog(parent, locus, rng);
                    return IsMale(childSex) ? FirstPresent(parent.PaternalAt(locus), parent.MaternalAt(locus)) : -1;
                case SexDeterminationSystem.Haplodiploid:
                    if (isFather) return IsFemale(childSex) ? PickHomolog(parent, locus, rng) : -1;
                    return FirstPresent(parent.PaternalAt(locus), parent.MaternalAt(locus));
                default:
                    return PickHomolog(parent, locus, rng);
            }
        }

        private static int PassHeterogameticLinked(SexDeterminationSystem system, GenomeInstance parent,
            bool isFather, GeneticSex childSex, int locus)
        {
            switch (system)
            {
                case SexDeterminationSystem.XY:
                    return isFather && IsMale(childSex)
                        ? FirstPresent(parent.PaternalAt(locus), parent.MaternalAt(locus))
                        : -1;
                case SexDeterminationSystem.ZW:
                    return !isFather && IsFemale(childSex)
                        ? FirstPresent(parent.MaternalAt(locus), parent.PaternalAt(locus))
                        : -1;
                default:
                    return -1;
            }
        }

        private static int PickHomolog(GenomeInstance parent, int locus, Random rng)
        {
            int paternal = parent.PaternalAt(locus);
            int maternal = parent.MaternalAt(locus);
            if (paternal < 0) return maternal;
            if (maternal < 0) return paternal;
            return rng.Next(2) == 0 ? paternal : maternal;
        }

        private static bool IsMale(GeneticSex sex) =>
            sex == GeneticSex.Male || sex == GeneticSex.Drone;

        private static bool IsFemale(GeneticSex sex) =>
            sex == GeneticSex.Female || sex == GeneticSex.Worker || sex == GeneticSex.Queen;

'''
if start >= 0 and end > start:
    t = t[:start] + block + t[end:]
    p.write_text(t, encoding='utf-8', newline='\n')
    print('engine sex systems')
else:
    print('MISS')
