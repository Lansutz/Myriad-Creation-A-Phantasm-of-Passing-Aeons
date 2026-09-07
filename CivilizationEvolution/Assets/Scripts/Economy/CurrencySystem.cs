using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    [System.Serializable]
    public class CurrencySystem
    {
        public GameEnums.CurrencyStage currentStage = GameEnums.CurrencyStage.Barter;
        public string currencyName = "";
        public float goldReserve = 0f;
        public float silverReserve = 0f;
        public float coinPurity = 1.0f;
        public float paperMoneyInCirculation = 0f;
        public float inflationRate = 0f;

        /// <summary>计算货币价值</summary>
        public float GetCurrencyValue()
        {
            return currentStage switch
            {
                GameEnums.CurrencyStage.Barter => 1f,
                GameEnums.CurrencyStage.Bullion => 1f,
                GameEnums.CurrencyStage.MintedCoin => coinPurity,
                GameEnums.CurrencyStage.PaperMoney => Mathf.Clamp(
                    (goldReserve + silverReserve * 0.1f) / Mathf.Max(1f, paperMoneyInCirculation),
                    0.01f, 2f),
                _ => 1f
            };
        }

        /// <summary>铸造劣币</summary>
        public void DebaseCoin(float purityReduction, float amountMinted)
        {
            coinPurity = Mathf.Max(0.1f, coinPurity - purityReduction);
            inflationRate += purityReduction * 0.5f;
        }

        /// <summary>发行纸币</summary>
        public bool IssuePaperMoney(float amount)
        {
            float reserveValue = goldReserve + silverReserve * 0.1f;
            float maxIssue = reserveValue * 3f;
            if (paperMoneyInCirculation + amount > maxIssue) return false;

            paperMoneyInCirculation += amount;
            if (paperMoneyInCirculation > reserveValue * 1.5f)
                inflationRate += (paperMoneyInCirculation / reserveValue - 1.5f) * 0.1f;
            return true;
        }

        /// <summary>每日通胀衰减</summary>
        public void DailyTick()
        {
            inflationRate = Mathf.Max(0f, inflationRate - 0.001f);
        }
    }
}
