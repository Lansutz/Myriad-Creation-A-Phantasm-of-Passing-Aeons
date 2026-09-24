
using System.Collections.Generic;
using System;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;



namespace MyriadCreation.Simulation.Economy
{
    [Serializable]
    public class TaxSystemDTO
    {
        public float agriculturalTax = 0.1f;
        public float headTax = 0.05f;
        public float tradeTax = 0.1f;
        public float miningTax = 0.15f;
        public float craftTax = 0.1f;
        public float livestockTax = 0.08f;
        public float luxuryTax = 0.3f;
        public float saltMonopolyTax = 0.5f;
        public float wartimeSpecialTax = 0f;
        public List<IntBoolEntry> taxExemptions = new List<IntBoolEntry>();

        public static TaxSystemDTO FromTaxSystem(TaxSystem t)
        {
            var dto = new TaxSystemDTO
            {
                agriculturalTax = t.agriculturalTax,
                headTax = t.headTax,
                tradeTax = t.tradeTax,
                miningTax = t.miningTax,
                craftTax = t.craftTax,
                livestockTax = t.livestockTax,
                luxuryTax = t.luxuryTax,
                saltMonopolyTax = t.saltMonopolyTax,
                wartimeSpecialTax = t.wartimeSpecialTax
            };
            if (t.taxExemptions != null)
                foreach (var kv in t.taxExemptions) dto.taxExemptions.Add(new IntBoolEntry((int)kv.Key, kv.Value));
            return dto;
        }

        public TaxSystem ToTaxSystem()
        {
            var t = new TaxSystem
            {
                agriculturalTax = agriculturalTax,
                headTax = headTax,
                tradeTax = tradeTax,
                miningTax = miningTax,
                craftTax = craftTax,
                livestockTax = livestockTax,
                luxuryTax = luxuryTax,
                saltMonopolyTax = saltMonopolyTax,
                wartimeSpecialTax = wartimeSpecialTax
            };
            foreach (var e in taxExemptions) t.taxExemptions[(GameEnums.SocialClass)e.key] = e.value;
            return t;
        }
    }
}
