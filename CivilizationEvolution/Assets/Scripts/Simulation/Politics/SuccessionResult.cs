namespace CivilizationEvolution.Simulation.Politics
{
        public class SuccessionResult
        {
            public bool triggered;      // 是否有统治者死亡需要处理
            public bool succeeded;      // 是否完成扶正
            public bool disputed;       // 争议（绝嗣/幼主）
            public int newRulerId = -1;
            public int deadRulerId = -1; // 死亡统治者（绰号/谥号评估用）
            public string reason = "";
        }
}
