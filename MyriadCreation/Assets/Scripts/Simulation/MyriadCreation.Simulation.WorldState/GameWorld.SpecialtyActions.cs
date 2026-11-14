using MyriadCreation.Simulation.State;

namespace MyriadCreation.Simulation.WorldState
{
    public partial class GameWorld
    {
        /// <summary>
        /// P11.1：实地踏勘——已解锁 field_survey 的随员可揭示目标地块周边知识。
        /// 返回是否真的新增了知识（重复踏勘返回 false，不刷状态）。
        /// </summary>
        public bool TrySurveyTile(int realmId, int surveyorId, int tileIndex)
        {
            if (tiles == null || Knowledge == null) return false;
            if (tileIndex < 0 || tileIndex >= tiles.Length) return false;
            CharacterData surveyor = Characters?.GetCharacter(surveyorId);
            if (surveyor == null || !surveyor.HasPerk("field_survey")) return false;

            bool knownBefore = Knowledge.IsKnown(realmId, tileIndex);
            Knowledge.RevealRadius(realmId, tileIndex, 1, mapWidth, mapHeight);
            return !knownBefore && Knowledge.IsKnown(realmId, tileIndex);
        }
    }
}
