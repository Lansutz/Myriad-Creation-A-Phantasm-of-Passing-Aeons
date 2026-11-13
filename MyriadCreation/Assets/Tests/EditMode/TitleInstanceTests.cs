using NUnit.Framework;
using MyriadCreation.Simulation.Politics;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>TitleInstance：共持、在任、继位转移（沿革不删除旧记录）。</summary>
    public class TitleInstanceTests
    {
        [Test]
        public void TransferSenior_EndsOldAndAddsNew()
        {
            var instance = new TitleInstance("title_king");
            instance.AddHolding(new TitleHolding { characterId = 1, role = TitleHolderRole.Sole, share = 1f, sinceDay = 10 });

            TitleHolding transferred = instance.TransferSenior(2, 100);

            Assert.AreEqual(2, instance.holders.Count, "旧记录保留，新增一条");
            Assert.AreEqual(2, transferred.characterId);
            Assert.AreEqual(TitleHolderRole.Sole, transferred.role);
            Assert.IsFalse(instance.holders[0].IsActive(100), "旧持有在转移日结束");
            Assert.AreEqual(2, instance.GetSeniorHolder(100).characterId, "新君为在任 senior");
        }

        [Test]
        public void CoRuler_IsNotSenior()
        {
            var instance = new TitleInstance("title_king");
            instance.AddHolding(new TitleHolding { characterId = 1, role = TitleHolderRole.CoRuler, share = 0.5f, sinceDay = 1 });
            Assert.IsNull(instance.GetSeniorHolder(5), "只有 CoRuler 时没有 senior");
            instance.AddHolding(new TitleHolding { characterId = 2, role = TitleHolderRole.Senior, share = 0.5f, sinceDay = 1 });
            Assert.AreEqual(2, instance.GetSeniorHolder(5).characterId);
        }
    }
}
