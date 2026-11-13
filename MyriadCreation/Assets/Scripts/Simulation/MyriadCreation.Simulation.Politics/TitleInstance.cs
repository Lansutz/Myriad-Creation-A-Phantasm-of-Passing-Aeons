using System.Collections.Generic;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Simulation.Politics
{
    /// <summary>
    /// 运行时头衔实例（M1：模板 TitleDefinition → 实例 TitleInstance → 持有 TitleHolding）。
    /// 模板在 TitleManager；实例负责"谁在何时持有什么"，为继承/存档/显示提供真源。
    /// </summary>
    public sealed class TitleInstance
    {
        public readonly string titleId;

        /// <summary>绑定的模板 id（TitleManager 中的 TitleDefinition.titleId）。</summary>
        public string definitionId = "";

        public readonly List<TitleHolding> holders = new List<TitleHolding>();

        public TitleInstance(string titleId)
        {
            this.titleId = titleId ?? string.Empty;
        }

        public void AddHolding(TitleHolding holding)
        {
            if (holding == null)
            {
                return;
            }
            holders.Add(holding);
        }

        /// <summary>结束当前所有在任持有（不删除记录，便于沿革）。</summary>
        public void EndActiveHoldings(int day)
        {
            for (int i = 0; i < holders.Count; i++)
            {
                TitleHolding holding = holders[i];
                if (holding.IsActive(day))
                {
                    holding.endDay = day;
                    holders[i] = holding;
                }
            }
        }

        public TitleHolding GetSeniorHolder(int day)
        {
            for (int i = 0; i < holders.Count; i++)
            {
                TitleHolding holding = holders[i];
                if (holding.IsActive(day) && (holding.role == TitleHolderRole.Sole || holding.role == TitleHolderRole.Senior))
                {
                    return holding;
                }
            }
            return null;
        }

        /// <summary>继位/授予：结束旧在任者，写入 new Sole 持有。</summary>
        public TitleHolding TransferSenior(int characterId, int day, float legitimacy = 50f)
        {
            EndActiveHoldings(day);
            TitleHolding holding = new TitleHolding
            {
                characterId = characterId,
                role = TitleHolderRole.Sole,
                share = 1f,
                sinceDay = day,
                legitimacy = legitimacy
            };
            AddHolding(holding);
            return holding;
        }
    }
}
