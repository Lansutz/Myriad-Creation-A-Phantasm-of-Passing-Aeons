using System;

namespace MyriadCreation.Content.Definitions
{
    /// <summary>
    /// 具体物品/装备定义：由某条革新解锁；革新本身不是物品。
    /// </summary>
    [Serializable]
    public class EquipmentDef
    {
        public int equipmentId;
        public string equipmentName = string.Empty;
        public EquipmentCategory category;
        public int tier = 1;
        public int requiredInnovation;

        /// <summary>多革新约束：All/Any/Leaf；非空时与 requiredInnovation 同时校验。</summary>
        public InnovationPrerequisiteNode requiredInnovationTree;
        public string description = string.Empty;

        // 船类专用；非船物品忽略。
        public ShipHullForm hullForm;
        public ShipPropulsion propulsion;
        public RamType ramType;
        public BatteryLayout batteryLayout;
        public bool boardingPlatform;

        // 武器类专用；非武器忽略。
        public WeaponFamily weaponFamily;

        // 成形工艺：铸造/锻造/热处理等；processDetail 写具体细分（范铸/失蜡/自由锻/折叠锻...）。
        public CraftProcess craftProcess;
        public string processDetail = string.Empty;
    }
}
