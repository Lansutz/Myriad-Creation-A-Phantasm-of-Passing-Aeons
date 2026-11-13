using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 城防等级（0–15）：5 个 tier × 3 档材质工艺。
    /// 等级只表示综合城防，不表示材质；材质单独放在 WallMaterial。
    ///
    /// tier 1：简易围护（palissade/earthwork）
    /// tier 2：连续墙体（夯土/石/砖）
    /// tier 3：设防墙体（城门/塔楼/护城河）
    /// tier 4：棱堡/多面堡
    /// tier 5：多重防线/巨型要塞
    /// </summary>
    public static class FortificationLevels
    {
        public const int None = 0;
        public const int Max = 15;

        public const int Palisade = 1;
        public const int EarthenRampart = 4;
        public const int StoneWall = 5;
        public const int FortifiedWall = 7;
        public const int StarFortification = 10;
        public const int MegaFortification = 13;

        public const int MinTier = 1;
        public const int MaxTier = 5;
        public const int MinMaterialGrade = 1;
        public const int MaxMaterialGrade = 3;

        /// <summary>tier 1..5 + 材质工艺 1..3 → 等级 1..15。</summary>
        public static int Compose(int tier, int materialGrade)
        {
            int safeTier = Math.Min(MaxTier, Math.Max(MinTier, tier));
            int safeGrade = Math.Min(MaxMaterialGrade, Math.Max(MinMaterialGrade, materialGrade));
            return (safeTier - 1) * 3 + safeGrade;
        }

        /// <summary>等级 → tier；0 返回 0。</summary>
        public static int TierOf(int level)
        {
            int safe = Clamp(level);
            return safe <= 0 ? 0 : (safe - 1) / 3 + 1;
        }

        /// <summary>等级 → 材质工艺档位；0 返回 0。</summary>
        public static int GradeOf(int level)
        {
            int safe = Clamp(level);
            return safe <= 0 ? 0 : (safe - 1) % 3 + 1;
        }

        public static int Clamp(int level)
        {
            return Math.Min(Max, Math.Max(None, level));
        }

        public static int ClampMaterialGrade(int grade)
        {
            return Math.Min(MaxMaterialGrade, Math.Max(MinMaterialGrade, grade));
        }

        public static WallMaterial DefaultMaterialForLevel(int level)
        {
            int tier = TierOf(level);
            if (tier <= 1) return WallMaterial.Palisade;
            if (tier == 2) return WallMaterial.Earth;
            if (tier == 3) return WallMaterial.Stone;
            if (tier == 4) return WallMaterial.Brick;
            return WallMaterial.Composite;
        }

        /// <summary>
        /// 旧 WallLevel → 新 0–15 映射。
        /// 旧值：None=0/Palisade=1/EarthenRampart=2/StoneWall=3/FortifiedWall=4/StarFortification=5/MegaFortification=6。
        /// </summary>
        public static int FromLegacy(WallLevel legacy)
        {
            switch (legacy)
            {
                case WallLevel.None: return None;
                case WallLevel.Palisade: return Palisade;
                case WallLevel.EarthenRampart: return EarthenRampart;
                case WallLevel.StoneWall: return StoneWall;
                case WallLevel.FortifiedWall: return FortifiedWall;
                case WallLevel.StarFortification: return StarFortification;
                case WallLevel.MegaFortification: return MegaFortification;
                default: return None;
            }
        }
    }
}
