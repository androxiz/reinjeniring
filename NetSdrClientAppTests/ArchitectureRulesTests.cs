using NetArchTest.Rules;
using Xunit;

namespace NetSdrClientAppTests
{
    public class ArchitectureRulesTests
    {
        [Fact]
        public void ClientApp_Should_Not_Depend_On_EchoServer()
        {
            // Проверяем, что клиент (NetSdrClientApp) не имеет прямых зависимостей от сервера (EchoTspServer)
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.NetSdrClient).Assembly)
                .ShouldNot()
                .HaveDependencyOn("EchoTspServer")
                .GetResult();

            Xunit.Assert.True(result.IsSuccessful, "NetSdrClientApp имеет недопустимую зависимость от EchoTspServer!");
        }

        [Fact]
        public void EchoServer_Should_Not_Depend_On_ClientApp()
        {
            // Проверяем, что сервер (EchoTspServer) не зависит от клиента (NetSdrClientApp)
            var result = Types
                .InAssembly(typeof(EchoTspServer.EchoServer).Assembly)
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp")
                .GetResult();

            Xunit.Assert.True(result.IsSuccessful, "EchoTspServer имеет недопустимую зависимость от NetSdrClientApp!");
        }
    }
}
