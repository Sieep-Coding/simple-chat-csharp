using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CSharpStream
{
    internal sealed class ChatServer
    {
        private const bool SafeMode = true;
        private readonly ConcurrentDictionary<string, TcpClient> _clients = new();

        private static string SafeRemoteAddress(TcpClient client) =>
            SafeMode ? "[REDACTED]" : client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

        public async Task Listen(int port, CancellationToken token)
        {
            var listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Listening on port {port}...");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (listener.Pending())
                    {
                        var client = await listener.AcceptTcpClientAsync(token);
                        var address = SafeRemoteAddress(client);
                        Console.WriteLine($"Client {address} connected");
                        _clients[address] = client;

                        _ = HandleClientAsync(client, address, token);
                    }
                    else
                    {
                        await Task.Delay(100, token); // avoid busy-waiting
                    }
                }
            }
            finally
            {
                listener.Stop();
                foreach (var kvp in _clients)
                {
                    kvp.Value.Close();
                }
                _clients.Clear();
                Console.WriteLine("Server stopped.");
            }
        }

        private async Task HandleClientAsync(TcpClient client, string address, CancellationToken token)
        {
            try
            {
                await using var stream = client.GetStream();
                var buffer = new byte[512];

                while (!token.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0) break; // client disconnected

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received from {address}: {message}");
                    await BroadcastMessageAsync(message, address, token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client {address} error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Client {address} disconnected");
                _clients.TryRemove(address, out _);
                client.Close();
            }
        }

        private async Task BroadcastMessageAsync(string message, string senderAddress, CancellationToken token)
        {
            var bytes = Encoding.UTF8.GetBytes(message);

            foreach (var (address, client) in _clients)
            {
                if (address == senderAddress) continue;

                try
                {
                    var stream = client.GetStream();
                    await stream.WriteAsync(bytes, token);
                }
                catch
                {
                    Console.WriteLine($"Could not send to {address}");
                    _clients.TryRemove(address, out _);
                    client.Close();
                }
            }
        }
    }
}
