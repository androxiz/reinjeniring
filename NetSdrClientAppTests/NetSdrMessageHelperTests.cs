using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void GetControlItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var codeBytes = msg.Skip(2).Take(2);
            var parametersBytes = msg.Skip(4);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);
            var actualCode = BitConverter.ToInt16(codeBytes.ToArray());

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(actualCode, Is.EqualTo((short)code));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetDataItemMessageTest()
        {
            //Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem2;
            int parametersLength = 7500;

            //Act
            byte[] msg = NetSdrMessageHelper.GetDataItemMessage(type, new byte[parametersLength]);

            var headerBytes = msg.Take(2);
            var parametersBytes = msg.Skip(2);

            var num = BitConverter.ToUInt16(headerBytes.ToArray());
            var actualType = (NetSdrMessageHelper.MsgTypes)(num >> 13);
            var actualLength = num - ((int)actualType << 13);

            //Assert
            Assert.That(headerBytes.Count(), Is.EqualTo(2));
            Assert.That(msg.Length, Is.EqualTo(actualLength));
            Assert.That(type, Is.EqualTo(actualType));

            Assert.That(parametersBytes.Count(), Is.EqualTo(parametersLength));
        }

        [Test]
        public void GetSamples_ValidSampleSize_ReturnsCorrectValues()
        {
            // Arrange
            ushort sampleSize = 16; // 2 bytes
            byte[] body = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };

            // Act
            var samples = NetSdrMessageHelper.GetSamples(sampleSize, body).ToList();

            // Assert
            Assert.That(samples.Count, Is.EqualTo(3));
            Assert.That(samples[0], Is.EqualTo(BitConverter.ToInt32(new byte[] { 0x01, 0x02, 0x00, 0x00 })));
            Assert.That(samples[1], Is.EqualTo(BitConverter.ToInt32(new byte[] { 0x03, 0x04, 0x00, 0x00 })));
            Assert.That(samples[2], Is.EqualTo(BitConverter.ToInt32(new byte[] { 0x05, 0x06, 0x00, 0x00 })));
        }

        [Test]
        public void GetSamples_InvalidSampleSize_ThrowsException()
        {
            // Arrange
            ushort sampleSize = 40; // 5 bytes - invalid
            byte[] body = new byte[] { 1, 2, 3, 4, 5 };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => NetSdrMessageHelper.GetSamples(sampleSize, body).ToList());
        }

        [Test]
        public void GetControlItemMessage_ExceedMaxLength_ThrowsException()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            int parametersLength = 8192; // Exceeds max message length

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                NetSdrMessageHelper.GetControlItemMessage(type, code, new byte[parametersLength]));
        }

        [Test]
        public void TranslateHeader_DataItem0ZeroLength_SetsMaxDataItemLength()
        {
            var method = typeof(NetSdrClientApp.Messages.NetSdrMessageHelper)
                .GetMethod("TranslateHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var header = BitConverter.GetBytes((ushort)((int)NetSdrClientApp.Messages.NetSdrMessageHelper.MsgTypes.DataItem0 << 13));
            object[] parameters = new object[] { header, null, null };
            method.Invoke(null, parameters);
            // msgLength should be set to 8194
            int msgLength = (int)parameters[2];
            Assert.That(msgLength, Is.EqualTo(8194));
        }

        [Test]
        public void GetSamples_EmptyBody_ReturnsEmpty()
        {
            var samples = NetSdrClientApp.Messages.NetSdrMessageHelper.GetSamples(16, new byte[0]);
            Assert.That(samples.Count(), Is.EqualTo(0));
        }

    }
}