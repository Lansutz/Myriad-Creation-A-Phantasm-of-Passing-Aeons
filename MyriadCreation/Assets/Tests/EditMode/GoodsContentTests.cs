using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Content.Definitions;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>P2.4：商品表外提后的完整性、属性合并与自然/活物转换。</summary>
    public class GoodsContentTests
    {
        [Serializable]
        private class Wrapper
        {
            public List<GoodsContentDef> goods = new List<GoodsContentDef>();
        }

        [TearDown]
        public void TearDown()
        {
            GoodsCatalog.ResetContent();
        }

        [Test]
        public void ShippedGoods_AreCompleteAndUnique()
        {
            string path = Path.Combine(Application.dataPath, "StreamingAssets/Base/Goods/Goods.json");
            Assert.IsTrue(File.Exists(path), "找不到 Goods.json: " + path);
            Wrapper wrapper = JsonUtility.FromJson<Wrapper>(File.ReadAllText(path));
            Assert.IsNotNull(wrapper);
            Assert.AreEqual(57, wrapper.goods.Count);

            var ids = new HashSet<int>();
            foreach (GoodsContentDef def in wrapper.goods)
            {
                Assert.GreaterOrEqual(def.goodsId, 0);
                Assert.IsTrue(ids.Add(def.goodsId), "重复商品 id: " + def.goodsId);
                Assert.IsFalse(string.IsNullOrEmpty(def.goodsName));
                Assert.IsTrue(Enum.IsDefined(typeof(MyriadCreation.Core.Enums.GameEnums.GoodsCategory), def.category));
                if (def.isNaturalResource)
                {
                    Assert.Greater(def.baseAbundance, 0f, "自然资源应有丰度: " + def.goodsId);
                    Assert.IsTrue(Enum.IsDefined(typeof(ResourceType), def.resourceType));
                }
            }
        }

        [Test]
        public void LoadFromContent_ConvertsMarksAndDefaultsBiomes()
        {
            GoodsCatalog.LoadFromContent(new List<GoodsContentDef>
            {
                new GoodsContentDef
                {
                    goodsId = 50,
                    goodsName = "外提铁矿",
                    category = (int)MyriadCreation.Core.Enums.GameEnums.GoodsCategory.MetalOre,
                    baseValue = 2f,
                    weight = 0.4f,
                    isNaturalResource = true,
                    resourceType = (int)ResourceType.Mineral,
                    baseAbundance = 0.4f,
                    requiredInnovation = -1
                }
            });

            Assert.IsTrue(GoodsCatalog.IsLoaded);
            GoodsDef def = GoodsCatalog.All[50];
            Assert.AreEqual("外提铁矿", def.goodsName);
            Assert.IsTrue(def.isNaturalResource);
            Assert.AreEqual(ResourceType.Mineral, def.resourceType);
            Assert.Greater(def.originBiomes.Count, 0);
        }
    }
}
