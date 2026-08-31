using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;

namespace CivilizationEvolution.Politics
{
    public static class GovernmentEffects
    {
        [Serializable]
        public class GovernmentEffectData
        {
            public string realmNameSuffix = "";
            public string realmNamePrefix = "";
            public float expansionism = 0f;
            public float centralization = 0.5f;
            public float stability = 0f;
            public float administrativeCapacity = 0.5f;
            public float taxEfficiency = 1f;
            public float tradeEfficiency = 1f;
            public float developmentSpeed = 1f;
            public float inflationResistance = 1f;
            public float manpowerRecruitment = 1f;
            public float armyMaintenanceCost = 1f;
            public float armyMorale = 1f;
            public float fortificationBonus = 0f;
            public float diplomaticReputation = 0f;
            public float allianceDesirability = 0f;
            public float aggressiveDiplomacy = 0f;
            public float vassalizationEfficiency = 1f;
            public List<string> requiredInnovations = new List<string>();
            public List<string> unlockedBuildings = new List<string>();
            public List<string> unlockedSettlementTypes = new List<string>();
        }

        [Serializable]
        public class RealmNameSuffix
        {
            public string id;
            public string suffix;
            public string prefix;
            public GovernmentConstraints.SupremeSovereignty sovereignty;
            public SupremeSuccession succession;
            public SupremeScope scope;
        }

        private static readonly List<RealmNameSuffix> _suffixTable = new List<RealmNameSuffix>
        {
            new RealmNameSuffix { id = "kingdom", suffix = "王国", sovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy, succession = SupremeSuccession.Hereditary, scope = SupremeScope.Absolute },
            new RealmNameSuffix { id = "empire", suffix = "帝国", sovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy, succession = SupremeSuccession.Hereditary, scope = SupremeScope.Absolute },
            new RealmNameSuffix { id = "duchy", suffix = "公国", sovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy, succession = SupremeSuccession.Hereditary, scope = SupremeScope.CustomBound },
            new RealmNameSuffix { id = "elective_monarchy", suffix = "选侯国", sovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy, succession = SupremeSuccession.ElectiveRepresentative, scope = SupremeScope.Absolute },
            new RealmNameSuffix { id = "theocracy", suffix = "神权国", sovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy, succession = SupremeSuccession.Divine, scope = SupremeScope.DivinelyBound },
            new RealmNameSuffix { id = "tyranny", suffix = "僭主国", sovereignty = GovernmentConstraints.SupremeSovereignty.Monarchy, succession = SupremeSuccession.Usurpation, scope = SupremeScope.Absolute },
            new RealmNameSuffix { id = "republic", suffix = "共和国", sovereignty = GovernmentConstraints.SupremeSovereignty.Republic, succession = SupremeSuccession.ElectiveDirect, scope = SupremeScope.Consensual },
            new RealmNameSuffix { id = "senate_republic", suffix = "元老院共和国", sovereignty = GovernmentConstraints.SupremeSovereignty.Republic, succession = SupremeSuccession.ElectiveRepresentative, scope = SupremeScope.LegallyBound },
            new RealmNameSuffix { id = "federation", suffix = "联邦", sovereignty = GovernmentConstraints.SupremeSovereignty.Republic, succession = SupremeSuccession.ElectiveRepresentative, scope = SupremeScope.Consensual },
            new RealmNameSuffix { id = "confederation", suffix = "邦联", sovereignty = GovernmentConstraints.SupremeSovereignty.Republic, succession = SupremeSuccession.Rotation, scope = SupremeScope.Consensual },
            new RealmNameSuffix { id = "free_city", suffix = "自由市", sovereignty = GovernmentConstraints.SupremeSovereignty.Republic, succession = SupremeSuccession.ElectiveDirect, scope = SupremeScope.LegallyBound },
        };

        public static RealmNameSuffix GetDefaultSuffix(GovernmentComposition comp)
        {
            foreach (var s in _suffixTable)
                if (s.sovereignty == comp.supremeSovereignty && s.succession == (SupremeSuccession)comp.supremeSuccession.primary && s.scope == (SupremeScope)comp.supremeScope.primary)
                    return s;
            foreach (var s in _suffixTable)
                if (s.sovereignty == comp.supremeSovereignty)
                    return s;
            return new RealmNameSuffix { id = "generic", suffix = "政权" };
        }

