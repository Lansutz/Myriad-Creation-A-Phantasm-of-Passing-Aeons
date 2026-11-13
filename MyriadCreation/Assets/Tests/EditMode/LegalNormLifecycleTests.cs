using NUnit.Framework;
using MyriadCreation.Simulation.Laws;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>法律生命周期含固化/根本化，不是只到生效。</summary>
    public class LegalNormLifecycleTests
    {
        [Test]
        public void StageOrder_IsExplicit()
        {
            Assert.Less((int)LegalNormStage.Known, (int)LegalNormStage.Pending);
            Assert.Less((int)LegalNormStage.Pending, (int)LegalNormStage.Active);
            Assert.Less((int)LegalNormStage.Active, (int)LegalNormStage.Consolidated);
            Assert.Less((int)LegalNormStage.Consolidated, (int)LegalNormStage.Fundamentalized);
            Assert.Less((int)LegalNormStage.Fundamentalized, (int)LegalNormStage.Repealed);
        }

        [Test]
        public void State_CanRecordFundamentalizedLaw()
        {
            var state = new RealmLawState();
            state.fundamentalizedLawIds.Add("fundamental_law_1");
            CollectionAssert.Contains(state.fundamentalizedLawIds, "fundamental_law_1");
            Assert.AreEqual(LegalNormStage.Active, new LawGroupState().stage);
        }
    }
}
