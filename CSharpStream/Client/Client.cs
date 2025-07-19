using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpStream.Models;

namespace CSharpStream.Client
{
    public class Client
    {
        public async Task RunAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            Console.Write("Enter your name: ");
            var userName = Console.ReadLine()?.Trim() ?? "Anonymous";
            var user = new User { Name = userName };

            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken);
            Console.WriteLine($"Connected to {host}:{port}");

            await using var stream = client.GetStream();

            // Start receiving messages
            var receiveTask = Task.Run(async () =>
            {
                var buffer = new byte[512];
                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, cancellationToken);
                    if (bytesRead == 0)
                    {
                        Console.WriteLine("Server disconnected.");
                        break;
                    }

                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine(response.Trim());
                }
            }, cancellationToken);

            // Send user input messages prefixed with user name
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(message))
                    continue;

                var fullMessage = $"Received From {user.Name}: {message}";
                var bytes = Encoding.UTF8.GetBytes(fullMessage + "\n");

                try
                {
                    await stream.WriteAsync(bytes, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Send error: {ex.Message}");
                    break;
                }
            }

            await receiveTask;
        }
    }
}
