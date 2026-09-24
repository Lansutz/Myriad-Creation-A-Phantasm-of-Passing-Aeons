using System;
using System.Collections.Generic;
using UnityEngine;
using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Generation;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Population;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;




namespace MyriadCreation.Simulation.State
{
    public class EcclesiasticalTitle
    {
        public string titleName;
        public int level;
 /// <summary>true=俗人任命（世俗君主任命——叙任权之争教义基础——
 /// doctrine_clerical_appointment_temporal）；false=灵性任命（教会自任）</summary>
        public bool temporalAppointment;
    }
}