        public static List<RealmNameSuffix> GetAllSuffixes() { return new List<RealmNameSuffix>(_suffixTable); }
        public static void AddCustomSuffix(RealmNameSuffix suffix) { _suffixTable.Add(suffix); }

        public static GovernmentEffectData CalculateEffects(GovernmentComposition comp)
        {
            var e = new GovernmentEffectData();
            var def = GetDefaultSuffix(comp);
            e.realmNameSuffix = def.suffix;
            e.realmNamePrefix = def.prefix;
            CalcSupreme(comp, e);
            CalcCentral(comp, e);
            CalcLocal(comp, e);
            CalcSpatial(comp, e);
            CalcInnovations(comp, e);
            CalcUnlocked(comp, e);
            return e;
        }

        private static void CalcSupreme(GovernmentComposition comp, GovernmentEffectData e)
        {
            var sov = comp.supremeSovereignty;
            var suc = (SupremeSuccession)comp.supremeSuccession.primary;
            var scp = (SupremeScope)comp.supremeScope.primary;
            if (sov == GovernmentConstraints.SupremeSovereignty.Monarchy)
            { e.centralization += 0.1f; e.armyMorale += 0.05f; e.diplomaticReputation += 5f; }
            else { e.tradeEfficiency += 0.15f; e.developmentSpeed += 0.1f; e.allianceDesirability += 5f; e.armyMaintenanceCost -= 0.1f; }
            switch (suc)
            {
                case SupremeSuccession.Hereditary: e.stability += 10f; e.centralization += 0.1f; e.expansionism += 0.1f; break;
                case SupremeSuccession.ElectiveDirect: e.stability -= 5f; e.tradeEfficiency += 0.1f; e.manpowerRecruitment += 0.1f; break;
                case SupremeSuccession.ElectiveRepresentative: e.stability += 5f; e.diplomaticReputation += 5f; e.taxEfficiency += 0.05f; break;
                case SupremeSuccession.Usurpation: e.expansionism += 0.2f; e.stability -= 15f; e.armyMorale += 0.1f; e.aggressiveDiplomacy += 10f; break;
                case SupremeSuccession.Rotation: e.stability -= 10f; e.centralization -= 0.15f; e.allianceDesirability += 5f; break;
                case SupremeSuccession.Divine: e.stability += 15f; e.centralization += 0.15f; e.diplomaticReputation -= 5f; e.inflationResistance += 0.1f; break;
            }
            switch (scp)
            {
                case SupremeScope.Absolute: e.centralization += 0.2f; e.taxEfficiency += 0.1f; e.expansionism += 0.1f; e.stability -= 5f; break;
                case SupremeScope.LegallyBound: e.stability += 5f; e.tradeEfficiency += 0.05f; e.diplomaticReputation += 5f; break;
                case SupremeScope.CustomBound: e.centralization -= 0.1f; e.stability += 10f; e.armyMaintenanceCost -= 0.05f; break;
                case SupremeScope.Consensual: e.stability += 10f; e.allianceDesirability += 10f; e.manpowerRecruitment += 0.1f; break;
                case SupremeScope.DivinelyBound: e.stability += 15f; e.centralization += 0.1f; e.inflationResistance += 0.15f; break;
            }
            foreach (var sec in comp.supremeSuccession.secondary)
            {
                var ss = (SupremeSuccession)sec;
                if (ss == SupremeSuccession.Usurpation) { e.expansionism += 0.05f; e.stability -= 3f; }
                else if (ss == SupremeSuccession.ElectiveRepresentative) { e.diplomaticReputation += 3f; e.stability += 3f; }
            }
            if (comp.titleDistribution == GovernmentConstraints.TitleDistribution.FamilyShared) { e.centralization -= 0.15f; e.stability -= 5f; e.expansionism += 0.05f; }
            else if (comp.titleDistribution == GovernmentConstraints.TitleDistribution.NoTitle) { e.centralization -= 0.1f; e.tradeEfficiency += 0.1f; }
            if (comp.domainDistribution == GovernmentConstraints.DomainDistribution.Partible) { e.centralization -= 0.2f; e.stability -= 10f; e.expansionism += 0.1f; }
            else if (comp.domainDistribution == GovernmentConstraints.DomainDistribution.Appanage) { e.centralization -= 0.1f; e.stability += 5f; }
        }

