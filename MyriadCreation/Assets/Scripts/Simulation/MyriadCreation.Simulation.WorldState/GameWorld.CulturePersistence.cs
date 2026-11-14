using System.Collections.Generic;
using MyriadCreation.Save.Contracts;
using MyriadCreation.Simulation.Culture;

namespace MyriadCreation.Simulation.WorldState
{
    public partial class GameWorld
    {
        /// <summary>
        /// 文化入档/还原收口：存档层只拿 DTO，不直接触碰 CultureData（A2 引用收窄）。
        /// </summary>
        public List<CultureSaveData> CaptureCultures()
        {
            var result = new List<CultureSaveData>(cultures != null ? cultures.Count : 0);
            if (cultures == null) return result;
            foreach (KeyValuePair<int, CultureData> pair in cultures)
            {
                CultureSaveData dto = CultureSaveMapping.ToSave(pair.Value);
                if (dto != null) result.Add(dto);
            }
            return result;
        }

        public void RestoreCultures(List<CultureSaveData> saved)
        {
            cultures.Clear();
            if (saved == null) return;
            foreach (CultureSaveData dto in saved)
            {
                CultureData culture = CultureSaveMapping.ToRuntime(dto);
                if (culture != null) cultures[culture.cultureId] = culture;
            }
        }
    }
}
