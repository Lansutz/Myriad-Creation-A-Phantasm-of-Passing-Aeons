using NUnit.Framework;
using MyriadCreation.Core.Contracts;
using MyriadCreation.Simulation.ActionGate;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>B3.1：职权解析（元首/摄政/无权）。</summary>
    public class RealmAuthorityResolverTests
    {
        private static RealmAuthorityResolver Build(int rulerId, int regentId)
        {
            return new RealmAuthorityResolver((int realmId, out int ruler, out int regent) =>
            {
                if (realmId != 5)
                {
                    ruler = -1;
                    regent = -1;
                    return false;
                }
                ruler = rulerId;
                regent = regentId;
                return true;
            });
        }

        [Test]
        public void Ruler_ResolvesToHeadOfState()
        {
            var resolver = Build(rulerId: 1, regentId: 9);
            Assert.IsTrue(resolver.TryResolve(1, 5, out AuthoritySource source));
            Assert.AreEqual(AuthoritySource.HeadOfState, source);
        }

        [Test]
        public void Regent_ResolvesToRegent()
        {
            var resolver = Build(rulerId: 1, regentId: 9);
            Assert.IsTrue(resolver.TryResolve(9, 5, out AuthoritySource source));
            Assert.AreEqual(AuthoritySource.Regent, source);
        }

        [Test]
        public void OtherCharacter_IsNotAuthorized()
        {
            var resolver = Build(rulerId: 1, regentId: 9);
            Assert.IsFalse(resolver.TryResolve(2, 5, out AuthoritySource source));
            Assert.AreEqual(AuthoritySource.None, source);
        }

        [Test]
        public void UnknownRealm_FailsClosed()
        {
            var resolver = Build(rulerId: 1, regentId: 9);
            Assert.IsFalse(resolver.TryResolve(1, 99, out AuthoritySource source));
            Assert.AreEqual(AuthoritySource.None, source);
        }
    }
}
