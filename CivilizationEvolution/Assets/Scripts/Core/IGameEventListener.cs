using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.War;
using CivilizationEvolution.Diplomacy;
using CivilizationEvolution.Character;
using CivilizationEvolution.Thought;
using CivilizationEvolution.Disaster;
using CivilizationEvolution.Building;
using CivilizationEvolution.Tech;
using CivilizationEvolution.AI;

namespace CivilizationEvolution.Core
{
    /// <summary>游戏事件监听器接口</summary>
    public interface IGameEventListener
    {
        void OnGameEvent(GameEvent evt);
    }
}
