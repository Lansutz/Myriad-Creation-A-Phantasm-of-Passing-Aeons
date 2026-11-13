using System.Collections.Generic;
using MyriadCreation.Simulation.Politics;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>头衔实例注册表入档；旧档无该段时清空静态表，避免上一局持有者残留。</summary>
    public static class TitlePersistence
    {
        public static List<TitleInstanceSaveData> Capture()
        {
            var result = new List<TitleInstanceSaveData>();
            foreach (TitleInstance instance in TitleInstanceRegistry.All)
            {
                if (instance == null) continue;
                var holders = new List<TitleHoldingSaveData>();
                foreach (TitleHolding holding in instance.holders)
                {
                    if (holding == null) continue;
                    holders.Add(new TitleHoldingSaveData
                    {
                        characterId = holding.characterId,
                        institutionId = holding.institutionId,
                        role = (int)holding.role,
                        share = holding.share,
                        sinceDay = holding.sinceDay,
                        endDay = holding.endDay,
                        legitimacy = holding.legitimacy
                    });
                }
                result.Add(new TitleInstanceSaveData
                {
                    titleId = instance.titleId,
                    definitionId = instance.definitionId,
                    holders = holders.ToArray()
                });
            }
            return result;
        }

        public static void Restore(List<TitleInstanceSaveData> saved)
        {
            TitleInstanceRegistry.Clear();
            if (saved == null) return;
            foreach (TitleInstanceSaveData dto in saved)
            {
                if (dto == null || string.IsNullOrEmpty(dto.titleId)) continue;
                TitleInstance instance = TitleInstanceRegistry.GetOrCreate(dto.titleId);
                instance.definitionId = dto.definitionId ?? string.Empty;
                if (dto.holders == null) continue;
                foreach (TitleHoldingSaveData savedHolding in dto.holders)
                {
                    if (savedHolding == null) continue;
                    instance.AddHolding(new TitleHolding
                    {
                        characterId = savedHolding.characterId,
                        institutionId = savedHolding.institutionId,
                        role = (TitleHolderRole)savedHolding.role,
                        share = savedHolding.share,
                        sinceDay = savedHolding.sinceDay,
                        endDay = savedHolding.endDay,
                        legitimacy = savedHolding.legitimacy
                    });
                }
            }
        }
    }
}
