using System;
using System.Collections.Generic;
using UnityEngine;
using CivilizationEvolution.Core;
using CivilizationEvolution.Politics;

namespace CivilizationEvolution.Thought
{
    public class EcclesiasticalTitle
    {
        public string titleName;
        public int level;
 /// <summary>true=俗人任命（世俗君主任命——叙任权之争教义基础—— /// doctrine_clerical_appointment_temporal）；false=灵性任命（教会自任）</summary>        public bool temporalAppointment;
    }
}
