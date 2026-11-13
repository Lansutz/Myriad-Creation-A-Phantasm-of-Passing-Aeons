using System;
using System.Collections.Generic;
using MyriadCreation.Save.Contracts;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// `SettlementBlock` 与 `SettlementBlockSaveData` 的映射（**状态所有者侧**，递归处理 children）。
    ///
    /// 与其它 mapping 一致：DTO 是纯字段（`MyriadCreation.Save.Contracts`），
    /// 这里负责捕获/还原，存档层不再引用 World。
    /// `ToBlock` 写成扩展方法，调用点 `dto.ToBlock()` 保持不变。
    /// </summary>
    public static class SettlementBlockSaveMapping
    {
        public static SettlementBlockSaveData From(SettlementBlock block)
        {
            return From(block, string.Empty);
        }

        private static SettlementBlockSaveData From(SettlementBlock block, string parentBlockId)
        {
            if (block == null) return null;
            return new SettlementBlockSaveData
            {
                block_id = block.blockId,
                display_name = block.displayName,
                builderCharacterId = block.builderCharacterId,
                kind = (int)block.kind,
                subtype = (int)block.subtype,
                enabled = block.enabled,
                level = block.level,
                condition = block.condition,
                hasSpatialData = block.hasSpatialData,
                areaRatio = block.areaRatio,
                populationRatio = block.populationRatio,
                development = block.development,
                wealth = block.wealth,
                length = block.length,
                districtType = (int)block.districtType,
                buildingDefId = block.buildingDefId,
                wallLayerCount = block.wallLayerCount,
                wallMoatLevel = block.wallMoatLevel,
                wallRole = (int)block.wallRole,
                wallLayout = (int)block.wallLayout,
                wallDataVersion = 1,
                wallMaterial = (int)block.wallMaterial,
                wallMaterialGrade = block.wallMaterialGrade,
                wallSegments = WallSegmentSaveMapping.CaptureAll(block.wallSegments),
                parent_block_id = parentBlockId ?? string.Empty,
            };
        }

        public static SettlementBlock ToBlock(this SettlementBlockSaveData data)
        {
            if (data == null) return null;
            var block = new SettlementBlock
            {
                blockId = data.block_id,
                displayName = data.display_name ?? string.Empty,
                builderCharacterId = data.builderCharacterId,
                kind = (SettlementBlockKind)data.kind,
                subtype = (SettlementBlockSubtype)data.subtype,
                enabled = data.enabled,
                level = data.level,
                condition = data.condition,
                hasSpatialData = data.hasSpatialData,
                areaRatio = data.areaRatio,
                populationRatio = data.populationRatio,
                development = data.development,
                wealth = data.wealth,
                length = data.length,
                districtType = (CityDistrictType)data.districtType,
                buildingDefId = data.buildingDefId,
                wallLayerCount = data.wallLayerCount,
                wallMoatLevel = data.wallMoatLevel,
                wallRole = (WallRole)data.wallRole,
                wallLayout = (WallLayout)data.wallLayout,
                wallMaterial = (WallMaterial)data.wallMaterial,
                wallMaterialGrade = data.wallMaterialGrade,
                children = new List<SettlementBlock>(),
            };
            if (block.kind == SettlementBlockKind.Wall)
            {
                if (data.wallDataVersion <= 0)
                {
                    // 旧档：level 存的是旧 WallLevel（0..6），映射为 0–15。
                    int legacyLevel = data.level;
                    block.level = FortificationLevels.FromLegacy((WallLevel)legacyLevel);
                    block.wallMaterial = FortificationLevels.DefaultMaterialForLevel(block.level);
                    block.wallMaterialGrade = Math.Max(1, FortificationLevels.GradeOf(block.level));
                }
                else
                {
                    block.level = FortificationLevels.Clamp(block.level);
                    block.wallMaterialGrade = FortificationLevels.ClampMaterialGrade(block.wallMaterialGrade);
                }
                WallSegmentSaveMapping.RestoreAll(block.wallSegments, data.wallSegments);
                block.NormalizeWallData();
            }
            return block;
        }

        /// <summary>
        /// 扁平化捕获（D30）：把整棵区块树按 DFS 铺平成一张表，
        /// 层级用 `parent_block_id` 表达。这样既不撞 Unity 序列化深度上限，
        /// 也让每个节点可被独立引用。
        /// </summary>
        public static SettlementBlockSaveData[] CaptureAll(List<SettlementBlock> blocks)
        {
            if (blocks == null || blocks.Count == 0) return null;
            var flat = new List<SettlementBlockSaveData>();
            foreach (var block in blocks)
                FlattenInto(block, string.Empty, flat);
            return flat.ToArray();
        }

        private static void FlattenInto(SettlementBlock block, string parentBlockId, List<SettlementBlockSaveData> sink)
        {
            if (block == null) return;
            sink.Add(From(block, parentBlockId));
            if (block.children == null) return;
            foreach (var child in block.children)
                FlattenInto(child, block.blockId ?? string.Empty, sink);
        }

        /// <summary>
        /// 还原：两趟。第一趟建出全部实体并按 `block_id` 建索引；第二趟按 `parent_block_id` 挂父。
        /// 同时兼容**旧档的嵌套形状**（`children` 非空时递归挂回）。
        /// </summary>
        public static void RestoreAll(List<SettlementBlock> target, SettlementBlockSaveData[] data)
        {
            if (target == null) return;
            target.Clear();
            if (data == null) return;

            var byId = new Dictionary<string, SettlementBlock>();
            var created = new List<SettlementBlock>(data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                var entry = data[i];
                if (entry == null) continue;
                var block = entry.ToBlock();
                created.Add(block);
                if (!string.IsNullOrEmpty(entry.block_id) && !byId.ContainsKey(entry.block_id))
                    byId[entry.block_id] = block;
            }

            for (int i = 0; i < data.Length; i++)
            {
                var entry = data[i];
                if (entry == null) continue;
                var block = created[i];

                // 旧档兼容：嵌套形状直接递归挂回
                if (entry.children != null && entry.children.Length > 0)
                {
                    RestoreAll(block.children, entry.children);
                    target.Add(block);
                    continue;
                }

                if (string.IsNullOrEmpty(entry.parent_block_id))
                {
                    target.Add(block);
                    continue;
                }

                if (byId.TryGetValue(entry.parent_block_id, out var parent) && parent != block)
                    parent.children.Add(block);
                else
                    target.Add(block); // 父缺失（脏档/被删）时降级为根，避免整棵树丢失
            }
        }
    }
}
