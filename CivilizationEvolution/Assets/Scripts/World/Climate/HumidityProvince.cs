namespace CivilizationEvolution.Climate
{
        public enum HumidityProvince
        {
            SuperArid,      // 超干旱 PER > 16
            PerArid,        // 极干旱 PER 8-16
            Arid,           // 干旱 PER 4-8
            SemiArid,       // 半干旱 PER 2-4
            SubHumid,       // 半湿润 PER 1-2
            Humid,          // 湿润 PER 0.5-1
            PerHumid,       // 极湿润 PER 0.25-0.5
            SuperHumid      // 超湿润 PER < 0.25
        }
}