        private static void CalcCentral(GovernmentComposition comp, GovernmentEffectData e)
        {
            if (comp.centralExistence == GovernmentConstraints.CentralExistence.None)
            { e.centralization -= 0.3f; e.taxEfficiency -= 0.2f; e.administrativeCapacity -= 0.2f; e.stability -= 5f; return; }
            var cs = (CentralSuccession)comp.centralSuccession.primary;
            switch (cs)
            {
                case CentralSuccession.Appointed: e.centralization += 0.1f; e.administrativeCapacity += 0.1f; break;
                case CentralSuccession.Elected: e.stability += 5f; e.tradeEfficiency += 0.05f; break;
                case CentralSuccession.Examination: e.administrativeCapacity += 0.2f; e.taxEfficiency += 0.1f; e.developmentSpeed += 0.1f; break;
                case CentralSuccession.Hereditary: e.centralization -= 0.1f; e.stability += 5f; e.administrativeCapacity -= 0.1f; break;
            }
            var ins = (CentralInstitution)comp.centralInstitution.primary;
            switch (ins)
            {
                case CentralInstitution.None: e.administrativeCapacity -= 0.2f; e.taxEfficiency -= 0.15f; break;
                case CentralInstitution.Court: e.centralization += 0.05f; e.diplomaticReputation += 3f; break;
                case CentralInstitution.Assembly: e.stability += 10f; e.tradeEfficiency += 0.1f; e.allianceDesirability += 5f; break;
                case CentralInstitution.EldersCouncil: e.stability += 15f; e.centralization -= 0.05f; break;
                case CentralInstitution.BureaucraticCore: e.administrativeCapacity += 0.2f; e.taxEfficiency += 0.15f; e.developmentSpeed += 0.1f; e.centralization += 0.1f; break;
                case CentralInstitution.ReligiousCouncil: e.stability += 10f; e.inflationResistance += 0.1f; e.diplomaticReputation -= 3f; break;
                case CentralInstitution.MilitaryCouncil: e.armyMorale += 0.15f; e.expansionism += 0.15f; e.aggressiveDiplomacy += 10f; e.stability -= 5f; break;
            }
        }

        private static void CalcLocal(GovernmentComposition comp, GovernmentEffectData e)
        {
            var ls = (LocalSuccession)comp.localSuccession.primary;
            var lsc = (LocalScope)comp.localScope.primary;
            switch (ls)
            {
                case LocalSuccession.Appointed: e.centralization += 0.1f; e.administrativeCapacity += 0.05f; break;
                case LocalSuccession.Elected: e.stability += 5f; e.developmentSpeed += 0.05f; break;
                case LocalSuccession.Examination: e.administrativeCapacity += 0.15f; e.taxEfficiency += 0.1f; break;
                case LocalSuccession.Hereditary: e.centralization -= 0.15f; e.stability += 5f; e.armyMaintenanceCost -= 0.1f; e.expansionism += 0.05f; break;
                case LocalSuccession.CityCharter: e.tradeEfficiency += 0.2f; e.developmentSpeed += 0.15f; e.centralization -= 0.1f; break;
            }
            switch (lsc)
            {
                case LocalScope.FullAutonomy: e.centralization -= 0.2f; e.stability += 10f; e.taxEfficiency -= 0.15f; e.developmentSpeed += 0.1f; break;
                case LocalScope.FiscalJudicial: e.centralization -= 0.05f; e.taxEfficiency += 0.05f; break;
                case LocalScope.MilitaryOnly: e.centralization += 0.05f; e.fortificationBonus += 0.1f; e.developmentSpeed -= 0.1f; break;
                case LocalScope.None: e.centralization += 0.2f; e.taxEfficiency += 0.1f; e.administrativeCapacity += 0.1f; e.stability -= 5f; break;
            }
        }

