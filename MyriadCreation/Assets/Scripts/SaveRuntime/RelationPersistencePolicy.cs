using System.Collections.Generic;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Enums;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 关系持久化边界：主存档（SaveData）只保存两端都能被它持久化的关系，
    /// 避免“关系存了、端点没存”导致读档悬空。Anchor/Unit/Character 关系等
    /// 对应实体有各自存档路径后再放开（P1/P2）。
    /// </summary>
    public static class RelationPersistencePolicy
    {
        public static bool IsWorldSaved(EntityRef entity)
            => entity.kind == EntityKind.Realm || entity.kind == EntityKind.Province;

        public static List<EntityRelation> ForWorldSave(IEnumerable<EntityRelation> relations)
        {
            var result = new List<EntityRelation>();
            if (relations == null) return result;
            foreach (var relation in relations)
            {
                if (relation == null || relation.isProjection) continue;
                if (IsWorldSaved(relation.from) && IsWorldSaved(relation.to))
                    result.Add(relation);
            }
            return result;
        }

        /// <summary>地图存档可承载的关系：至少一端是 Anchor，另一端只能是 Anchor/Realm。</summary>
        public static bool IsMapSaved(EntityRef entity)
            => entity.kind == EntityKind.Anchor || entity.kind == EntityKind.Realm;

        public static List<EntityRelation> ForMapSave(IEnumerable<EntityRelation> relations)
        {
            var result = new List<EntityRelation>();
            if (relations == null) return result;
            foreach (var relation in relations)
            {
                if (relation == null || relation.isProjection) continue;
                bool touchesAnchor = relation.from.kind == EntityKind.Anchor
                    || relation.to.kind == EntityKind.Anchor;
                if (touchesAnchor && IsMapSaved(relation.from) && IsMapSaved(relation.to))
                    result.Add(relation);
            }
            return result;
        }
    }
}
