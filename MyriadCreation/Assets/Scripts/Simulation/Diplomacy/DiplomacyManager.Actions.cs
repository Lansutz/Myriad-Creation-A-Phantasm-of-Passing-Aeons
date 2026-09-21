using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Events;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.Simulation.WorldState;






namespace CivilizationEvolution.Simulation.Diplomacy
{
 /// DiplomacyManager.Actions —— 外交行动（使馆/断交/礼物/侮辱/禁运/军事通行权）（partial class，与 DiplomacySystem.cs 共享字段）
    public partial class DiplomacyManager
    {

 // ===== 外交动作 =====
 /// <summary>派遣使节/建立外交关系</summary>
        public bool EstablishEmbassy(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;
            rel.hasDiplomaticRelations = true;
            ModifyRelation(realmA, realmB, 5f, "建立外交关系");
            return true;
        }


 /// <summary>断绝外交关系</summary>
        public bool SeverRelations(int realmA, int realmB)
        {
            var rel = GetRelation(realmA, realmB);
            if (rel == null) return false;
            rel.hasDiplomaticRelations = false;
            rel.activeAlliances.Clear();
            ModifyRelation(realmA, realmB, -30f, "断绝外交关系");
            ModifyTrust(realmA, realmB, -25f);
            return true;
        }


 /// <summary>赠送礼物</summary>
        public bool SendGift(int fromId, int toId, float amount)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null || !_realms.ContainsKey(fromId)) return false;

            if (_realms[fromId].treasury < amount) return false;
            _realms[fromId].treasury -= amount;

            float relationGain = Mathf.Min(20f, amount / 100f);
            ModifyRelation(fromId, toId, relationGain, $"赠送礼物 {amount}");
            ModifyTrust(fromId, toId, relationGain * 0.5f);
            return true;
        }


 /// <summary>外交侮辱</summary>
        public bool DiplomaticInsult(int fromId, int toId, string insult)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null) return false;
            ModifyRelation(fromId, toId, -15f, $"外交侮辱：{insult}");
            ModifyTrust(fromId, toId, -10f);
            ModifyThreat(toId, fromId, 10f);
            return true;
        }


 /// <summary>贸易禁运</summary>
        public bool ImposeEmbargo(int fromId, int toId)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null) return false;
            rel.hasTradeEmbargo = true;
            ModifyRelation(fromId, toId, -20f, "贸易禁运");
            ModifyThreat(toId, fromId, 15f);
            return true;
        }


 /// <summary>解除禁运</summary>
        public bool LiftEmbargo(int fromId, int toId)
        {
            var rel = GetRelation(fromId, toId);
            if (rel == null) return false;
            rel.hasTradeEmbargo = false;
            ModifyRelation(fromId, toId, 10f, "解除贸易禁运");
            return true;
        }


        public bool HasMilitaryAccess(int fromRealm, int throughRealm)
        {
            var rel = GetRelation(fromRealm, throughRealm);
            if (rel == null) return false;
 // 军事通行权：全面同盟/阵营盟约包含通行权，或单独的通行权协议（由通行管制系统处理）
            return rel.activeAlliances.Exists(a => a.isActive && a.militaryAccess);
        }

    }
}
