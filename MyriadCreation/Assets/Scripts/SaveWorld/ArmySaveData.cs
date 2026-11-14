using System;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.State;
using MyriadCreation.Simulation.Units;

namespace MyriadCreation.Infrastructure.Save
{
    /// <summary>
    /// 军队实体存档 DTO（SaveVersion 13 起）。
    ///
    /// `Army` 没标 `[Serializable]`，且 `unitCounts` 是字典、`transport` 是 `[NonSerialized]` 引用，
    /// JsonUtility 不能直接存，所以拍平成数组 + 嵌套队伍 DTO。
    /// 只存"稳定状态"：编制、位置、士气/组织/补给/训练、行军路径；战斗瞬时量
    /// （originalManpower / casualtyRate / pursuitBonus 等）读档后按新战斗重算。
    /// </summary>
    [Serializable]
    public class ArmySaveData
    {
        public int armyId;
        public string armyName = "";
        public int ownerRealmId = -1;
        public int commanderId = -1;
        public int currentTileIndex = -1;

        // unitCounts：unitTypeId → 人数
        public int[] unitTypeIds = new int[0];
        public float[] unitCounts = new float[0];

        public GameEnums.CombatState state;
        public float organization = 100f;
        public float morale = 100f;
        public float supply = 100f;
        public float training = 50f;
        /// <summary>斥候可达范围（D37）；旧档缺字段为 1，读档后次日重算。</summary>
        public int scoutRange = 1;
        public bool scoutDetached;
        public int scoutTileIndex = -1;

        public int[] movePath = new int[0];
        public int moveTargetTile = -1;
        public float moveProgress;

        public TransportUnitSaveData transport;

        public static ArmySaveData FromArmy(Army army)
        {
            if (army == null) return null;

            var dto = new ArmySaveData
            {
                armyId = army.armyId,
                armyName = army.armyName ?? "",
                ownerRealmId = army.ownerRealmId,
                commanderId = army.commanderId,
                currentTileIndex = army.currentTileIndex,
                state = army.state,
                organization = army.organization,
                morale = army.morale,
                supply = army.supply,
                training = army.training,
                scoutRange = army.scoutRange,
                scoutDetached = army.scoutDetached,
                scoutTileIndex = army.scoutTileIndex,
                movePath = army.movePath?.ToArray() ?? new int[0],
                moveTargetTile = army.moveTargetTile,
                moveProgress = army.moveProgress,
                transport = TransportUnitSaveData.FromTransport(army.transport)
            };

            if (army.unitCounts != null && army.unitCounts.Count > 0)
            {
                int n = army.unitCounts.Count;
                dto.unitTypeIds = new int[n];
                dto.unitCounts = new float[n];
                int i = 0;
                foreach (var pair in army.unitCounts)
                {
                    dto.unitTypeIds[i] = pair.Key;
                    dto.unitCounts[i] = pair.Value;
                    i++;
                }
            }

            return dto;
        }

        public Army ToArmy()
        {
            var army = new Army
            {
                armyId = armyId,
                armyName = armyName ?? "",
                ownerRealmId = ownerRealmId,
                commanderId = commanderId,
                currentTileIndex = currentTileIndex,
                state = state,
                organization = organization,
                morale = morale,
                supply = supply,
                training = training,
                scoutRange = scoutRange,
                scoutDetached = scoutDetached,
                scoutTileIndex = scoutTileIndex,
                movePath = movePath != null ? new System.Collections.Generic.List<int>(movePath) : new System.Collections.Generic.List<int>(),
                moveTargetTile = moveTargetTile,
                moveProgress = moveProgress,
                transport = transport != null
                    ? transport.ToTransport()
                    : new TransportUnit { type = TransportUnitType.Army }
            };

            if (unitTypeIds != null && unitCounts != null && unitTypeIds.Length == unitCounts.Length)
            {
                for (int i = 0; i < unitTypeIds.Length; i++)
                    army.unitCounts[unitTypeIds[i]] = unitCounts[i];
            }

            return army;
        }
    }
}
