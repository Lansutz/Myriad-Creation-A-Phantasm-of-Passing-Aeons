using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Warfare;







using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.Faith;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Content.Definitions;
namespace MyriadCreation.Infrastructure.Save
{
 /// 存档系统（v2）
 /// v1 使用 BinaryFormatter，在 Unity 6 中已被禁用（运行时抛 NotSupportedException，存档静默失败），
 /// v2 改为 JsonUtility + 可序列化 DTO：Dictionary/HashSet 转 List 包装（JsonUtility 不支持字典），
 /// 零第三方依赖；JSON 文本亦为 WebGL 等沙箱平台迁移留有余地。
 /// 存档结构变更时递增 GameConstants.SaveVersion。
    public static class SaveSystem
    {
        private static string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

        /// <summary>世界默认随机种子（与 GameWorld.randomSeed 字段初始值一致；旧档缺 randomSeed 时回退）。</summary>
        private const int DefaultWorldSeed = 42;

 /// <summary>保存游戏</summary>
        public static bool SaveGame(GameWorld world, string saveName)
        {
            // 写档前校验存档名：空名/路径分隔符/".." 会把文件写到 Saves 目录之外（写穿目录）。
            // 校验必须在创建目录之前，非法存档名不产生任何副作用。
            if (!IsValidSaveName(saveName))
            {
                Debug.LogError($"[SaveSystem] 存档失败：存档名非法 \"{saveName}\"（不能为空，不能包含路径分隔符或 \"..\"）");
                return false;
            }

            string filePath = null;
            string tempPath = null;
            string backupPath = null;

            try
            {
                if (!Directory.Exists(SaveDirectory))
                    Directory.CreateDirectory(SaveDirectory);

                filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
                // 原子写三步：先写同目录 .tmp → 回读校验 → 整文件替换 .sav。
                // 替换发生前旧档一直完好，任一步失败只丢临时文件。
                tempPath = filePath + ".tmp";
                backupPath = filePath + ".bak";
                string restoreTempPath = filePath + ".restore.tmp";

                // 清掉上次崩溃/异常遗留的同名 .tmp / .restore.tmp：陈旧临时档既占空间，
                // 也可能被误当作本次写入结果。（若同名路径被目录占据，File.Exists 为 false，
                // 本次清理不会误删目录，后续写入会按失败处理。）
                DeleteFileQuietly(tempPath);
                DeleteFileQuietly(restoreTempPath);

                var saveData = ToSaveData(world);
                string json = JsonUtility.ToJson(saveData, true);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogError($"[SaveSystem] 存档失败：序列化结果为空 ({filePath})");
                    return false;
                }

                // 1) 写临时文件（File.WriteAllText 内部会 flush 并关闭句柄）。
                File.WriteAllText(tempPath, json);

                // 2) 回读校验：临时档必须可读且长度与序列化结果一致，否则视为写盘失败；
                //    读回半截档时绝不能替换旧档。
                string readBack = File.ReadAllText(tempPath);
                if (readBack.Length != json.Length)
                {
                    Debug.LogError($"[SaveSystem] 存档失败：临档回读校验不通过（写入 {json.Length} 字符，读回 {readBack.Length} 字符）: {filePath}");
                    return false;
                }

                // 3) 原子替换：目标存在用 File.Replace（旧档先落 .bak 兜底），
                //    目标不存在用 File.Move（同目录 rename，不会出现写了一半的 .sav）。
                if (File.Exists(filePath))
                {
                    File.Replace(tempPath, filePath, backupPath);
                    // 备份只在替换失败时兜底，成功后清掉避免存档目录堆积陈旧 .bak。
                    DeleteFileQuietly(backupPath);
                }
                else
                {
                    File.Move(tempPath, filePath);
                    // 目标原本不存在（半失败/手工恢复场景）时可能残留旧 .bak；新档已成功落盘，一并清掉。
                    DeleteFileQuietly(backupPath);
                }

                Debug.Log($"[SaveSystem] 存档成功: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                // e.ToString() 含异常类型与堆栈：只打 e.Message 会吞掉调用链（审计 A-P1-8）。
                Debug.LogError($"[SaveSystem] 存档失败（旧档未受影响）: {e}");
                return false;
            }
            finally
            {
                // 任一步失败都不能留下 .tmp；成功路径下临时文件已被 Move/Replace 移走，
                // 此处是幂等的空操作（File.Exists 对不存在路径返回 false）。
                DeleteFileQuietly(tempPath);
            }
        }

