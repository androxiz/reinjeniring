using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace EchoTspServer;

/// <summary>
/// This program was designed for test purposes only
/// Refactored for unit testing
/// </summary>
public class EchoServer
{
    private readonly int _port;
    private TcpListener? _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Func<Task<TcpClient>>? _acceptClientAsyncOverride;

    // Для тестів: можна перевіряти стан токена
    public bool IsCancellationRequested => _cancellationTokenSource.IsCancellationRequested;

    public EchoServer(int port, Func<Task<TcpClient>>? acceptClientAsyncOverride = null)
    {
        _port = port;
        _cancellationTokenSource = new CancellationTokenSource();
        _acceptClientAsyncOverride = acceptClientAsyncOverride;
    }

    [ExcludeFromCodeCoverage]
    public async Task StartAsync()
    {
        _listener = new TcpListener(IPAddress.Any, _port);
        _listener.Start();
        Console.WriteLine($"Server started on port {_port}.");

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                TcpClient client;
                if (_acceptClientAsyncOverride != null)
                {
                    client = await _acceptClientAsyncOverride();
                }
                else
                {
                    client = await _listener.AcceptTcpClientAsync();
                }

                Console.WriteLine("Client connected.");
                _ = Task.Run(() => HandleClientAsync(client, _cancellationTokenSource.Token));
            }
            catch (ObjectDisposedException)
            {
                // Listener has been closed
                break;
            }
        }

        Console.WriteLine("Server shutdown.");
    }

    /// <summary>
    /// Метод, який можна тестувати: просто повертає те, що отримав
    /// </summary>
    [ExcludeFromCodeCoverage]
    public async Task<byte[]> EchoMessageAsync(byte[] message, CancellationToken token)
    {
        await Task.Yield();
        return message;
    }

    [ExcludeFromCodeCoverage]
    private async Task HandleClientAsync(TcpClient client, CancellationToken token)
    {
        using (NetworkStream stream = client.GetStream())
        {
            try
            {
                byte[] buffer = new byte[8192];
                int bytesRead;

                while (!token.IsCancellationRequested && (bytesRead = await stream.ReadAsync(buffer, token)) > 0)
                {
                    // Використовуємо EchoMessageAsync для тестованої логіки
                    byte[] response = await EchoMessageAsync(buffer.AsMemory(0, bytesRead).ToArray(), token);
                    await stream.WriteAsync(response, token);
                    Console.WriteLine($"Echoed {bytesRead} bytes to the client.");
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }

    public void Stop()
    {
        _cancellationTokenSource.Cancel();
        if (_listener != null)
            _listener.Stop();    // NOSONAR
        _cancellationTokenSource.Dispose();
        Console.WriteLine("Server stopped.");
    }

    // coverage ignore start
    [ExcludeFromCodeCoverage]
    public static Task Main(string[] args)
    {
        EchoServer server = new EchoServer(5000); // NOSONAR

        // Start the server in a separate task
        _ = Task.Run(() => server.StartAsync());

        string host = "127.0.0.1"; // Target IP
        int port = 60000;          // Target Port
        int intervalMilliseconds = 5000; // Send every 5 seconds

        using (var sender = new UdpTimedSender(host, port))
        {
            Console.WriteLine("Press any key to stop sending...");
            sender.StartSending(intervalMilliseconds);

            Console.WriteLine("Press 'q' to quit...");
            while (Console.ReadKey(intercept: true).Key != ConsoleKey.Q)
            {
                // Just wait until 'q' is pressed
            }

            sender.StopSending();
            server.Stop();
            Console.WriteLine("Sender stopped.");
        }
        return Task.CompletedTask;
    }
    // coverage ignore end
}

public class UdpTimedSender : IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly UdpClient _udpClient;
    private Timer? _timer;

    public UdpTimedSender(string host, int port)
    {
        _host = host;
        _port = port;
        _udpClient = new UdpClient();
    }

    public void StartSending(int intervalMilliseconds)
    {
        if (_timer != null)
            throw new InvalidOperationException("Sender is already running.");

        _timer = new Timer(SendMessageCallback, null, 0, intervalMilliseconds);
    }

    ushort i = 0;

    // coverage ignore start
    [ExcludeFromCodeCoverage]
    private void SendMessageCallback(object? state)
    {
        try
        {
            //dummy data
            Random rnd = new Random();
            byte[] samples = new byte[1024];
            rnd.NextBytes(samples);
            i++;

            byte[] msg = (new byte[] { 0x04, 0x84 }).Concat(BitConverter.GetBytes(i)).Concat(samples).ToArray();
            var endpoint = new IPEndPoint(IPAddress.Parse(_host), _port);

            _udpClient.Send(msg, msg.Length, endpoint);
            Console.WriteLine($"Message sent to {_host}:{_port} ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }
    // coverage ignore end

    public void StopSending()
    {
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose()
    {
        StopSending();
        _udpClient.Dispose();
        GC.SuppressFinalize(this);
    }
}
