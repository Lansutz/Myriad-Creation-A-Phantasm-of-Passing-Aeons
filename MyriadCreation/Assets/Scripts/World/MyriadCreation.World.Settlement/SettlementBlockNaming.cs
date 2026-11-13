using System;

namespace MyriadCreation.World.Settlement
{
    /// <summary>
    /// 区块/城墙的**生成式命名**（不写死具体历史名）。
    ///
    /// 设计口径（用户 2026-11-10）：区块名不是内容作者手写的固定字符串，而是由**人物系统**给出——
    /// 具体是"谁建的"或"当时在位的统治者"，名字取那个人物的称谓（必要时用尊号/王朝名）。
    ///
    /// 注意：**不要接文化命名表**。文化命名表生成的是"人名/地名"的词，而城墙名是
    /// "某人的墙"——归属来自人物，不来自词表。`builderName` 必须由调用方从
    /// `CharacterData`（或统治者尊号）解析后传入；`builderCharacterId` 是它的锚点。
    /// </summary>
    public static class SettlementBlockNaming
    {
        /// <summary>
        /// 生成显示名。
        /// </summary>
        /// <param name="kind">区块类别（城墙/区划/市场/建筑…）。</param>
        /// <param name="ordinal">同类中的序号（从 1 起），用于无建造者时区分。</param>
        /// <param name="builderName">
        /// **人物本名**（建造者或当时统治者的个人名，不是尊号/王朝名/头衔）。可为空。
        /// </param>
        public static string For(SettlementBlockKind kind, int ordinal, string builderName)
        {
            string who = string.IsNullOrWhiteSpace(builderName) ? string.Empty : builderName.Trim();
            int n = ordinal < 1 ? 1 : ordinal;

            switch (kind)
            {
                case SettlementBlockKind.Wall:
                    return who.Length > 0 ? $"{who}城墙" : $"第{n}道城墙";
                case SettlementBlockKind.Market:
                    return who.Length > 0 ? $"{who}市场" : $"第{n}市场";
                case SettlementBlockKind.District:
                    return who.Length > 0 ? $"{who}所辟城区" : $"第{n}区";
                case SettlementBlockKind.Building:
                    return who.Length > 0 ? $"{who}所建" : $"第{n}座建筑";
                default:
                    return who.Length > 0 ? $"{who}营建" : $"第{n}营建";
            }
        }
    }
}
