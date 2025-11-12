using NUnit.Framework;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EchoTspServer;
using Moq;
using System.Linq;

namespace EchoTspServerTests
{
    [TestFixture]
    public class EchoServerTests
    {
        [Test]
        public async Task EchoMessageAsync_ReturnsSameMessage()
        {
            // Arrange
            var server = new EchoServer(5000);
            byte[] message = Encoding.UTF8.GetBytes("Hello World");

            // Act
            var result = await EchoServer.EchoMessageAsync(message, CancellationToken.None);

            // Assert
            Assert.AreEqual(message, result);
        }

        [Test]
        public void Stop_CancelsServer()
        {
            // Arrange
            var server = new EchoServer(5000);
            Assert.That(server.IsCancellationRequested, Is.False);

            // Act
            server.Stop();

            // Assert
            Assert.That(server.IsCancellationRequested, Is.True);
        }

        [Test]
        public void UdpTimedSender_StartSending_ThrowsIfAlreadyRunning()
        {
            // Arrange
            var sender = new UdpTimedSender("127.0.0.1", 5000);
            sender.StartSending(100);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => sender.StartSending(100));
            Assert.AreEqual("Sender is already running.", ex.Message);

            sender.StopSending();
        }

        [Test]
        public void UdpTimedSender_StartStop_Dispose_DoesNotThrow()
        {
            // Arrange
            var sender = new UdpTimedSender("127.0.0.1", 5000);

            // Act & Assert
            Assert.DoesNotThrow(() => sender.StartSending(100));
            Assert.DoesNotThrow(() => sender.StopSending());
            Assert.DoesNotThrow(() => sender.Dispose());
        }

        [Test]
        public async Task UdpTimedSender_SendMessageCallback_IncrementsCounter()
        {
            // Arrange
            var sender = new UdpTimedSender("127.0.0.1", 5000);
            // Use reflection to access private field 'i'
            var iField = typeof(UdpTimedSender).GetField("i", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(iField, Is.Not.Null);

            // Act
            sender.StartSending(10);
            await Task.Delay(50); // wait a few ticks
            sender.StopSending();

            ushort iValue = (ushort)iField.GetValue(sender)!;

            // Assert
            Assert.That(iValue, Is.GreaterThan(0));
        }
    }
}
