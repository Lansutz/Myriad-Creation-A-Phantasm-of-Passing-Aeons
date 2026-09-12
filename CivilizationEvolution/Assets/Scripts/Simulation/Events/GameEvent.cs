using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Map;
using CivilizationEvolution.Climate;
using CivilizationEvolution.Race;
using CivilizationEvolution.Culture;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Core
{
    public struct GameEvent
    {
        public GameEventType eventType;
        public int tileIndex;
        public int realmId;
        public float severity;
        public string description;
    }
}
