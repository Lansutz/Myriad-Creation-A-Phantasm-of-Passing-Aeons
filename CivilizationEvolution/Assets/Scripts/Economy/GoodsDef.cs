using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Economy
{
    public struct GoodsDef
    {
        public int goodsId;
        public string goodsName;
        public GameEnums.GoodsCategory category;
        public float baseValue;
        public float weight;
        public bool hasShelfLife;
        public float shelfLifeDays;
        public List<GameEnums.BiomeType> originBiomes;
        public int processedFromId;
        public float processingRatio;
    }
}
