using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Economy;
using CivilizationEvolution.Politics;
using CivilizationEvolution.Race;
using CivilizationEvolution.Tech;
using CivilizationEvolution.Thought;

namespace CivilizationEvolution.Role
{
    public enum RulerType
    {
        Benevolent,    // 明君：威望高、恶名低
        Tyrant,        // 暴君：恶名高、威望低
        TyrantFool,    // 昏暴之君：威望恶名双高
        Mediocre       // 平庸之主：双低
    }
}
