using CivilizationEvolution.Politics;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace CivilizationEvolution.Thought
{
    [System.Serializable]
    public class LawSystem
    {
        public int lawSystemId;
        public string lawSystemName;
        public LawSource source = LawSource.Customary;

 // 法律条文        public List<Law> laws = new List<Law>();

 // 罪行定义        [NonSerialized] public Dictionary<CrimeType, CrimeDefinition> crimes = new Dictionary<CrimeType, CrimeDefinition>();


 // 司法效率        public float judicialEfficiency = 0.5f;
        public float corruption = 0.2f;
        public float lawEnforcement = 0.5f;

 // 刑罚偏好        public float severity = 0.5f; // 刑罚严厉程度
        public bool useCapitalPunishment = true;
        public bool useCorporalPunishment = true;
        public bool useFines = true;
        public bool useImprisonment = true;
        public bool useExile = true;
        public bool useSlavery = true;

        public LawSystem()
        {
            InitializeDefaultCrimes();
        }

        private void InitializeDefaultCrimes()
        {
            crimes[CrimeType.Murder] = new CrimeDefinition
            {
                type = CrimeType.Murder,
                name = "谋杀",
                baseSeverity = 100f,
                defaultPunishment = PunishmentType.Death
            };
            crimes[CrimeType.Treason] = new CrimeDefinition
            {
                type = CrimeType.Treason,
                name = "叛国",
                baseSeverity = 100f,
                defaultPunishment = PunishmentType.Death
            };
            crimes[CrimeType.Theft] = new CrimeDefinition
            {
                type = CrimeType.Theft,
                name = "盗窃",
                baseSeverity = 30f,
                defaultPunishment = PunishmentType.Fine
            };
            crimes[CrimeType.Assault] = new CrimeDefinition
            {
                type = CrimeType.Assault,
                name = "伤害",
                baseSeverity = 40f,
                defaultPunishment = PunishmentType.Fine
            };
            crimes[CrimeType.Blasphemy] = new CrimeDefinition
            {
                type = CrimeType.Blasphemy,
                name = "亵渎",
                baseSeverity = 60f,
                defaultPunishment = PunishmentType.Exile
            };
            crimes[CrimeType.Heresy] = new CrimeDefinition
            {
                type = CrimeType.Heresy,
                name = "异端",
                baseSeverity = 80f,
                defaultPunishment = PunishmentType.Death
            };
            crimes[CrimeType.TaxEvasion] = new CrimeDefinition
            {
                type = CrimeType.TaxEvasion,
                name = "逃税",
                baseSeverity = 25f,
                defaultPunishment = PunishmentType.Fine
            };
            crimes[CrimeType.Desertion] = new CrimeDefinition
            {
                type = CrimeType.Desertion,
                name = "逃兵",
                baseSeverity = 70f,
                defaultPunishment = PunishmentType.Death
            };
        }

 /// <summary>审判罪行</summary>        public TrialResult TrialCrime(CrimeType crime, int suspectCharacterId, int realmId, RealmData realm)
        {
            var result = new TrialResult();
            if (!crimes.TryGetValue(crime, out var crimeDef))
            {
                result.verdict = VerdictType.NotGuilty;
                return result;
            }

 // 定罪概率（受司法效率、腐败、嫌疑人身份影响）            float convictionChance = judicialEfficiency * (1f - corruption);

 // 贵族/神职人员有更高的脱罪概率 // 简化：假设身份影响            convictionChance *= 0.8f;

            result.verdict = UnityEngine.Random.value < convictionChance
                ? VerdictType.Guilty
                : VerdictType.NotGuilty;

            if (result.verdict == VerdictType.Guilty)
            {
 // 量刑                result.punishment = crimeDef.defaultPunishment;
                result.severity = crimeDef.baseSeverity * severity;

 // 罚款金额                if (result.punishment == PunishmentType.Fine)
                    result.fineAmount = crimeDef.baseSeverity * 10f;
            }

            return result;
        }

 /// <summary>每日法律Tick</summary>        public void DailyTick()
        {
            judicialEfficiency = Mathf.Clamp(judicialEfficiency + UnityEngine.Random.Range(-0.001f, 0.001f), 0.1f, 1f);
        }
    }
}
