﻿using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.WorldState;



namespace CivilizationEvolution.Simulation.Economy
{
    /// <summary>
    /// 自然资源生成器。
    /// 在群系（Biomes）阶段之后、省份（Provinces）阶段之前运行：
    /// 遍历所有自然资源类物资（GoodsDef.isNaturalResource=true），
    /// 根据 originBiomes + spawnCondition + baseAbundance 在地块上生成资源点。
    /// </summary>
    public static class ResourceGenerator
    {
        /// <summary>
        /// 为整个世界生成自然资源点。
        /// 直接修改 world.tiles 中每个 TileData 的 resources 列表。
        /// </summary>
        public static void GenerateAll(GameWorld world)
        {
            if (world == null || world.tiles == null) return;

            var naturalGoods = GetNaturalGoods(world);
            if (naturalGoods.Count == 0)
            {
                Debug.LogWarning("[ResourceGenerator] 没有自然资源类物资定义（GoodsDef.isNaturalResource=true）");
                return;
            }

            int generated = 0;
            for (int i = 0; i < world.tiles.Length; i++)
            {
                ref var tile = ref world.tiles[i];
                if (!tile.exists || !tile.isLand) continue;

                if (tile.resources == null)
                    tile.resources = new List<TileResource>();
                else
                    tile.resources.Clear();

                foreach (var goods in naturalGoods)
                {
                    if (TrySpawnResource(world, tile, goods, out var resource))
                    {
                        tile.resources.Add(resource);
                        generated++;
                    }
                }
            }

            Debug.Log($"[ResourceGenerator] 自然资源点生成完成：共 {generated} 个资源点，涉及 {naturalGoods.Count} 种自然资源");
        }

        /// <summary>
        /// 尝试在单个地块上生成某种资源。
        /// 检查：群系匹配 → 地理条件 → 随机概率（受丰度和地理质量影响）。
        /// </summary>
        private static bool TrySpawnResource(GameWorld world, in TileData tile, GoodsDef goods, out TileResource resource)
        {
            resource = default;

            // 1. 群系匹配：地块群系必须在 originBiomes 中
            if (goods.originBiomes == null || goods.originBiomes.Count == 0) return false;
            if (!goods.originBiomes.Contains(tile.biome)) return false;

            // 2. 地理条件检查
            if (!CheckSpawnCondition(tile, goods.spawnCondition)) return false;

            // 3. 地理质量修正（优质地理条件提高生成概率和丰度）
            float quality = CalculateGeographicQuality(tile, goods);

            // 4. 随机概率 = baseAbundance × quality
            float chance = goods.baseAbundance * quality;
            if (Random.value > chance) return false;

            // 5. 生成资源点，丰度 = baseAbundance × quality × 随机浮动
            float abundance = Mathf.Clamp01(goods.baseAbundance * quality * Random.Range(0.7f, 1.3f));
            resource = TileResource.Create(goods.goodsId, abundance);
            return true;
        }

        /// <summary>检查地块是否满足资源生成的地理条件</summary>
        private static bool CheckSpawnCondition(in TileData tile, ResourceSpawnCondition cond)
        {
            if (tile.elevation01 < cond.minElevation) return false;
            if (tile.elevation01 > cond.maxElevation) return false;
            if (tile.annualPrecipMm < cond.minPrecipitation) return false;
            if (tile.annualPrecipMm > cond.maxPrecipitation) return false;
            if (tile.annualTemp < cond.minTemperature) return false;
            if (tile.annualTemp > cond.maxTemperature) return false;
            if (tile.slopeDegree > cond.maxSlope) return false;
            if (cond.requiresCoast && !tile.isCoast) return false;
            if (cond.requiresRiver && !tile.isRiver) return false;
            return true;
        }

        /// <summary>
        /// 计算地块对某种资源的地理质量（0-1）。
        /// 越接近该资源的理想条件，质量越高，生成概率和丰度越高。
        /// </summary>
        private static float CalculateGeographicQuality(in TileData tile, GoodsDef goods)
        {
            float quality = 1f;

            // 肥力对植物/农业资源有加成
            if (goods.resourceType == ResourceType.WildPlant)
            {
                quality *= 0.5f + tile.fertility * 0.5f;
            }

            // 坡度对矿物有加成（山地矿物更丰富）
            if (goods.resourceType == ResourceType.Mineral)
            {
                quality *= 0.6f + Mathf.Clamp01(tile.slopeDegree / 45f) * 0.4f;
            }

            // 海岸/河流对水产有加成
            if (goods.resourceType == ResourceType.Aquatic)
            {
                if (tile.isCoast || tile.isRiver) quality *= 1.2f;
                else quality *= 0.5f;
            }

            return Mathf.Clamp01(quality);
        }

        /// <summary>从GameWorld获取所有自然资源类物资</summary>
        private static List<GoodsDef> GetNaturalGoods(GameWorld world)
        {
            var result = new List<GoodsDef>();
            if (world == null || world.goodsDefs == null) return result;

            foreach (var kv in world.goodsDefs)
            {
                if (kv.Value.isNaturalResource)
                    result.Add(kv.Value);
            }
            return result;
        }
    }
}
