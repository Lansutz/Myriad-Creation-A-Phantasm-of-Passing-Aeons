using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;




namespace CivilizationEvolution.Simulation.Population
{
 /// 人口三维占比统计（阶层/文化/信仰——count 加权——传播机制数据基础）
 /// 主流=count 最大块——文化/宗教地图按主流着色——面板显示占比明细
    public static class PopulationStats
    {
 /// <summary>该文化占比（0-1——count 加权）</summary>
        public static float GetCultureShare(TileData tile, int cultureId)
        {
            float total = 0f, match = 0f;
            if (tile.populationBlocks == null) return 0f;
            foreach (var pb in tile.populationBlocks)
            {
                total += pb.count;
                if (pb.cultureId == cultureId) match += pb.count;
            }
            return total > 0f ? match / total : 0f;
        }

 /// <summary>该信仰占比（0-1）</summary>
        public static float GetFaithShare(TileData tile, int faithId)
        {
            float total = 0f, match = 0f;
            if (tile.populationBlocks == null) return 0f;
            foreach (var pb in tile.populationBlocks)
            {
                total += pb.count;
                if (pb.faithId == faithId) match += pb.count;
            }
            return total > 0f ? match / total : 0f;
        }

 /// <summary>该阶层占比（0-1）</summary>
        public static float GetClassShare(TileData tile, GameEnums.SocialClass socialClass)
        {
            float total = 0f, match = 0f;
            if (tile.populationBlocks == null) return 0f;
            foreach (var pb in tile.populationBlocks)
            {
                total += pb.count;
                if (pb.socialClass == socialClass) match += pb.count;
            }
            return total > 0f ? match / total : 0f;
        }

 /// <summary>主流文化（count 最大块——-1 无）</summary>
        public static int GetDominantCulture(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return -1;
            var best = tile.populationBlocks[0];
            foreach (var pb in tile.populationBlocks)
                if (pb.count > best.count) best = pb;
            return best.cultureId;
        }

 /// <summary>主流信仰（count 最大块——-1 无）</summary>
        public static int GetDominantFaith(TileData tile)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return -1;
            var best = tile.populationBlocks[0];
            foreach (var pb in tile.populationBlocks)
                if (pb.count > best.count) best = pb;
            return best.faithId;
        }

 /// <summary>占比明细文本（地块信息面板——"佛教 70% · 原始崇拜 30%"）</summary>
        public static string BuildShareText(TileData tile, System.Func<int, string> cultureName, System.Func<int, string> faithName)
        {
            if (tile.populationBlocks == null || tile.populationBlocks.Count == 0) return "";
            var parts = new System.Collections.Generic.List<string>();
            var seenCulture = new System.Collections.Generic.HashSet<int>();
            var seenFaith = new System.Collections.Generic.HashSet<int>();
            foreach (var pb in tile.populationBlocks)
            {
                if (pb.cultureId >= 0 && seenCulture.Add(pb.cultureId))
                    parts.Add($"{cultureName(pb.cultureId)} {GetCultureShare(tile, pb.cultureId) * 100f:F0}%");
            }
            var faithParts = new System.Collections.Generic.List<string>();
            foreach (var pb in tile.populationBlocks)
            {
                if (pb.faithId >= 0 && seenFaith.Add(pb.faithId))
                    faithParts.Add($"{faithName(pb.faithId)} {GetFaithShare(tile, pb.faithId) * 100f:F0}%");
            }
            return $"文化：{string.Join(" · ", parts)}｜信仰：{string.Join(" · ", faithParts)}";
        }
    }
}
