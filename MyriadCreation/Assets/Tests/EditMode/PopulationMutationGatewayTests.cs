using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.5 C 门禁：人口块 count 的增删缩放只能经 PopulationLedger（阶层再平衡除外）。</summary>
    public class PopulationMutationGatewayTests
    {
        private static readonly Regex CountMutation = new Regex(@"\.count\s*(\*=|\+=|-=)");

        [Test]
        public void NoDirectPopulationCountMutations()
        {
            string root = Path.Combine(Application.dataPath, "Scripts");
            var violations = new List<string>();
            foreach (string file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name == "PopulationLedger.cs" || name == "SocialDifferentiation.cs") continue;

                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (CountMutation.IsMatch(lines[i]))
                    {
                        violations.Add($"{name}:{i + 1} 直接改人口：{lines[i].Trim()}");
                    }
                }
            }

            Assert.IsEmpty(violations, "人口块 count 必须走 PopulationLedger：\n" + string.Join("\n", violations));
        }
    }
}
