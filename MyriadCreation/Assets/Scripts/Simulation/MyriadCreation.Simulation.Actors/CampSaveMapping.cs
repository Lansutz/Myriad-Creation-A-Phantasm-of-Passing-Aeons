using System.Collections.Generic;
using MyriadCreation.Save.Contracts;
using MyriadCreation.World.Settlement;

namespace MyriadCreation.Simulation.Actors
{
    /// <summary>
    /// `CampData` 与 `HabitationSiteSaveData` 的映射（**状态所有者侧**）。
    ///
    /// 为什么单独一个文件：营地属 Simulation.Actors，而 World 不得引用 Simulation；
    /// 栖居点本体（site mode / 设施）的映射在 `World.Settlement.HabitationSiteSaveMapping`。
    /// 写成扩展方法，调用点保持 `saveData.CaptureCampData(camp)` 不变。
    /// </summary>
    public static class CampSaveMapping
    {
        public static void CaptureCampData(this HabitationSiteSaveData data, CampData camp)
        {
            // 营地本身是 HabitationSite 的子类型，先走栖居点侧的模式/设施映射。
            data.CaptureModeData(camp);
            data.camp_id = camp.campId;
            data.camp_type = camp.type.ToString();
            data.camp_tile_index = camp.tileIndex;
            data.owner_realm_id = camp.ownerRealmId;
            data.owner_actor_id = camp.ownerActorId;
            data.established_day = camp.establishedDay;
            data.camp_defense = camp.defense;
            data.camp_supplies = camp.supplies;
            data.camp_morale = camp.morale;
            data.camp_permanence = camp.permanence;
            if (camp.attributes == null)
            {
                data.camp_attribute_keys = null;
                data.camp_attribute_values = null;
                return;
            }

            var attributes = new List<KeyValuePair<string, float>>(camp.attributes);
            data.camp_attribute_keys = new string[attributes.Count];
            data.camp_attribute_values = new float[attributes.Count];
            for (int i = 0; i < attributes.Count; i++)
            {
                data.camp_attribute_keys[i] = attributes[i].Key;
                data.camp_attribute_values[i] = attributes[i].Value;
            }
        }
    }
}
