using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Core.Constants;
using CivilizationEvolution.Core.Data;
using CivilizationEvolution.Core.Dto;
using CivilizationEvolution.Core.Enums;
using CivilizationEvolution.Simulation.AI;
using CivilizationEvolution.Simulation.Characters;
using CivilizationEvolution.Simulation.Culture;
using CivilizationEvolution.Simulation.Diplomacy;
using CivilizationEvolution.Simulation.Disaster;
using CivilizationEvolution.Simulation.Economy;
using CivilizationEvolution.Simulation.Generation;
using CivilizationEvolution.Simulation.Innovation;
using CivilizationEvolution.Simulation.Modding;
using CivilizationEvolution.Simulation.Politics;
using CivilizationEvolution.Simulation.Population;
using CivilizationEvolution.Simulation.Religion;
using CivilizationEvolution.Simulation.Settlement;
using CivilizationEvolution.Simulation.Society;
using CivilizationEvolution.Simulation.Warfare;
using CivilizationEvolution.Simulation.WorldState;
using CivilizationEvolution.World.Biome;
using CivilizationEvolution.World.Climate;
using CivilizationEvolution.World.Generation;
using CivilizationEvolution.World.Hydrology;
using CivilizationEvolution.World.Settlement;
using CivilizationEvolution.World.Terrain;

















namespace CivilizationEvolution.Simulation.Events
{
 /// <summary>游戏事件监听器接口</summary>
    public interface IGameEventListener
    {
        void OnGameEvent(GameEvent evt);
    }
}
