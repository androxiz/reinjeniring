﻿using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    NetSdrClient _client;
    Mock<ITcpClient> _tcpMock;
    Mock<IUdpClient> _updMock;

    public NetSdrClientTests() { }

    [SetUp]
    public void Setup()
    {
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        });

        _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
        {
            _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        });

        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback<byte[]>((bytes) =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
        });

        _updMock = new Mock<IUdpClient>();

        _client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        //act
        await _client.ConnectAsync();

        //assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public async Task DisconnectWithNoConnectionTest()
    {
        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        _client.Disconect();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StartIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        //Arrange 
        await ConnectAsyncTest();

        //act
        await _client.StopIQAsync();

        //assert
        //No exception thrown
        _updMock.Verify(tcp => tcp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    [Test]
    public async Task SendTcpRequest_NoConnection_ReturnsNull()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
        var client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
        var result = await client.SendTcpRequest(new byte[] { 1, 2, 3 });
        Assert.IsNull(result);
    }

    [Test]
    public async Task SendTcpRequest_ReceivesResponse()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        var response = new byte[] { 9, 8, 7 };
        _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>())).Callback(() =>
        {
            _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, response);
        });
        var client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
        // Act
        var result = await client.SendTcpRequest(new byte[] { 1, 2, 3 });
        // Assert
        Assert.That(result, Is.EqualTo(response));
    }

    [Test]
    public void TcpClient_MessageReceived_UnsolicitedHandled()
    {
        // Arrange
        var client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
        var data = new byte[] { 1, 2, 3 };
        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var method = typeof(NetSdrClient).GetMethod("_tcpClient_MessageReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(client, new object[] { _tcpMock.Object, data });
        });
    }

    [Test]
    public async Task StartIQAsync_AlreadyStarted_DoesNotStartAgain()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        var client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
        await client.ConnectAsync();
        // Act
        await client.StartIQAsync();
        // Assert
        _updMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
    }

    [Test]
    public async Task StopIQAsync_NotStarted_DoesNotThrow()
    {
        // Arrange
        _tcpMock.Setup(tcp => tcp.Connected).Returns(true);
        var client = new NetSdrClient(_tcpMock.Object, _updMock.Object);
        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await client.StopIQAsync());
    }

}
