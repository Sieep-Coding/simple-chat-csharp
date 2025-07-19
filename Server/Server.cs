using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpStream.Server
{
    public class Server(int port, CancellationToken cancellationToken)
    {
        private readonly TcpListener _listener = new(IPAddress.Any, port);
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new();

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("Server started. Listening for connections...");

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    var clientKey = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

                    if (!_clients.TryAdd(clientKey, client)) continue;
                    Console.WriteLine($"Client connected: {clientKey}");
                    _ = HandleClientAsync(client, clientKey);
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Server stopped.");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
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
                while (!cancellationToken.IsCancellationRequested)
                {
                    var byteCount = await stream.ReadAsync(buffer, cancellationToken);
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
        
        /// <summary>
        /// main loop to broadcast message to all clients.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="excludeClientKey"></param>
        private async Task BroadcastMessageAsync(string message, string excludeClientKey)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (var (key, client) in _clients)
            {
                if (key == excludeClientKey) continue;

                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(messageBytes, cancellationToken);
                }
                catch
                {
                    RemoveClient(key);
                    client.Close();
                    Console.WriteLine($"Removed client {key} due to send failure.");
                }
            }
        }

        private void RemoveClient(string clientKey)
        {
            _clients.TryRemove(clientKey, out _);
        }
    }
}
