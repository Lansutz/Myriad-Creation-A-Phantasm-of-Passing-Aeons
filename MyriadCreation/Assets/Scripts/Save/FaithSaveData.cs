using System;

namespace MyriadCreation.Save.Contracts
{
    /// <summary>
    /// 信仰运行态存档 DTO（SaveVersion 15 起；A1/A2 迁移：纯字段进契约层）。
    ///
    /// 内容定义（faithName/virtues/sins 等）由 ReligionCatalog 在
    /// PrepareLoadedWorld → InitializeFaithSystems 重建，不入档；这里只存运行态：
    /// 热忱、教会财富/影响力、传教热忱/宽容度、大祭司、教阶/教职、圣地、追随者、地区归附、教际关系。
    ///
    /// JsonUtility 不支持 Dictionary，regionAdherence / faithRelations 拍平为并行数组；
    /// hierarchyTitles 同样拍平。映射器在 Simulation 侧（FaithSaveMapping）。
    /// </summary>
    [Serializable]
    public class FaithSaveData
    {
        public int faithId;

        // G1/G2/G3（SaveVersion 18）：faith 的归属与传承。
        public int religionId = -1;
        public int denominationId = -1;
        public int lineageFaithId = -1;

        public float fervor = 50f;
        public float churchWealth;
        public float churchInfluence;
        public float missionaryZeal = 0.5f;
        public float tolerance = 0.5f;
        public int highPriestCharacterId = -1;
        public int hierarchyLevel;

        /// <summary>运行时教义覆盖（合一/改宗写入）；空数组 = 沿用内容表。</summary>
        public string[] doctrineIds = new string[0];

        /// <summary>共融期限（妥协写入）；-1 = 无共融约定。</summary>
        public int communionUntilDay = -1;

        public int[] holySiteTileIndices = new int[0];
        public int[] followerCharacterIds = new int[0];

        // regionAdherence：regionId → 归附度；两个数组按下标一一对应。
        public int[] regionAdherenceRegionIds = new int[0];
        public float[] regionAdherenceValues = new float[0];

        // faithRelations：目标 faithId → 关系值；两个数组按下标一一对应。
        public int[] relationFaithIds = new int[0];
        public float[] relationValues = new float[0];

        // hierarchyTitles 拍平；三个数组按下标一一对应。
        public string[] hierarchyTitleNames = new string[0];
        public int[] hierarchyTitleLevels = new int[0];
        public bool[] hierarchyTitleTemporalAppointments = new bool[0];
    }
}
