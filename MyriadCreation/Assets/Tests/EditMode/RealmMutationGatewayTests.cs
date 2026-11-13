using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5 C 门禁：RealmData 的 treasury/stability 只能经 RealmTreasury/RealmStability 改写。</summary>
    public class RealmMutationGatewayTests
    {
        private static readonly Regex TreasuryWrite = new Regex(@"(?<![=!<>])\.treasury\s*(\+=|-=|=)(?!=)");
        private static readonly Regex StabilityWrite = new Regex(@"(?<![=!<>])\.stability\s*(\+=|-=|=)(?!=)");

        [Test]
        public void NoDirectRealmTreasuryOrStabilityWrites()
        {
            string root = Path.Combine(Application.dataPath, "Scripts/Simulation");
            Assert.IsTrue(Directory.Exists(root), "找不到 Simulation 源码目录：" + root);

            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name == "RealmTreasury.cs" || name == "RealmStability.cs") continue;

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (TreasuryWrite.IsMatch(line) && !line.Contains("report."))
                    {
                        violations.Add($"{name}:{i + 1} 直接写 treasury：{line.Trim()}");
                    }
                    if (StabilityWrite.IsMatch(line)
                        && !line.Contains("e.stability")
                        && !line.Contains("report.")
                        && !line.Contains("realmSituation.stability")
                        && !line.Contains("tile.stability")
                        && !line.Contains("_tiles[")
                        && !line.Contains("tiles["))
                    {
                        violations.Add($"{name}:{i + 1} 直接写 stability：{line.Trim()}");
                    }
                }
            }

            Assert.IsEmpty(violations, "RealmData 货币/稳定度必须走单一入口：\n" + string.Join("\n", violations));
        }
    }
}
