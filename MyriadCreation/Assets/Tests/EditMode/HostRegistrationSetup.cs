using NUnit.Framework;
using MyriadCreation.Simulation.Hosts;

namespace MyriadCreation.Tests.EditMode
{
    /// <summary>EditMode 下不会触发 RuntimeInitialize；测试启动时注册 Host 工厂。</summary>
    [SetUpFixture]
    public class HostRegistrationSetup
    {
        [OneTimeSetUp]
        public void RegisterHosts()
        {
            HostRegistration.EnsureRegistered();
        }
    }
}
