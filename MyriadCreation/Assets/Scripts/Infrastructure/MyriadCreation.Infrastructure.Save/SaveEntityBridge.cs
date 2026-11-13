using System.Collections.Generic;
using System.Reflection;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using UnityEngine;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 读档实体安装桥（SaveVersion 11）。
    ///
    /// 为什么用反射：CharacterManager / MapActorManager 的内部容器（_characters / _families /
    /// _actors / _actorById / 自增 id 游标）是 private，且它们位于 Infrastructure/Save 的写入范围之外。
    /// 为了"按存档 id 原样重建、不重复造人、不因自增游标错位而覆盖旧角色"，只能在这里集中做一次
    /// 受控的反射注入。所有目标字段名在缺失时静默降级（LogError），不会因反射失败而抛异常中断读档。
    ///
    /// 注意：本类只做"装载容器"，不重建对象内部派生状态；恢复由 SaveSystem 按顺序调用。
    /// </summary>
    internal static class SaveEntityBridge
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly FieldInfo CharactersField =
            typeof(CharacterManager).GetField("_characters", InstanceFlags);
        private static readonly FieldInfo NextCharacterIdField =
            typeof(CharacterManager).GetField("_nextCharacterId", InstanceFlags);
        private static readonly FieldInfo FamiliesField =
            typeof(CharacterManager).GetField("_families", InstanceFlags);
        private static readonly FieldInfo NextFamilyIdField =
            typeof(CharacterManager).GetField("_nextFamilyId", InstanceFlags);

        private static readonly FieldInfo ActorsField =
            typeof(MapActorManager).GetField("_actors", InstanceFlags);
        private static readonly FieldInfo ActorByIdField =
            typeof(MapActorManager).GetField("_actorById", InstanceFlags);
        private static readonly FieldInfo NextActorIdField =
            typeof(MapActorManager).GetField("_nextActorId", InstanceFlags);

        private static readonly FieldInfo KnownField =
            typeof(CharacterKnowledgeRegistry).GetField("_known", StaticFlags);

        /// <summary>清空并写入角色字典，自增 id 置为 max+1（存档为空时保持 1）。</summary>
        public static void InstallCharacters(CharacterManager manager, List<CharacterData> characters)
        {
            if (manager == null) return;
            var dict = CharactersField?.GetValue(manager) as Dictionary<int, CharacterData>;
            if (dict == null)
            {
                Debug.LogError("[SaveEntityBridge] CharacterManager._characters 反射失败，角色未能恢复");
                return;
            }

            dict.Clear();
            int maxId = 0;
            if (characters != null)
            {
                foreach (var c in characters)
                {
                    if (c == null || c.characterId <= 0) continue;
                    dict[c.characterId] = c;
                    if (c.characterId > maxId) maxId = c.characterId;
                }
            }
            NextCharacterIdField?.SetValue(manager, maxId + 1);
        }

        /// <summary>清空并写入家族字典。</summary>
        public static void InstallFamilies(CharacterManager manager, List<FamilyNode> families)
        {
            if (manager == null) return;
            var dict = FamiliesField?.GetValue(manager) as Dictionary<int, FamilyNode>;
            if (dict == null)
            {
                Debug.LogError("[SaveEntityBridge] CharacterManager._families 反射失败，家族未能恢复");
                return;
            }

            dict.Clear();
            int maxId = 0;
            if (families != null)
            {
                foreach (var f in families)
                {
                    if (f == null || f.familyId <= 0) continue;
                    dict[f.familyId] = f;
                    if (f.familyId > maxId) maxId = f.familyId;
                }
            }
            NextFamilyIdField?.SetValue(manager, maxId + 1);
        }

        /// <summary>
        /// 清空并写入地图单位列表/索引；按 leaderAICategory 与 subcategory 重建 AI 执行器。
        /// MapUnitRegistry 不在此处刷新，由调用方随后统一 SyncFromWorld/按存档恢复。
        /// </summary>
        public static void InstallMapActors(MapActorManager manager, List<MapActor> actors)
        {
            if (manager == null) return;
            var list = ActorsField?.GetValue(manager) as List<MapActor>;
            var byId = ActorByIdField?.GetValue(manager) as Dictionary<int, MapActor>;
            if (list == null || byId == null)
            {
                Debug.LogError("[SaveEntityBridge] MapActorManager 内部容器反射失败，地图单位未能恢复");
                return;
            }

            list.Clear();
            byId.Clear();
            int maxId = 0;
            if (actors != null)
            {
                foreach (var a in actors)
                {
                    if (a == null || a.actorId <= 0) continue;
                    a.AI = UnitAIFactory.CreateForActor(a);
                    list.Add(a);
                    byId[a.actorId] = a;
                    if (a.actorId > maxId) maxId = a.actorId;
                }
            }
            NextActorIdField?.SetValue(manager, maxId + 1);
        }

        /// <summary>导出角色知识表（静态字典，无公开枚举接口，只能反射读取）。</summary>
        public static List<KnownCharacterSaveData> CaptureKnownCharacters()
        {
            var result = new List<KnownCharacterSaveData>();
            var dict = KnownField?.GetValue(null) as Dictionary<long, KnownCharacterInfo>;
            if (dict == null) return result;

            foreach (var kv in dict)
            {
                var info = kv.Value;
                result.Add(new KnownCharacterSaveData
                {
                    observerRealmId = info.observerRealmId,
                    characterId = info.characterId,
                    characterRealmId = info.characterRealmId,
                    knownSettlementAnchorId = info.knownSettlementAnchorId,
                    lastSeenDay = info.lastSeenDay,
                    source = info.source ?? ""
                });
            }
            return result;
        }

        /// <summary>清空并重建角色知识表（依赖角色已先安装）。</summary>
        public static void InstallKnownCharacters(CharacterManager manager, List<KnownCharacterSaveData> known)
        {
            CharacterKnowledgeRegistry.Clear();
            if (manager == null || known == null) return;

            foreach (var k in known)
            {
                if (k == null) continue;
                var character = manager.GetCharacter(k.characterId);
                if (character == null) continue;
                CharacterKnowledgeRegistry.Learn(k.observerRealmId, character, k.lastSeenDay, k.source);
            }
        }
    }

    /// <summary>
    /// 角色纽带（CharacterManager._bonds，管理器级实体而非单角色字段）的存档转换层。
    /// 捕获/恢复走 CharacterManager 的 CaptureBonds/RestoreBonds 公共 API，不反射私有容器；
    /// 读档时须在角色表安装完成后调用，两端角色缺失的纽带会被 RestoreBonds 跳过并告警。
    /// </summary>
    public static class CharacterBondPersistence
    {
        /// <summary>导出全部纽带 DTO（调用方负责把结果放进存档的纽带段）。</summary>
        public static List<CharacterBondSaveData> Capture(CharacterManager manager)
        {
            var result = new List<CharacterBondSaveData>();
            if (manager == null) return result;
            foreach (CharacterBond bond in manager.CaptureBonds())
            {
                if (bond == null) continue;
                result.Add(CharacterBondSaveData.FromBond(bond));
            }
            return result;
        }

        /// <summary>清空并重建纽带；返回实际恢复条数（无效 id 或角色不存在的条目跳过）。</summary>
        public static int Restore(CharacterManager manager, List<CharacterBondSaveData> bonds)
        {
            if (manager == null) return 0;
            var runtimeBonds = new List<CharacterBond>();
            if (bonds != null)
            {
                foreach (CharacterBondSaveData dto in bonds)
                {
                    if (dto == null) continue;
                    runtimeBonds.Add(dto.ToBond());
                }
            }
            manager.RestoreBonds(runtimeBonds);
            return manager.GetAllBonds().Count;
        }
    }
}
