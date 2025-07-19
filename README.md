# Simple Chat Server
A terminal-based chat server written in C#.

Ported from a [Go project I did last year.](https://github.com/Sieep-Coding/chatserver)

# Guide

#### Requirements
`.NET 9 SDK`

`Compatible terminal environment (Linux, macOS, Windows)`

#### Running the Server
- Open a terminal and navigate to the project root directory.
- Run the server with the following command:
- Run:
 `dotnet run --project CSharpStream server`
- The server listens on port 8000 by default.
- Press Ctrl+C to gracefully stop the server.

#### Running the Client
- Open a new terminal window.
- Run the client with:
- Run: 
`dotnet run --project CSharpStream client`
- When prompted, enter your username.
- Type messages and press Enter to send them to the chat.

#### Project Structure
- `CSharpStream.Server` – TCP server implementation handling client connections and message broadcasting.
- `CSharpStream.Client` – TCP client implementation for sending and receiving chat messages.
- `CSharpStream.Models` – Contains the User model used for storing username information.

#### Troubleshooting
- Ensure no firewall or network restrictions block port 8000.
- If connection is refused, verify the server is running before starting clients.
- Use `Ctrl+C` to exit clients and server cleanly.

### License
[MIT License](https://github.com/Sieep-Coding/simple-chat-csharp/tree/main?tab=MIT-1-ov-file)