        /// <summary>存档名最小校验：拒绝空名、路径分隔符、".." 与文件系统非法字符，避免写穿 Saves 目录。</summary>
        private static bool IsValidSaveName(string saveName)
        {
            if (string.IsNullOrWhiteSpace(saveName))
                return false;
            if (saveName.IndexOf('/') >= 0 || saveName.IndexOf('\\') >= 0)
                return false;
            if (saveName.Contains(".."))
                return false;
            if (saveName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;
            return true;
        }

        /// <summary>尽力删除文件：清理失败只记警告，不把成功写档误判为失败。</summary>
        private static void DeleteFileQuietly(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                if (!File.Exists(path))
                    return;
                // 崩溃残留/外部工具可能把文件标成只读：Windows 上直接 File.Delete 会失败，
                // 先复位只读属性再删（只处理文件，不碰同名目录）。
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 清理临时文件失败: {path} ({e.Message})");
            }
        }

        /// <summary>解析存档 JSON：格式非法/结构无效（tiles 为空）返回 null，不把异常抛给调用方。</summary>
        private static SaveData TryParseSaveData(string json)
        {
            if (string.IsNullOrEmpty(json))
                return null;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                return data != null && data.tiles != null ? data : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>从 .bak 恢复主存档：先复制到临时档再 rename，避免在 .sav 位置留下半截恢复档；失败保留 .bak。</summary>
        private static void RestoreSaveFromBackup(string filePath, string backupPath)
        {
            string restorePath = filePath + ".restore.tmp";
            try
            {
                File.Copy(backupPath, restorePath, true);
                // 目标可能不存在（半失败窗口），也可能是解析失败的损坏档（损坏主档回退）：
                // 先清掉目标再 rename，避免 File.Move 因目标已存在而失败。.bak 始终保留，任一步失败都能重试。
                DeleteFileQuietly(filePath);
                if (File.Exists(filePath))
                    throw new IOException($"无法删除待替换的主存档: {filePath}");
                File.Move(restorePath, filePath);
                Debug.LogWarning($"[SaveSystem] 已从备份恢复存档主文件: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveSystem] 备份可读但恢复主文件失败（.bak 保留，下次加载可重试）: {e}");
            }
            finally
            {
                DeleteFileQuietly(restorePath);
            }
        }

 /// <summary>加载游戏</summary>
        public static GameWorld LoadGame(string saveName)
        {
            // 读档入口与写档同样校验：否则 "../x" 会读到 Saves 目录之外（写侧已堵，读/删侧保持一致）。
            if (!IsValidSaveName(saveName))
            {
                Debug.LogError($"[SaveSystem] 读档失败：存档名非法 \"{saveName}\"（不能为空，不能包含路径分隔符或 \"..\"）");
                return null;
            }

            // 读档不是原子操作：ApplyToWorld 会先改写全局注册表再建世界，中途抛异常
            // 若直接 return null，旧世界已被污染、新建的 GameWorld 还会继续 GameTick。
            // 因此先快照可回滚的全局静态表，失败时恢复并销毁半成品世界。
            var previousRelations = RelationRegistry.Capture();
            var previousContracts = ContractRegistry.Capture();
            var previousDynamicGoods = DynamicGoodsRegistry.Capture();
            var previousMapUnits = new List<MapUnit>(MapUnitRegistry.All);
            GameObject worldObject = null;

            try
            {
                string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
                string backupPath = filePath + ".bak";
                bool recoveredFromBackup = false;
                SaveData saveData = null;

                if (File.Exists(filePath))
                {
                    // 主档存在：正常解析；解析失败（截断/损坏）且 .bak 存在时再回退一次备份。
                    saveData = TryParseSaveData(File.ReadAllText(filePath));
                    if (saveData == null && File.Exists(backupPath))
                    {
                        Debug.LogWarning($"[SaveSystem] 主存档解析失败，尝试从备份恢复: {backupPath}");
                        saveData = TryParseSaveData(File.ReadAllText(backupPath));
                        recoveredFromBackup = saveData != null;
                    }
                }
                else if (File.Exists(backupPath))
                {
                    // 主文件缺失（File.Replace 半失败窗口/误删）：回退读 .bak，解析成功后再复制回 .sav。
                    Debug.LogWarning($"[SaveSystem] 存档主文件缺失，尝试从备份恢复: {backupPath}");
                    saveData = TryParseSaveData(File.ReadAllText(backupPath));
                    recoveredFromBackup = saveData != null;
                }
                else
                {
                    Debug.LogError($"[SaveSystem] 存档不存在: {filePath}");
                    return null;
                }

                if (saveData == null)
                {
 // 非法 JSON / v1 二进制存档无法解析
                    Debug.LogError($"[SaveSystem] 读档失败: {filePath}（存档格式无效或为 v1 二进制存档，请新建游戏；.bak 保留供重试）");
                    return null;
                }

                // 只有从 .bak 成功解析出数据才回写主文件；恢复失败时 .bak 原样保留，下次加载可重试。
                if (recoveredFromBackup)
                    RestoreSaveFromBackup(filePath, backupPath);

                if (saveData.version != GameConstants.SaveVersion)
                {
                    Debug.LogWarning($"[SaveSystem] 存档版本 {saveData.version} ≠ 当前 {GameConstants.SaveVersion}，尝试兼容加载");
                }

                // 旧档字段显式迁移（RealmDTO.composition → laws）
                if (saveData.realms != null)
                    foreach (var realmDto in saveData.realms)
                        realmDto?.NormalizeLegacyFields();

                worldObject = new GameObject($"GameWorld_{saveName}");
                // B3-Step4：读档世界创建走 WorldFactory.CreateEmpty（AddComponent，不 InitializeWorld），
                // 数据填充与子系统重建由 ApplyToWorld + WorldCompositionRoot 完成。
                var world = WorldFactory.CreateEmpty(worldObject);
                ApplyToWorld(saveData, world);

                Debug.Log($"[SaveSystem] 读档成功: {filePath} (版本{saveData.version}, {saveData.saveTime})");
                return world;
            }
            catch (Exception e)
            {
                if (worldObject != null)
                {
                    // DestroyImmediate：不能在读档失败后留下会继续 tick 的幽灵世界
                    UnityEngine.Object.DestroyImmediate(worldObject);
                }
                RelationRegistry.Restore(previousRelations);
                ContractRegistry.Restore(previousContracts);
                DynamicGoodsRegistry.Restore(previousDynamicGoods);
                MapUnitRegistry.Restore(previousMapUnits);
                Debug.LogError($"[SaveSystem] 读档失败（已回滚全局表）: {e}");
                return null;
            }
        }

 /// <summary>删除存档（连带清理 .bak/.tmp 等辅助产物）</summary>
        public static bool DeleteSave(string saveName)
        {
            // 删除入口同样校验：否则 "../x" 会删到 Saves 目录之外的文件。
            if (!IsValidSaveName(saveName))
            {
                Debug.LogError($"[SaveSystem] 删除存档失败：存档名非法 \"{saveName}\"（不能为空，不能包含路径分隔符或 \"..\"）");
                return false;
            }

            string filePath = Path.Combine(SaveDirectory, $"{saveName}.sav");
            string backupPath = filePath + ".bak";
            // 返回值语义：主文件或 .bak 任一存在即视为"删到了存档"返回 true——
            // 只剩 .bak 的待恢复档也算存在（否则 ListSaves 列出后 UI 无法删掉它）；
            // .tmp/.restore.tmp 只是辅助产物，仅清理，不计入返回值。
            bool existed = File.Exists(filePath) || File.Exists(backupPath);
            DeleteFileQuietly(filePath);
            DeleteFileQuietly(backupPath);
            DeleteFileQuietly(filePath + ".tmp");
            DeleteFileQuietly(filePath + ".restore.tmp");
            return existed;
        }

 /// <summary>列出所有存档（含仅剩 .bak 的待恢复档，避免 UI 静默隐藏）</summary>
        public static string[] ListSaves()
        {
            if (!Directory.Exists(SaveDirectory))
                return new string[0];

            var names = new List<string>();
            foreach (string file in Directory.GetFiles(SaveDirectory, "*.sav"))
            {
                string fileName = Path.GetFileName(file);
                // 显式按后缀过滤：Windows 通配符/8.3 短名可能把 .sav.bak 之类混进来。
                if (!fileName.EndsWith(".sav", StringComparison.OrdinalIgnoreCase))
                    continue;
                names.Add(fileName.Substring(0, fileName.Length - ".sav".Length));
            }

            // 主文件缺失但 .bak 存在（File.Replace 半失败窗口）：按"可恢复档"列出，
            // 否则 UI 里完全看不到，用户会以为存档已丢失。
            const string backupSuffix = ".sav.bak";
            foreach (string file in Directory.GetFiles(SaveDirectory))
            {
                string fileName = Path.GetFileName(file);
                if (!fileName.EndsWith(backupSuffix, StringComparison.OrdinalIgnoreCase))
                    continue;
                string name = fileName.Substring(0, fileName.Length - backupSuffix.Length);
                if (!names.Contains(name))
                    names.Add(name);
            }

            return names.ToArray();
        }

 /// <summary>获取存档目录路径</summary>
        public static string GetSaveDirectory() => SaveDirectory;

 // ===== 游戏对象 ⇄ DTO 转换 =====
        private static SaveData ToSaveData(GameWorld world)
        {
            var data = new SaveData
            {
                version = GameConstants.SaveVersion,
                saveTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                mapWidth = world.mapWidth,
                mapHeight = world.mapHeight,
                currentYear = world.currentYear,
                currentDay = world.currentDay,
                currentSeason = world.currentSeason,
                randomSeed = world.randomSeed,
                playerRealmId = world.PlayerRealmId,
                tiles = world.tiles,
                races = new List<RaceDTO>(),
                cultures = new List<CultureData>(),
                realms = new List<RealmDTO>(),
                tradeCenters = new List<TradeCenterDTO>(),
                goodsDefs = new List<GoodsDef>(),
                dynamicGoods = DynamicGoodsRegistry.Capture(),
                relations = MyriadCreation.Simulation.Systems.RelationEndpointValidator.Filter(world, RelationPersistencePolicy.ForWorldSave(RelationRegistry.Capture()), out _),
                contracts = ContractRegistry.Capture(),
                configJson = JsonUtility.ToJson(world.config),
                setupOptions = world.SetupOptions
            };

            foreach (var kv in world.races) data.races.Add(RaceDTO.FromRaceData(kv.Value));
            foreach (var kv in world.cultures) data.cultures.Add(kv.Value);
            foreach (var kv in world.realms) data.realms.Add(RealmDTO.FromRealmData(kv.Value));
            foreach (var kv in world.tradeCenters) data.tradeCenters.Add(TradeCenterDTO.FromTradeCenter(kv.Value));
            foreach (var kv in world.goodsDefs) data.goodsDefs.Add(kv.Value);
            data.wars = world.GetWars() != null ? new List<WarState>(world.GetWars()) : new List<WarState>();

 // 省份/聚落/营地（v12 起；城墙/建筑/市场库存/城市区划/营地补给不再读档蒸发）
            data.provinces = SettlementPersistence.CaptureProvinces(world);
            data.habitationSites = SettlementPersistence.CaptureSites(world);

 // 军队实体（v13 起；军队编制/士气/补给/行军路径不再读档蒸发）
            data.armies = ArmyPersistence.CaptureArmies(world);

 // 革新/科技树运行时状态（v14 起；读档后科技不再清零）
            data.innovations = world.Innovations != null ? world.Innovations.CaptureState() : null;
            data.innovationKnowledge = world.ResearchPlans?.CaptureKnowledge();

 // 信仰运行态（v15 起；faithName/virtues/sins 等内容定义由 ReligionCatalog 重建，这里只存运行态）
            data.faiths = FaithPersistence.Capture(world);
            data.denominations = DenominationPersistence.Capture(world);
            data.chronicle = ChroniclePersistence.Capture(world);
            data.titleInstances = TitlePersistence.Capture();

            var diplomacy = world.GetDiplomacyManager();
            if (diplomacy != null)
            {
                data.diplomaticRelations = diplomacy.CaptureRelations();
                data.diplomaticSubordinations = diplomacy.CaptureSubordinations();
            }

            data.plans = world.Plans.CapturePlans();
            data.aiControllers = world.GetAIManager()?.CaptureState();

 // 角色/家族/官职 holder/地图单位/角色知识（v11 起；旧档读取时这些字段为 null）
            CapturePersistentEntities(world, data);
            return data;
        }

        private static void ApplyToWorld(SaveData data, GameWorld world)
        {
            world.mapWidth = data.mapWidth;
            world.mapHeight = data.mapHeight;
            world.currentYear = data.currentYear;
            world.currentDay = data.currentDay;
            world.currentSeason = data.currentSeason;
            world.PlayerRealmId = data.playerRealmId;
            world.tiles = data.tiles;
            world.SetupOptions = data.setupOptions ?? MyriadCreation.World.Generation.WorldSetupOptions.CreateDefault();

 // 注：WorldFactory.CreateEmpty 仅 AddComponent，不再触发 InitializeWorld；此处用存档数据整体覆盖；unitDefs 不入档，保留默认集。
            world.races.Clear();
            if (data.races != null)
                foreach (var dto in data.races) world.races[dto.raceId] = dto.ToRaceData();

            world.cultures.Clear();
            if (data.cultures != null)
                foreach (var c in data.cultures) world.cultures[c.cultureId] = c;

            world.realms.Clear();
            if (data.realms != null)
                foreach (var dto in data.realms) world.realms[dto.realmId] = dto.ToRealmData();

            world.tradeCenters.Clear();
            if (data.tradeCenters != null)
                foreach (var dto in data.tradeCenters) world.tradeCenters[dto.regionId] = dto.ToTradeCenter();

            DynamicGoodsRegistry.Restore(data.dynamicGoods);
            RelationRegistry.Restore(data.relations);
            ContractRegistry.Restore(data.contracts);
            world.goodsDefs.Clear();
            if (data.goodsDefs != null)
                foreach (var g in data.goodsDefs) world.goodsDefs[g.goodsId] = g;

 // ScriptableObject 配置：先建运行时实例，再用 JSON 快照覆盖恢复
            world.config = WorldConfig.CreateRuntimeInstance();
            if (!string.IsNullOrEmpty(data.configJson))
                JsonUtility.FromJsonOverwrite(data.configJson, world.config);

 // 战争状态恢复（读档后战争闭环继续）
            var wars = world.GetWars();
            if (wars != null)
            {
                wars.Clear();
                if (data.wars != null)
                    wars.AddRange(data.wars);
            }

            // 世界 seed（v16 起）：读档后重算/生成器重建必须继续使用存档 seed；旧档缺字段为 0 → 回退默认 42。
            // 必须放在重新初始化子系统之前：InitializeSubsystems 会用 randomSeed 重建 SeaLandGenerator。
            world.randomSeed = data.randomSeed > 0 ? data.randomSeed : DefaultWorldSeed;

 // 重新初始化子系统（引用类型无法序列化，需要重建）；B3-Step3b 走 WorldCompositionRoot 精确 hook。
            var composition = new WorldCompositionRoot(world);
            composition.InitializeSubsystems();
            composition.ApplySetupOptions();
            // 革新/科技树状态：子系统已持有 InnovationTree 实例，就地还原。
            world.Innovations?.RestoreState(data.innovations);
            world.ResearchPlans?.RestoreKnowledge(data.innovationKnowledge);
            // 省份/聚落/营地：子系统已捕获这些字典实例，必须就地填充后再恢复实体
            // （地图单位/角色要引用聚落锚点，顺序不能反）。
            SettlementPersistence.RestoreProvinces(world, data.provinces);
            SettlementPersistence.RestoreSites(world, data.habitationSites);

            // 军队实体（v13）：就地还原军队字典，并把自增 ID 推进到存档最大值之后，
            // 避免读档后新建军队/战争与旧实体撞 ID。
            int nextArmyId = ArmyPersistence.RestoreArmies(world, data.armies);
            int nextNavyId = NavyPersistence.RestoreNavies(world, data.navies);
            int nextWarId = 1;
            if (data.wars != null)
                foreach (var war in data.wars)
                    if (war != null && war.warId >= nextWarId) nextWarId = war.warId + 1;
            world.AdoptRuntimeIdCounters(nextWarId, nextArmyId, nextNavyId);

            // 实体恢复必须放在子系统重建之后：新子系统持有新的 CharacterManager/MapActorManager 实例，
            // 若先装实体再重建，会被整体丢弃（长历史读档"人物/商队蒸发"）。
            RestorePersistentEntities(data, world);

            // 地图单位全局自增 ID 续接：必须在按军队实体登记视图之前完成，
            // 否则 RestorePersistentEntities 里的 Clear() 把 _nextId 重置为 1，
            // 军队视图（FromArmy 不带 unitId）会从 1 分配并覆盖已还原的商队/难民条目。
            int nextUnitId = 1;
            foreach (var mapUnit in MapUnitRegistry.All)
                if (mapUnit != null && mapUnit.unitId >= nextUnitId) nextUnitId = mapUnit.unitId + 1;
            MapUnitRegistry.AdoptNextId(nextUnitId);

            // 军队地图视图按军队实体重新登记（必须在 MapUnit 恢复之后，那一步会 Clear 注册表）。
            ArmyPersistence.RegisterArmyViews(world);

            // 不入档的运行时定义/控制器（unitDefs / FaithSystem 骨架 / 每政权 AIController）。
            // B3-Step3b：PrepareLoadedWorld 走 WorldCompositionRoot.PrepareLoadedWorld（1:1 hook，含 UnitDefs/Faith/AI）。
            composition.PrepareLoadedWorld();

            // 信仰运行态 overlay（v15 起）：PrepareLoadedWorld 已按内容表重建 FaithSystem 骨架，
            // 这里按 faithId 覆盖运行态；旧档无 faiths 段 → 保持内容默认，不抛。
            FaithPersistence.Restore(world, data.faiths);

            // 宗派运行态 overlay（v18 起）：必须在 faiths 之后，faith.denominationId
            // 可能指向存档时动态分裂出的宗派；Restore 末尾会按最终归属重建 faithIds。
            DenominationPersistence.Restore(world, data.denominations);

            // 编年史/头衔实例（P0.1）：两者都是运行时静态/对象状态，必须在世界重建后覆盖。
            ChroniclePersistence.Restore(world, data.chronicle);
            TitlePersistence.Restore(data.titleInstances);

            // 外交四表（P0.1）：旧档 null 表示缺段，保留 InitializeSubsystems 的初始化关系。
            world.GetDiplomacyManager()?.RestoreDiplomacy(data.diplomaticRelations, data.diplomaticSubordinations);

            // 计划运行态（P0.1）：旧档 null 保持空表，不重建未计划的工程。
            world.Plans.RestorePlans(data.plans, world.currentDay);

            // AI 长期决策（P0.1）：PrepareLoadedWorld 已按政权补齐控制器，这里覆盖人格与决策状态。
            world.GetAIManager()?.RestoreState(data.aiControllers);
        }

        // ===== 角色/家族/官职/地图单位（SaveVersion 11 起）=====

        /// <summary>捕获角色、家族、官职 holder、地图单位与角色知识（旧档字段缺失不影响）。</summary>
        private static void CapturePersistentEntities(GameWorld world, SaveData data)
        {
            data.characters = new List<CharacterSaveData>();
            data.families = new List<FamilySaveData>();
            data.realmOffices = new List<RealmOfficeSaveData>();
            data.mapActors = new List<MapActorSaveData>();
            data.mapUnits = new List<MapUnitSaveData>();

            var characters = world.GetCharacterManager();
            if (characters != null)
            {
                foreach (var c in characters.GetAllCharacters().Values)
                    if (c != null) data.characters.Add(CharacterSaveData.FromCharacter(c));
                foreach (var f in characters.GetAllFamilies().Values)
                    if (f != null) data.families.Add(FamilySaveData.FromFamily(f));
            }

            if (world.realms != null)
                foreach (var realm in world.realms.Values)
                    if (realm != null) data.realmOffices.Add(RealmOfficeSaveData.FromRealm(realm));

            if (world.MapActors != null)
                foreach (var actor in world.MapActors.AllActors)
                    if (actor != null) data.mapActors.Add(MapActorSaveData.FromActor(actor));

            foreach (var unit in MapUnitRegistry.All)
                if (unit != null) data.mapUnits.Add(MapUnitSaveData.FromUnit(unit));

            data.knownCharacters = SaveEntityBridge.CaptureKnownCharacters();

            // 角色纽带（SaveVersion 15；CharacterManager 私有持有）
            if (characters != null) data.bonds = CharacterBondPersistence.Capture(characters);
        }

        /// <summary>读档恢复实体；data.characters 为 null 视为 v11 之前的旧档，按初始统治者兜底。</summary>
        private static void RestorePersistentEntities(SaveData data, GameWorld world)
        {
            var characters = world.GetCharacterManager();
            if (characters == null)
            {
                Debug.LogError("[SaveSystem] CharacterManager 为空，跳过角色/地图单位恢复");
                return;
            }

            if (data.characters == null)
            {
                // 旧档（SaveVersion < 11）：无角色段，重建初始统治者，避免读档后世界无君主（可接受的降级）。
                Debug.LogWarning("[SaveSystem] 旧档无角色段，按初始统治者兜底重建");
                characters.CreateInitialRulers(world.realms, world.currentYear);
            }
            else
            {
                var list = new List<CharacterData>(data.characters.Count);
                foreach (var dto in data.characters)
                    if (dto != null) list.Add(dto.ToCharacter());
                SaveEntityBridge.InstallCharacters(characters, list);
            }

            if (data.families != null && data.families.Count > 0)
            {
                var families = new List<FamilyNode>(data.families.Count);
                foreach (var dto in data.families)
                    if (dto != null) families.Add(dto.ToFamily());
                SaveEntityBridge.InstallFamilies(characters, families);
            }

            // 角色纽带（SaveVersion 15；必须在角色恢复之后，两端角色都要能查到）
            if (data.bonds != null && data.bonds.Count > 0)
                CharacterBondPersistence.Restore(characters, data.bonds);

            if (data.realmOffices != null && world.realms != null)
                foreach (var dto in data.realmOffices)
                {
                    if (dto == null) continue;
                    if (world.realms.TryGetValue(dto.realmId, out var realm)) dto.ApplyTo(realm);
                }

            // RealmDTO 不含 monarchId（旧契约）：读档后按角色表回填在世统治者指针，避免继位/法理判定断链。
            if (world.realms != null)
                foreach (var realm in world.realms.Values)
                {
                    if (realm == null || realm.monarchId >= 0) continue;
                    var ruler = characters.FindRulerOfRealm(realm.realmId);
                    if (ruler != null) realm.monarchId = ruler.characterId;
                }

            if (data.knownCharacters != null)
                SaveEntityBridge.InstallKnownCharacters(characters, data.knownCharacters);
            else
                CharacterKnowledgeRegistry.Clear(); // 旧档无知识段：清掉上一局/上一世界的静态残留

            if (data.mapActors != null && world.MapActors != null)
            {
                var actors = new List<MapActor>(data.mapActors.Count);
                foreach (var dto in data.mapActors)
                    if (dto != null) actors.Add(dto.ToActor());
                SaveEntityBridge.InstallMapActors(world.MapActors, actors);
            }

            if (data.mapUnits != null && data.mapUnits.Count > 0)
            {
                MapUnitRegistry.Clear();
                foreach (var dto in data.mapUnits)
                {
                    // 军队（Army）不在本任务入档范围：跳过其视图条目，避免读档后指向不存在的 army 实体。
                    // 军队视图会在军队系统重建后通过 RegisterArmy/SyncFromWorld 重新登记。
                    if (dto == null || dto.source == UnitSourceKind.Army) continue;
                    MapUnitRegistry.Add(dto.ToUnit());
                }
                // 极端情况：存档里只有军队视图条目（全部被跳过）——回退到按当前世界重建，避免空视图。
                if (MapUnitRegistry.Count == 0) MapUnitRegistry.SyncFromWorld(world);
            }
            else
            {
                MapUnitRegistry.SyncFromWorld(world);
            }
        }
    }

 /// 存档数据容器（v2，纯 JsonUtility 可序列化）
 /// 只包含可序列化的数据，引用类型的子系统由 ReinitializeSubsystems 重建

 /// <summary>通用键值包装（JsonUtility 不支持 Dictionary，存档用 List 包装）</summary>

 /// <summary>通用键值包装（bool 值）</summary>

 /// <summary>种族存档 DTO（Dictionary/枚举列表字段转 List 包装）</summary>

 /// <summary>税收系统存档 DTO（taxExemptions 字典转 List 包装）</summary>

 /// <summary>政权存档 DTO（字典/哈希集字段转 List 包装）</summary>

 /// <summary>贸易中心存档 DTO（库存/供需字典转 List 包装）</summary>
}
