using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MyriadCreation.Core.Data;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Actors;
using MyriadCreation.Simulation.Systems;
using MyriadCreation.Simulation.Units;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Generation;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>
    /// 存档写入链路回归测试（审计 A-P1-8：SaveGame 非原子写 + 异常吞栈；复核 P1/P2 follow-up）。
    ///
    /// 覆盖：正常存读往返、覆盖旧档、成功/失败路径不残留 .tmp、非法 saveName 拒绝（写/读/删都不写穿目录）、
    /// “写盘失败时旧档字节保持不变”、.sav 缺失或损坏时从 .bak 恢复（含 ListSaves 可见性、损坏 .bak 的
    /// 回退尝试与保留）、SaveGame 前清理崩溃遗留的 .tmp/.restore.tmp（陈旧只读 .tmp 用例对实现敏感）。
    /// 所有产物都落在 Application.persistentDataPath/Saves（即 SaveSystem.GetSaveDirectory()），
    /// 不读写工程外临时文件。
    /// </summary>
    public class SaveSystemAtomicWriteTests
    {
        private const string SaveName = "__atomic_write_test";
        private readonly List<GameObject> _objects = new List<GameObject>();

        private static string SaveFilePath(string saveName)
            => Path.Combine(SaveSystem.GetSaveDirectory(), saveName + ".sav");

        private static string TempFilePath(string saveName) => SaveFilePath(saveName) + ".tmp";

        private static string BackupFilePath(string saveName) => SaveFilePath(saveName) + ".bak";

        /// <summary>Saves 目录之外的哨兵文件：验证非法名 DeleteSave 不会穿越删到目录外。</summary>
        private static string GuardFilePath =>
            Path.Combine(Application.persistentDataPath, "__atomic_write_guard.sav");

        private static void TryDeleteGuardFile()
        {
            try
            {
                if (File.Exists(GuardFilePath)) File.Delete(GuardFilePath);
            }
            catch (System.Exception)
            {
                // 清理是尽力而为，不影响测试结论。
            }
        }

        /// <summary>清掉只读属性后删除文件（测试兜底清理用；正常路径由 SaveSystem 自己清理）。</summary>
        private static void ClearReadOnlyAndDelete(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
                File.Delete(path);
            }
            catch (System.Exception)
            {
                // 清理是尽力而为，不影响测试结论。
            }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _objects)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _objects.Clear();
            MapUnitRegistry.Clear();
            CharacterKnowledgeRegistry.Clear();
            RelationRegistry.Clear();
            ContractRegistry.Clear();

            // 失败路径用例会在 .tmp 路径上造一个同名目录当障碍：即使断言提前失败也要兜底清掉。
            try
            {
                string tempDir = TempFilePath(SaveName);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch (System.Exception)
            {
                // 清理是尽力而为，不影响测试结论。
            }
            TryDeleteGuardFile();

            // DeleteSave 现已连带清理 .sav/.bak/.tmp/.restore.tmp，恢复用例造的 .bak 也会被清掉。
            SaveSystem.DeleteSave(SaveName);
        }

        private GameWorld NewWorld(int width = 24, int height = 12)
        {
            var go = new GameObject("AtomicWriteTestWorld");
            _objects.Add(go);
            var world = go.AddComponent<GameWorld>();
            world.mapWidth = width;
            world.mapHeight = height;
            world.SetupOptions = WorldSetupOptions.CreateStandard();
            world.InitializeWorld();
            world.GenerateTerrain(11); // 与 InnovationPersistenceTests 相同的已验证路径（生成地块/政权/聚落）
            return world;
        }

        [Test]
        public void SaveGame_ThenLoadGame_RoundTripsWorld()
        {
            var world = NewWorld();
            world.currentYear = 137;
            int realmCount = world.realms.Count;
            int tileCount = world.tiles.Length;

            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            Assert.IsTrue(File.Exists(SaveFilePath(SaveName)), "存档文件应存在");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "读档应返回世界");
            _objects.Add(loaded.gameObject);

            Assert.AreEqual(137, loaded.currentYear, "读档应还原写入的年份");
            Assert.AreEqual(realmCount, loaded.realms.Count, "政权数应一致");
            Assert.AreEqual(tileCount, loaded.tiles.Length, "地块数应一致");
        }

        [Test]
        public void SaveGame_OverExistingSave_ReplacesContent()
        {
            var world = NewWorld();
            world.currentYear = 111;
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "首次存盘应成功");
            Assert.IsTrue(File.Exists(SaveFilePath(SaveName)), "首次存盘应创建文件");

            // 第二次存盘目标已存在，走 File.Replace 分支。
            world.currentYear = 222;
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "覆盖存盘应成功");
            Assert.IsFalse(File.Exists(BackupFilePath(SaveName)), "覆盖成功后应清理 .bak 备份，避免目录堆积");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "覆盖后读档应返回世界");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(222, loaded.currentYear, "读档应读到覆盖后的新内容（旧档 111 已被替换）");
        }

        [Test]
        public void SaveGame_Success_LeavesNoTemporaryFile()
        {
            var world = NewWorld();
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "存盘应成功");
            Assert.IsFalse(File.Exists(TempFilePath(SaveName)), "成功写档后不应残留 .tmp 文件");

            world.currentYear = 222; // 再走一次目标已存在的 File.Replace 分支
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "覆盖存盘应成功");
            Assert.IsFalse(File.Exists(TempFilePath(SaveName)), "覆盖写档后不应残留 .tmp 文件");
            Assert.AreEqual(0,
                Directory.GetFiles(SaveSystem.GetSaveDirectory(), SaveName + "*.tmp").Length,
                "存档目录里不应有该存档的 .tmp 残留");
        }

        [Test]
        public void SaveGame_InvalidSaveName_ReturnsFalseAndCreatesNoFile()
        {
            // 非法名会命中 Debug.LogError：这是本用例的预期行为，不能让该日志把测试判失败。
            LogAssert.ignoreFailingMessages = true;

            var world = NewWorld();
            string saveDir = SaveSystem.GetSaveDirectory();
            int filesBefore = Directory.Exists(saveDir)
                ? Directory.GetFiles(saveDir, "*", SearchOption.AllDirectories).Length
                : 0;

            string[] invalidNames = { null, "", "   ", "..", "../evil", "a/b", "a\\b", "sub/../evil" };
            foreach (string invalidName in invalidNames)
            {
                string display = invalidName == null ? "<null>" : "\"" + invalidName + "\"";
                Assert.IsFalse(SaveSystem.SaveGame(world, invalidName), "非法存档名应被拒绝: " + display);
            }

            int filesAfter = Directory.Exists(saveDir)
                ? Directory.GetFiles(saveDir, "*", SearchOption.AllDirectories).Length
                : 0;
            Assert.AreEqual(filesBefore, filesAfter, "非法存档名不应创建任何文件");
            Assert.IsFalse(File.Exists(Path.Combine(Application.persistentDataPath, "evil.sav")),
                "路径穿越（../evil）不应写出 Saves 目录");
            Assert.IsFalse(File.Exists(SaveFilePath("evil")), "非法存档名不应在 Saves 目录里留下文件");
        }

        /// <summary>
        /// 写盘失败路径：在 .tmp 路径上放一个同名目录，让 File.WriteAllText 因“目标是目录”抛异常。
        /// 断言失败调用返回 false、旧档字节完全不变、且没有 .tmp 文件残留。
        /// （构造方式依赖“目标路径是目录时写文件必抛异常”，Windows/编辑器下成立。）
        /// </summary>
        [Test]
        public void SaveGame_WriteFailure_PreservesExistingSave()
        {
            // 失败路径会命中 Debug.LogError：这是预期行为，不让该日志把测试判失败。
            LogAssert.ignoreFailingMessages = true;

            var world = NewWorld();
            world.currentYear = 321;
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：首次存盘应成功");
            Assert.IsTrue(File.Exists(SaveFilePath(SaveName)), "前置：旧档应存在");
            byte[] oldBytes = File.ReadAllBytes(SaveFilePath(SaveName));

            string tempPath = TempFilePath(SaveName);
            Directory.CreateDirectory(tempPath);
            try
            {
                world.currentYear = 999;
                Assert.IsFalse(SaveSystem.SaveGame(world, SaveName), "临时文件路径不可写时存盘应返回 false");
                Assert.IsTrue(File.Exists(SaveFilePath(SaveName)), "写盘失败不得删除旧档");
                CollectionAssert.AreEqual(oldBytes, File.ReadAllBytes(SaveFilePath(SaveName)),
                    "写盘失败后旧档字节必须保持原样（不能被半截临时内容覆盖）");
                Assert.AreEqual(0,
                    Directory.GetFiles(SaveSystem.GetSaveDirectory(), SaveName + "*.tmp").Length,
                    "失败路径不应留下 .tmp 文件");
            }
            finally
            {
                if (Directory.Exists(tempPath)) Directory.Delete(tempPath, true);
            }

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "写盘失败后旧档应仍可正常读取");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(321, loaded.currentYear, "旧档内容应仍是写盘失败前的年份");
        }

        /// <summary>
        /// .bak 恢复：模拟 File.Replace“旧档已挪到 .bak、新档未落位”的半失败状态，
        /// LoadGame 应能从 .bak 读回世界并把备份复制回 .sav；ListSaves 也不能把这个档静默隐藏。
        /// </summary>
        [Test]
        public void LoadGame_MissingMainFile_RecoversFromBackup()
        {
            var world = NewWorld();
            world.currentYear = 456;
            int realmCount = world.realms.Count;
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            // 成功存盘后 .bak 已清理，这里手工构造“只剩 .bak”的半失败状态。
            string savePath = SaveFilePath(SaveName);
            string backupPath = BackupFilePath(SaveName);
            byte[] savedBytes = File.ReadAllBytes(savePath);
            File.Copy(savePath, backupPath, true);
            File.Delete(savePath);
            Assert.IsFalse(File.Exists(savePath), "前置：主档应已缺失");
            Assert.IsTrue(File.Exists(backupPath), "前置：备份应存在");

            CollectionAssert.Contains(SaveSystem.ListSaves(), SaveName,
                "只有 .bak 的存档也应被 ListSaves 列出（不能被静默隐藏）");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "主文件缺失时应能从 .bak 恢复");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(456, loaded.currentYear, "恢复出的世界内容应与备份一致");
            Assert.AreEqual(realmCount, loaded.realms.Count, "恢复后政权数应一致");

            Assert.IsTrue(File.Exists(savePath), "恢复成功后应把 .bak 复制回 .sav");
            CollectionAssert.AreEqual(savedBytes, File.ReadAllBytes(savePath), "恢复出的 .sav 应与备份字节一致");
            Assert.IsTrue(File.Exists(backupPath), "恢复后 .bak 应保留（不作为唯一副本被删除）");
        }

        /// <summary>
        /// 读/删入口的 saveName 校验：非法名必须被拒绝，且不能把 Saves 目录之外的文件删掉（或读到）。
        /// </summary>
        [Test]
        public void LoadGameAndDeleteSave_InvalidSaveName_AreRejected()
        {
            // 非法名会命中 Debug.LogError：预期行为，不让日志把测试判失败。
            LogAssert.ignoreFailingMessages = true;

            var world = NewWorld();
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：合法存盘应成功");

            string guardPath = GuardFilePath;
            File.WriteAllText(guardPath, "guard");
            try
            {
                string[] invalidNames = { null, "", "   ", "..", "../__atomic_write_guard", "a/b", "a\\b" };
                foreach (string invalidName in invalidNames)
                {
                    string display = invalidName == null ? "<null>" : "\"" + invalidName + "\"";
                    Assert.IsNull(SaveSystem.LoadGame(invalidName), "非法存档名的 LoadGame 应返回 null: " + display);
                    Assert.IsFalse(SaveSystem.DeleteSave(invalidName), "非法存档名的 DeleteSave 应返回 false: " + display);
                }

                Assert.IsTrue(File.Exists(guardPath), "非法名 DeleteSave 不得删除 Saves 目录之外的文件");
                Assert.AreEqual("guard", File.ReadAllText(guardPath), "哨兵文件内容不应被改动");
                Assert.IsTrue(File.Exists(SaveFilePath(SaveName)), "非法名操作不得影响合法存档");
            }
            finally
            {
                TryDeleteGuardFile();
            }
        }

        /// <summary>
        /// 陈旧临时档：预置“只读”的 {save}.sav.tmp（模拟崩溃残留）与 {save}.sav.restore.tmp。
        /// 若 SaveGame 开头不显式清理，File.WriteAllText 会因 .tmp 只读而失败（返回 false），
        /// .restore.tmp 也会一直残留 → 用例对“写前清理”实现敏感（删掉清理行会真红）。
        /// </summary>
        [Test]
        public void SaveGame_CleansStaleTemporaryFileBeforeWrite()
        {
            var world = NewWorld();
            string saveDir = SaveSystem.GetSaveDirectory();
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);

            // 只读 .tmp：只有 SaveGame 开头的显式清理（先复位只读属性）才能让后续写档成功。
            string tempPath = TempFilePath(SaveName);
            File.WriteAllText(tempPath, "stale-crash-leftover");
            File.SetAttributes(tempPath, File.GetAttributes(tempPath) | FileAttributes.ReadOnly);
            // 崩溃在 RestoreSaveFromBackup 的复制之后会留下 restore.tmp：只有显式清理能清掉。
            string restoreTempPath = SaveFilePath(SaveName) + ".restore.tmp";
            File.WriteAllText(restoreTempPath, "stale-restore-leftover");

            try
            {
                world.currentYear = 654;
                Assert.IsTrue(SaveSystem.SaveGame(world, SaveName),
                    "存盘应成功：陈旧只读 .tmp 必须被写前清理，否则写临时档会抛异常");

                Assert.IsFalse(File.Exists(tempPath), "存盘后应已清理陈旧 .tmp");
                Assert.IsFalse(File.Exists(restoreTempPath), "存盘后应已清理崩溃残留的 .restore.tmp");
                Assert.AreEqual(0,
                    Directory.GetFiles(saveDir, SaveName + "*.tmp").Length,
                    "存盘后不应有任何 .tmp 残留");
                StringAssert.DoesNotContain("stale-crash-leftover", File.ReadAllText(SaveFilePath(SaveName)),
                    ".sav 内容不应来自陈旧临时档");

                var loaded = SaveSystem.LoadGame(SaveName);
                Assert.IsNotNull(loaded, "清理陈旧临时档后读档应成功");
                _objects.Add(loaded.gameObject);
                Assert.AreEqual(654, loaded.currentYear, "读档应读到本次写入的年份");
            }
            finally
            {
                // 若实现回退（SaveGame 返回 false），清掉只读陈旧档，避免污染后续用例。
                ClearReadOnlyAndDelete(tempPath);
                ClearReadOnlyAndDelete(restoreTempPath);
            }
        }

        /// <summary>
        /// 损坏 .bak 的失败路径（对实现敏感：用 LogAssert.Expect 断言“确实尝试过备份回退”的 Warning）：
        /// ① 主档缺失 + .bak 损坏 → 必须尝试回退，失败保留 .bak、不产生 .sav；
        /// ② 主档也损坏 → 必须从“主档解析失败”回退到 .bak，失败保留 .bak 与损坏主档（不改写、不删除）。
        /// 旧实现（不做回退）不会打这些 Warning，本用例会真红。
        /// </summary>
        [Test]
        public void LoadGame_CorruptBackup_KeepsBackupAndReturnsNull()
        {
            // 损坏档解析会如期待打 Error（只关注下面的 Warning 契约），用 ignore 隔离。
            LogAssert.ignoreFailingMessages = true;

            string saveDir = SaveSystem.GetSaveDirectory();
            if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
            SaveSystem.DeleteSave(SaveName); // 确保从“主档缺失”状态开始

            string savePath = SaveFilePath(SaveName);
            string backupPath = BackupFilePath(SaveName);
            File.WriteAllText(backupPath, "not-a-valid-save");
            Assert.IsTrue(File.Exists(backupPath), "前置：应已构造损坏的 .bak");

            // ① 主档缺失：实现必须尝试备份回退（否则不会打这条 Warning）。
            LogAssert.Expect(LogType.Warning,
                "[SaveSystem] 存档主文件缺失，尝试从备份恢复: " + backupPath);
            Assert.IsNull(SaveSystem.LoadGame(SaveName), "损坏的 .bak 不应读出世界");
            Assert.IsTrue(File.Exists(backupPath), "恢复失败必须保留 .bak");
            Assert.IsFalse(File.Exists(savePath), "恢复失败不应在 .sav 位置留下半截恢复档");

            // ② 主档也损坏：实现必须从“主档解析失败”回退到 .bak（否则不会打第二条 Warning）。
            File.WriteAllText(savePath, "corrupt-main");
            LogAssert.Expect(LogType.Warning,
                "[SaveSystem] 主存档解析失败，尝试从备份恢复: " + backupPath);
            Assert.IsNull(SaveSystem.LoadGame(SaveName), "主档与备份都损坏时应返回 null");
            Assert.IsTrue(File.Exists(backupPath), "恢复失败必须保留 .bak");
            Assert.AreEqual("corrupt-main", File.ReadAllText(savePath),
                "恢复失败不得删除/改写损坏的主档");
        }

        /// <summary>
        /// 损坏主档 + 有效备份：.sav 存在但解析失败时应回退读 .bak，
        /// 用有效备份恢复主档并返回世界（而不是直接返回 null）。
        /// </summary>
        [Test]
        public void LoadGame_CorruptMainFile_RecoversFromValidBackup()
        {
            // 损坏主档解析失败可能由 JsonUtility 打 Error：只验证回退结果，不让该日志干扰。
            LogAssert.ignoreFailingMessages = true;

            var world = NewWorld();
            world.currentYear = 789;
            int realmCount = world.realms.Count;
            Assert.IsTrue(SaveSystem.SaveGame(world, SaveName), "前置：存盘应成功");

            // 有效备份 + 损坏主档：模拟写到一半/被外部改坏的 .sav。
            string savePath = SaveFilePath(SaveName);
            string backupPath = BackupFilePath(SaveName);
            byte[] validBytes = File.ReadAllBytes(savePath);
            File.Copy(savePath, backupPath, true);
            File.WriteAllText(savePath, "corrupt-main-not-json");
            Assert.IsTrue(File.Exists(savePath), "前置：损坏主档应存在");
            Assert.IsTrue(File.Exists(backupPath), "前置：有效备份应存在");

            var loaded = SaveSystem.LoadGame(SaveName);
            Assert.IsNotNull(loaded, "损坏主档 + 有效 .bak 时应从备份恢复成功");
            _objects.Add(loaded.gameObject);
            Assert.AreEqual(789, loaded.currentYear, "恢复出的世界内容应与备份一致");
            Assert.AreEqual(realmCount, loaded.realms.Count, "恢复后政权数应一致");

            CollectionAssert.AreEqual(validBytes, File.ReadAllBytes(savePath),
                "损坏主档应被有效备份恢复替换");
            Assert.IsTrue(File.Exists(backupPath), "恢复后 .bak 应保留");
        }
    }
}
