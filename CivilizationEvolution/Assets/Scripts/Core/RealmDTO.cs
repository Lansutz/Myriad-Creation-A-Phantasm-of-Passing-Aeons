using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using System.Collections.Generic;
using System;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Core
{
    [Serializable]
    public class RealmDTO
    {
        public int realmId;
        public string realmName;
        /// <summary>政体七维成分组合（整体序列化；GovernmentComposition 及其成员类全部 [Serializable]，无 Dictionary）</summary>
        public GovernmentComposition composition = new GovernmentComposition();
        public float treasury;
        public float prestige;
        public float stability;
        public float centralization;
        public TaxSystemDTO taxSystem = new TaxSystemDTO();
        public CurrencySystem currencySystem = new CurrencySystem();
        public List<IntFloatEntry> classRelations = new List<IntFloatEntry>();
        public List<int> coreTiles = new List<int>();
        public List<int> claimedTiles = new List<int>();
        public int suzerainId = -1;
        public List<int> vassalIds = new List<int>();

        public static RealmDTO FromRealmData(RealmData r)
        {
            var dto = new RealmDTO
            {
                realmId = r.realmId,
                realmName = r.realmName,
                composition = r.composition,
                treasury = r.treasury,
                prestige = r.prestige,
                stability = r.stability,
                centralization = r.centralization,
                taxSystem = TaxSystemDTO.FromTaxSystem(r.taxSystem),
                currencySystem = r.currencySystem,
                suzerainId = r.suzerainId
            };
            if (r.classRelations != null)
                foreach (var kv in r.classRelations) dto.classRelations.Add(new IntFloatEntry((int)kv.Key, kv.Value));
            if (r.coreTiles != null)
                foreach (int t in r.coreTiles) dto.coreTiles.Add(t);
            if (r.claimedTiles != null)
                foreach (int t in r.claimedTiles) dto.claimedTiles.Add(t);
            if (r.vassalIds != null)
                dto.vassalIds.AddRange(r.vassalIds);
            return dto;
        }

        public RealmData ToRealmData()
        {
            // 构造函数会初始化 classRelations 默认值与 taxSystem/currencySystem，随后整体覆盖
            var r = new RealmData
            {
                realmId = realmId,
                realmName = realmName,
                composition = composition,
                treasury = treasury,
                prestige = prestige,
                stability = stability,
                centralization = centralization,
                taxSystem = taxSystem.ToTaxSystem(),
                currencySystem = currencySystem,
                suzerainId = suzerainId
            };
            r.classRelations.Clear();
            foreach (var e in classRelations) r.classRelations[(GameEnums.SocialClass)e.key] = e.value;
            r.coreTiles.Clear();
            foreach (int t in coreTiles) r.coreTiles.Add(t);
            r.claimedTiles.Clear();
            foreach (int t in claimedTiles) r.claimedTiles.Add(t);
            r.vassalIds.Clear();
            r.vassalIds.AddRange(vassalIds);
            return r;
        }
    }
}
