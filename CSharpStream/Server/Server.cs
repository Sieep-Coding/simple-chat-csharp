using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpStream.Server
{
    public class Server
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new();
        private readonly CancellationToken _cancellationToken;

        public Server(int port, CancellationToken cancellationToken)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _cancellationToken = cancellationToken;
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Server started. Listening for connections...");

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cancellationToken);
                    var clientKey = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

                    if (_clients.TryAdd(clientKey, client))
                    {
                        Console.WriteLine($"Client connected: {clientKey}");
                        _ = HandleClientAsync(client, clientKey); // fire-and-forget
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, shutting down server.
            }
            finally
            {
                _listener.Stop();
                Console.WriteLine("Server stopped.");
            }
        }

        private async Task HandleClientAsync(TcpClient client, string clientKey)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    int byteCount = await stream.ReadAsync(buffer, _cancellationToken);
                    if (byteCount == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Console.WriteLine($"Received from {clientKey}: {message.Trim()}");

                    await BroadcastMessageAsync(message, excludeClientKey: clientKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientKey}: {ex.Message}");
            }
            finally
            {
                RemoveClient(clientKey);
                client.Close();
                Console.WriteLine($"Client disconnected: {clientKey}");
            }
        }

        private async Task BroadcastMessageAsync(string message, string excludeClientKey)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (var kvp in _clients)
            {
                if (kvp.Key == excludeClientKey) continue;

                var client = kvp.Value;
                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(messageBytes, _cancellationToken);
                }
                catch
                {
                    // Failed to send message, remove client
                    RemoveClient(kvp.Key);
                    client.Close();
                    Console.WriteLine($"Removed client {kvp.Key} due to send failure.");
                }
            }
        }

        private void RemoveClient(string clientKey)
        {
            _clients.TryRemove(clientKey, out _);
        }
    }
}
