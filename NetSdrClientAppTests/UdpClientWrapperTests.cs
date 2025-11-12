using System;
using System.Net;
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

        [SetUp]
        public void Setup()
        {
            _udpClient = new UdpClientWrapper(0); // порт 0 = випадковий доступний
        }

        [TearDown]
        public void TearDown()
        {
            _udpClient.Dispose();
        }

        [Test]
        public void StopListening_WithoutStart_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _udpClient.StopListening());
        }


        [Test]
        public void Dispose_CallsStopListening_AndClosesResources()
        {
            Assert.DoesNotThrow(() => _udpClient.Dispose());
        }

        [Test]
        public void Equals_SameEndpoint_ReturnsTrue()
        {
            var wrapper1 = new UdpClientWrapper(12345);
            var wrapper2 = new UdpClientWrapper(12345);

            Assert.IsTrue(wrapper1.Equals(wrapper2));
        }

        [Test]
        public void Equals_DifferentEndpoint_ReturnsFalse()
        {
            var wrapper1 = new UdpClientWrapper(12345);
            var wrapper2 = new UdpClientWrapper(54321);

            Assert.IsFalse(wrapper1.Equals(wrapper2));
        }

        [Test]
        public void GetHashCode_SameEndpoint_SameHash()
        {
            var wrapper1 = new UdpClientWrapper(12345);
            var wrapper2 = new UdpClientWrapper(12345);

            Assert.That(wrapper2.GetHashCode(), Is.EqualTo(wrapper1.GetHashCode()));
        }

        [Test]
        public void GetHashCode_DifferentEndpoint_DifferentHash()
        {
            var wrapper1 = new UdpClientWrapper(12345);
            var wrapper2 = new UdpClientWrapper(54321);

            Assert.That(wrapper2.GetHashCode(), Is.Not.EqualTo(wrapper1.GetHashCode()));
        }
    }
}