        private static void CalcSpatial(GovernmentComposition comp, GovernmentEffectData e)
        {
            var sp = (SpatialStructure)comp.spatialStructure.primary;
            switch (sp)
            {
                case SpatialStructure.Unitary: e.centralization += 0.2f; e.taxEfficiency += 0.1f; e.administrativeCapacity += 0.1f; e.stability += 5f; break;
                case SpatialStructure.Federal: e.centralization -= 0.1f; e.stability += 10f; e.developmentSpeed += 0.1f; e.allianceDesirability += 5f; break;
                case SpatialStructure.Confederal: e.centralization -= 0.25f; e.stability -= 10f; e.expansionism -= 0.1f; e.tradeEfficiency += 0.1f; break;
            }
        }

        private static void CalcInnovations(GovernmentComposition comp, GovernmentEffectData e)
        {
            var ins = (CentralInstitution)comp.centralInstitution.primary;
            if (ins == CentralInstitution.BureaucraticCore) { e.requiredInnovations.Add("bureaucracy"); e.requiredInnovations.Add("writing"); }
            if (ins == CentralInstitution.Assembly) e.requiredInnovations.Add("republican_traditions");
            if (ins == CentralInstitution.ReligiousCouncil) e.requiredInnovations.Add("organized_religion");
            if (ins == CentralInstitution.MilitaryCouncil) e.requiredInnovations.Add("military_reform");
            var ls = (LocalSuccession)comp.localSuccession.primary;
            if (ls == LocalSuccession.Examination) { e.requiredInnovations.Add("civil_service_exam"); e.requiredInnovations.Add("bureaucracy"); }
            if (ls == LocalSuccession.CityCharter) { e.requiredInnovations.Add("urban_charter"); e.requiredInnovations.Add("merchant_guilds"); }
            var sp = (SpatialStructure)comp.spatialStructure.primary;
            if (sp == SpatialStructure.Federal) e.requiredInnovations.Add("federalism");
            if (sp == SpatialStructure.Confederal) e.requiredInnovations.Add("confederation");
            if (comp.supremeSovereignty == GovernmentConstraints.SupremeSovereignty.Republic) e.requiredInnovations.Add("republican_government");
        }

        private static void CalcUnlocked(GovernmentComposition comp, GovernmentEffectData e)
        {
            var ins = (CentralInstitution)comp.centralInstitution.primary;
            if (ins == CentralInstitution.BureaucraticCore) { e.unlockedBuildings.Add("tax_office"); e.unlockedBuildings.Add("courthouse"); e.unlockedBuildings.Add("archive"); }
            if (ins == CentralInstitution.Assembly) { e.unlockedBuildings.Add("senate_house"); e.unlockedBuildings.Add("forum"); }
            if (ins == CentralInstitution.ReligiousCouncil) { e.unlockedBuildings.Add("temple_complex"); e.unlockedBuildings.Add("monastery"); }
            if (ins == CentralInstitution.MilitaryCouncil) { e.unlockedBuildings.Add("barracks"); e.unlockedBuildings.Add("military_academy"); e.unlockedBuildings.Add("fortress"); }
            var ls = (LocalSuccession)comp.localSuccession.primary;
            if (ls == LocalSuccession.CityCharter) { e.unlockedSettlementTypes.Add("free_city"); e.unlockedSettlementTypes.Add("trade_hub"); }
            if (ls == LocalSuccession.Hereditary) { e.unlockedSettlementTypes.Add("manor"); e.unlockedSettlementTypes.Add("castle_town"); }
            if (ls == LocalSuccession.Examination) { e.unlockedSettlementTypes.Add("administrative_center"); e.unlockedSettlementTypes.Add("scholar_town"); }
        }

        public static bool IsHighlyExpansionist(GovernmentComposition comp) { return CalculateEffects(comp).expansionism > 0.3f; }
        public static bool IsHighlyCentralized(GovernmentComposition comp) { return CalculateEffects(comp).centralization > 0.7f; }
        public static bool IsStable(GovernmentComposition comp) { return CalculateEffects(comp).stability > 0f; }
        public static string GetExpansionismDescription(GovernmentComposition comp)
        {
            var ex = CalculateEffects(comp).expansionism;
            if (ex > 0.3f) return "高度扩张";
            if (ex > 0.1f) return "倾向扩张";
            if (ex > -0.1f) return "平衡";
            if (ex > -0.3f) return "倾向内敛";
            return "高度内敛";
        }
    }
}
