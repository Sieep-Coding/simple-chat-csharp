using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSharpStream.Models;

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

                    if (!_clients.TryAdd(clientKey, client)) continue;
                    Console.WriteLine($"Client connected: {clientKey}");
                    _ = HandleClientAsync(client, clientKey);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Server stopped.");
            }
            finally
            {
                _listener.Stop();
                Console.WriteLine("Server stopped.");
            }
        }

        private async Task HandleClientAsync(TcpClient client, string clientKey)
        {
            var buffer = new byte[4096];
            var stream = client.GetStream();

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, _cancellationToken);
                    if (bytesRead == 0)
                    {
                        // Client disconnected
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Message? message;

                    try
                    {
                        message = JsonSerializer.Deserialize<Message>(json);
                    }
                    catch (JsonException)
                    {
                        Console.WriteLine($"Invalid message format from {clientKey}");
                        continue;
                    }

                    if (message is null)
                    {
                        Console.WriteLine($"Null message received from {clientKey}");
                        continue;
                    }

                    Console.WriteLine($"[{message.Timestamp:T}] {message.Sender}: {message.Content}");

                    await BroadcastMessageAsync(json, excludeClientKey: clientKey);
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

        private async Task BroadcastMessageAsync(string jsonMessage, string excludeClientKey)
        {
            var messageBytes = Encoding.UTF8.GetBytes(jsonMessage);

            foreach (var (key, client) in _clients)
            {
                if (key == excludeClientKey) continue;

                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(messageBytes, _cancellationToken);
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
