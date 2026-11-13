using System;
using System.Collections.Generic;
using MyriadCreation.Simulation.Systems;

namespace MyriadCreation.Simulation.State
{
    /// <summary>
    /// 行政区划存档 DTO（`RealmData.adminDivisions`）。
    ///
    /// `AdminDivision` 里 `tiles` 是 HashSet、`childIds` 是 List，JsonUtility 不能直接序列化，
    /// 因此转成数组保存；树关系（parentDivisionId/childIds）与治理者绑定（holderCharacterId）
    /// 都是结构事实，必须入档，否则读档后区划树、官员归属、法令下发路径全部丢失。
    /// </summary>
    [Serializable]
    public class AdminDivisionSaveData
    {
        public int divisionId;

        public int realmId;

        public int level;

        public string name;

        public int parentDivisionId = -1;

        public int[] childIds;

        public int[] tiles;

        public string titleId;

        public int holderCharacterId = -1;

        public static AdminDivisionSaveData From(AdminDivision division)
        {
            if (division == null) throw new ArgumentNullException(nameof(division));
            return new AdminDivisionSaveData
            {
                divisionId = division.divisionId,
                realmId = division.realmId,
                level = division.level,
                name = division.name,
                parentDivisionId = division.parentDivisionId,
                childIds = division.childIds?.ToArray() ?? new int[0],
                tiles = division.tiles != null ? new List<int>(division.tiles).ToArray() : new int[0],
                titleId = division.titleId,
                holderCharacterId = division.holderCharacterId
            };
        }

        public AdminDivision ToDivision()
        {
            var division = new AdminDivision
            {
                divisionId = divisionId,
                realmId = realmId,
                level = level,
                name = name ?? string.Empty,
                parentDivisionId = parentDivisionId,
                titleId = titleId ?? string.Empty,
                holderCharacterId = holderCharacterId
            };

            if (childIds != null)
                division.childIds.AddRange(childIds);
            if (tiles != null)
                foreach (var tile in tiles) division.tiles.Add(tile);

            return division;
        }
    }
}
