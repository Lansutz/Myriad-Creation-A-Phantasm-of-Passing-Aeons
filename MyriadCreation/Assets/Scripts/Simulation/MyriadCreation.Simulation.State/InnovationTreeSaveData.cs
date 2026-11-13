using System;

namespace MyriadCreation.Simulation.State
{
    /// <summary>
    /// 革新/科技树存档 DTO（SaveVersion 14）。
    ///
    /// `InnovationTree` 的 6 张私有字典（已获革新、研究点、当前研究、已发现、进度、资源累计产出）
    /// 以前完全不入档，读档后所有政权科技清零。这里把可恢复状态拍平：
    /// 革新定义本身来自内容包（ContentRegistry），不入档，读档时由构造函数重新加载。
    /// </summary>
    [Serializable]
    public class InnovationTreeSaveData
    {
        public RealmInnovationSaveData[] realms;
        public InnovationProgressSaveData[] progress;
        public StringFloatEntry[] resourceOutput;
    }

    /// <summary>单个政权的革新状态。</summary>
    [Serializable]
    public class RealmInnovationSaveData
    {
        public int realmId;
        public int[] innovations;
        public int[] discovered;
        public float researchPoints;
        public int currentResearch = -1;
    }

    /// <summary>单项革新的研究进度（含增益明细）。</summary>
    [Serializable]
    public class InnovationProgressSaveData
    {
        public int innovationId;
        public float progress;
        public float monthlyGain;
        public bool isAvailable;
        public string lockedReason;
        public string[] gainBreakdownKeys;
        public float[] gainBreakdownValues;
        public float cumulativeOutput;
        public float averageQuality;
        public int oldMethodPracticeCount;
    }

    /// <summary>字符串→浮点条目（JsonUtility 无字典，资源累计产出 key 形如 "realmId_goodsId"）。</summary>
    [Serializable]
    public class StringFloatEntry
    {
        public string key;
        public float value;
    }
}
