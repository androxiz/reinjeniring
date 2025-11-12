using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class TcpClientWrapperTests
    {
        private TcpClientWrapper _client;

        [SetUp]
        public void Setup()
        {
            _client = new TcpClientWrapper("127.0.0.1", 0);
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        [Test]
        public void Connect_WhenFails_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _client.Connect());
        }

        [Test]
        public void Disconnect_WhenNotConnected_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _client.Disconnect());
        }

        [Test]
        public async Task Disconnect_WhenConnected_ClosesResources()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(IPAddress.Loopback, port);
            var serverClient = await listener.AcceptTcpClientAsync();
            await connectTask;

            var stream = tcpClient.GetStream();

            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_client, tcpClient);
            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_client, stream);
            typeof(TcpClientWrapper).GetField("_cts", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_client, new CancellationTokenSource());

            Assert.That(_client.Connected, Is.True);

            Assert.DoesNotThrow(() => _client.Disconnect());

            var tcpAfter = typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_client);
            var streamAfter = typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_client);
            Assert.That(tcpAfter, Is.Null);
            Assert.That(streamAfter, Is.Null);

            listener.Stop();
            serverClient.Close();
        }

        [Test]
        public async Task Dispose_Calls_Disconnect_And_Disposes_Resources()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var tcpClient = new TcpClient();
            var connectTask = tcpClient.ConnectAsync(IPAddress.Loopback, port);
            var serverClient = await listener.AcceptTcpClientAsync();
            await connectTask;

            var stream = tcpClient.GetStream();

            typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_client, tcpClient);
            typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_client, stream);
            typeof(TcpClientWrapper).GetField("_cts", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_client, new CancellationTokenSource());

            Assert.DoesNotThrow(() => _client.Dispose());

            var tcpAfter = typeof(TcpClientWrapper).GetField("_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_client);
            var streamAfter = typeof(TcpClientWrapper).GetField("_stream", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_client);
            var ctsAfter = typeof(TcpClientWrapper).GetField("_cts", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_client);
          
            Assert.That(tcpAfter, Is.Null, "TcpClient should be null after Dispose()");
            Assert.That(streamAfter, Is.Null, "Stream should be null after Dispose()");
            Assert.That(ctsAfter, Is.Null, "CTS should be null after Dispose()");

            listener.Stop();
            serverClient.Close();
        }

        [Test]
        public void SendMessageAsync_NotConnected_Throws()
        {
            var data = new byte[] { 1, 2, 3 };
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.SendMessageAsync(data));
            Assert.That(ex.Message, Does.Contain("Not connected"));
        }

        [Test]
        public void StartListeningAsync_WhenNotConnected_Throws()
        {
            var method = typeof(TcpClientWrapper).GetMethod("StartListeningAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(method, Is.Not.Null);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await (Task)method!.Invoke(_client, null)!);
        }
    }
}
