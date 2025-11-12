using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class UdpClientWrapperTests
    {
        private UdpClientWrapper _udpClient;
        private int _port = 55000;

        [SetUp]
        public void Setup()
        {
            _udpClient = new UdpClientWrapper(_port);
        }

        [TearDown]
        public void TearDown()
        {
            _udpClient.Dispose();
        }

        [Test]
        public async Task StartListeningAsync_ReceivesMessage_InvokesEvent()
        {
            // Arrange
            byte[] testData = Encoding.UTF8.GetBytes("hello");
            byte[]? receivedData = null;
            var evt = new ManualResetEventSlim(false);

            _udpClient.MessageReceived += (s, data) =>
            {
                receivedData = data;
                evt.Set();
            };

            // Act
            var listeningTask = _udpClient.StartListeningAsync();

            using (var sender = new UdpClient())
            {
                sender.Send(testData, testData.Length, "127.0.0.1", _port);
            }

            bool signaled = evt.Wait(1000);

            _udpClient.StopListening();
            await Task.Delay(50); // подождать завершение цикла
            _udpClient.Dispose();

            // Assert
            Assert.IsTrue(signaled, "MessageReceived event should be triggered");
            Assert.That(receivedData, Is.EqualTo(testData));
        }

        [Test]
        public void StopListening_CancelsListeningWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                _udpClient.StopListening();
            });
        }

        [Test]
        public void Dispose_CleansUpResourcesWithoutException()
        {
            Assert.DoesNotThrow(() =>
            {
                _udpClient.Dispose();
            });
        }

        [Test]
        public void Equals_SameEndpoint_ReturnsTrue()
        {
            var another = new UdpClientWrapper(_port);
            Assert.IsTrue(_udpClient.Equals(another));
        }

        [Test]
        public void Equals_DifferentEndpoint_ReturnsFalse()
        {
            var another = new UdpClientWrapper(_port + 1);
            Assert.IsFalse(_udpClient.Equals(another));
        }

        [Test]
        public void GetHashCode_ReturnsInt()
        {
            int hash = _udpClient.GetHashCode();
            Assert.That(hash, Is.TypeOf<int>());
        }
    }
}
