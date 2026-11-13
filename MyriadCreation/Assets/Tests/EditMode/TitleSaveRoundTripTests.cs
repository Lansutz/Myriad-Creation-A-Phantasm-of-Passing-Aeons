using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MyriadCreation.Infrastructure.Save;
using MyriadCreation.Simulation.Politics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>头衔实例存档：DTO JsonUtility 往返 + 重建到注册表（旧持有保留结束日）。</summary>
    public class TitleSaveRoundTripTests
    {
        [TearDown]
        public void TearDown()
        {
            TitleInstanceRegistry.Clear();
        }

        [Test]
        public void TitleInstance_RoundTripsThroughSaveDto()
        {
            var instance = new TitleInstance("realm_9_primary");
            instance.AddHolding(new TitleHolding { characterId = 1, role = TitleHolderRole.Sole, sinceDay = 10, endDay = 100 });
            instance.AddHolding(new TitleHolding { characterId = 2, role = TitleHolderRole.CoRuler, share = 0.5f, sinceDay = 50 });
            instance.AddHolding(new TitleHolding { institutionId = 3, role = TitleHolderRole.Elector, sinceDay = 60 });

            var save = new TitleInstanceSaveData
            {
                titleId = instance.titleId,
                holders = new TitleHoldingSaveData[instance.holders.Count]
            };
            for (int i = 0; i < instance.holders.Count; i++)
            {
                TitleHolding h = instance.holders[i];
                save.holders[i] = new TitleHoldingSaveData
                {
                    characterId = h.characterId,
                    institutionId = h.institutionId,
                    role = (int)h.role,
                    share = h.share,
                    sinceDay = h.sinceDay,
                    endDay = h.endDay,
                    legitimacy = h.legitimacy
                };
            }

            string json = JsonUtility.ToJson(save);
            TitleInstanceSaveData restored = JsonUtility.FromJson<TitleInstanceSaveData>(json);

            Assert.AreEqual(3, restored.holders.Length);
            Assert.AreEqual(2, restored.holders[1].characterId);
            Assert.AreEqual((int)TitleHolderRole.CoRuler, restored.holders[1].role);
            Assert.AreEqual(0.5f, restored.holders[1].share);
            Assert.AreEqual(3, restored.holders[2].institutionId);

            TitleInstance rebuilt = TitleInstanceRegistry.GetOrCreate(restored.titleId);
            foreach (TitleHoldingSaveData h in restored.holders)
            {
                rebuilt.AddHolding(new TitleHolding
                {
                    characterId = h.characterId,
                    institutionId = h.institutionId,
                    role = (TitleHolderRole)h.role,
                    share = h.share,
                    sinceDay = h.sinceDay,
                    endDay = h.endDay,
                    legitimacy = h.legitimacy
                });
            }

            Assert.AreEqual(3, rebuilt.holders.Count);
            Assert.IsFalse(rebuilt.holders[0].IsActive(120), "结束的旧持有不应仍在任");
            Assert.IsNull(rebuilt.GetSeniorHolder(120), "CoRuler 不是 senior");
        }
    }
}
