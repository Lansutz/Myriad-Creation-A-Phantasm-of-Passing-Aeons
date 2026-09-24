using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.AI;
using MyriadCreation.Simulation.Characters;
using MyriadCreation.Simulation.Culture;
using MyriadCreation.Simulation.Diplomacy;
using MyriadCreation.Simulation.Disaster;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Innovation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.Religion;
using MyriadCreation.Simulation.Settlement;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.Warfare;
using MyriadCreation.Simulation.WorldState;
using MyriadCreation.World.Biome;
using MyriadCreation.World.Climate;
using MyriadCreation.World.Generation;
using MyriadCreation.World.Hydrology;
using MyriadCreation.World.Settlement;
using MyriadCreation.World.Terrain;

















namespace MyriadCreation.Simulation.Events
{
 /// <summary>游戏事件监听器接口</summary>
    public interface IGameEventListener
    {
        void OnGameEvent(GameEvent evt);
    }
}
