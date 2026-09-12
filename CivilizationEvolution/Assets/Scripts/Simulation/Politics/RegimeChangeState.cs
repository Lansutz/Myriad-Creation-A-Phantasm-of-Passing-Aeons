using System.Collections.Generic;
using System;

namespace CivilizationEvolution.Politics
{
    [Serializable]
    public class RegimeChangeState
    {
        public int realmId;
        public StructuralTension tension = new StructuralTension();
        public ActiveJuncture activeJuncture;           // null=路径依赖期
        public int compositionEstablishedDay;           // 现政体确立日（路径依赖黏性）
        [UnityEngine.Range(0f, 100f)] public float institutionalInertia; // 制度黏性（存续越久越难撼动）
        public List<string> history = new List<string>();// 变迁摘要（调试/编年史）
        public bool IsWindowOpen => activeJuncture != null && !activeJuncture.resolved;
    }
}
