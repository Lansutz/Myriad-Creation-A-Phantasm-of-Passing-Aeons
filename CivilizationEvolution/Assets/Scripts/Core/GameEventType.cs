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
    public enum GameEventType
    {
        Famine,
        Rebellion,
        SeasonChange,
        WarDeclaration,
        PeaceTreaty,
        Plague,
        NaturalDisaster,
        EconomicCrisis
    }
}
