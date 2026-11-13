using System;
using System.Collections.Generic;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Units;

namespace MyriadCreation.Simulation.State
{
    /// <summary>
    /// 海军特殊实体：不是陆军 Army，也不是普通 TransportUnit。
    /// 以舰船/舰队为基本单位，拥有自己的指挥、士气、补给与驻港逻辑。
    /// </summary>
    [Serializable]
    public class Navy
    {
        public int navyId;
        public string navyName = string.Empty;
        public int ownerRealmId;
        public int commanderId = -1;

        /// <summary>驻港/母港所在 tile；-1 表示海上编队。</summary>
        public int homePortTile = -1;

        /// <summary>当前所在 tile。</summary>
        public int currentTileIndex = -1;

        /// <summary>舰船 id → 数量；id 对应 EquipmentDef 的 ship 条目。</summary>
        public Dictionary<int, float> shipCounts = new Dictionary<int, float>();

        public GameEnums.CombatState state;
        public float organization = 100f;
        public float morale = 100f;
        public float supply = 100f;
        public float training = 50f;

        public List<int> movePath = new List<int>();
        public int moveTargetTile = -1;
        public float moveProgress;

        public float TotalShips
        {
            get
            {
                float n = 0f;
                foreach (var pair in shipCounts) n += pair.Value;
                return n;
            }
        }

        public bool IsAtSea => homePortTile < 0 || currentTileIndex != homePortTile;

        public void AddShips(int shipEquipmentId, float count)
        {
            if (shipEquipmentId <= 0 || count <= 0f) return;
            shipCounts.TryGetValue(shipEquipmentId, out var current);
            shipCounts[shipEquipmentId] = current + count;
        }

        public bool RemoveShips(int shipEquipmentId, float count)
        {
            if (!shipCounts.TryGetValue(shipEquipmentId, out var current) || current < count) return false;
            current -= count;
            if (current <= 1e-4f) shipCounts.Remove(shipEquipmentId);
            else shipCounts[shipEquipmentId] = current;
            return true;
        }

        public void DailyTick(float supplyPerShip)
        {
            supply = Math.Max(0f, supply - Math.Max(0f, supplyPerShip) * TotalShips);
            if (supply <= 0f)
            {
                morale = Math.Max(0f, morale - 5f);
                organization = Math.Max(0f, organization - 3f);
            }
            else if (state == GameEnums.CombatState.Idle)
            {
                training = Math.Min(100f, training + 0.05f);
            }
        }
    }
}
