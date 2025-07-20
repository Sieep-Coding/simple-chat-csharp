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
            Console.WriteLine("Server started");
            Logger.Info("Server", "Server started. Listening for connections...");

            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(_cancellationToken);
                    var clientKey = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();

                    if (!_clients.TryAdd(clientKey, client)) continue;
                    Logger.Debug("Server", $"Client connected: {clientKey}");
                    _ = HandleClientAsync(client, clientKey);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Error("Server", "Server stopped.");
            }
            finally
            {
                _listener.Stop();
                Logger.Info("Server", "Server stopped.");
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
                        Logger.Error("Server", $"Client {clientKey} disconnected.");
                        break;
                    }

                    var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Message? message;

                    try
                    {
                        message = JsonSerializer.Deserialize<Message>(json);
                    }
                    catch (JsonException jsonException)
                    {
                        Logger.Error("Server",$"Invalid message format from {clientKey}");
                        Logger.Debug("Server", jsonException.Message + "\n" + jsonException.StackTrace);
                        continue;
                    }

                    if (message is null)
                    {
                        Console.WriteLine($"Null message received from {clientKey}");
                        Logger.Error("Server",$"Null message received from {clientKey}");
                        continue;
                    }

                    Logger.Info("Server", $"[{message.Timestamp:T}] {message.Sender}: {message.Content}");

                    await BroadcastMessageAsync(json, excludeClientKey: clientKey);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error with client {clientKey}: {ex.Message}");
                Logger.Error("Server", $"Error with client {clientKey}: {ex.Message}");
                Logger.Debug("Server", $"Error with client {clientKey}: {ex.StackTrace}");
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
                catch (Exception ex)
                {
                    RemoveClient(key);
                    client.Close();
                    Console.WriteLine($"Removed client {key} due to send failure.");
                    Logger.Info("Server", $"Client {key} due to send failure.");
                    Logger.Debug("Server", $"Error: {ex.Message} \n"  + ex.StackTrace);
                }
            }
        }

        private void RemoveClient(string clientKey)
        {
            try
            {
                _clients.TryRemove(clientKey, out _);
                Logger.Info("Server", $"Client {clientKey} removed.");
            }
            catch (Exception ex)
            {
                Logger.Error("Server", $"Error removing client {clientKey}.");
                Logger.Debug("Server", $"Error: {ex.Message} \n"  + ex.StackTrace);
                throw;
            }
        }
    }
}
