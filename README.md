# Simple Chat Server
A terminal-based chat server written in C#.

Recreated from a [Go project I did last year.](https://github.com/Sieep-Coding/chatserver)

> [!WARNING]  
> Only Tested on Linux PopOS!

![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)
![PopOS Passing](https://img.shields.io/badge/PopOS-Passing-darkgreen.svg)
[![wakatime](https://wakatime.com/badge/user/2156ce13-ae9d-4c0e-a543-89b2bddcd2f6/project/5b0c4b85-4c80-47c8-a9ae-26af61b969a9.svg)](https://wakatime.com/badge/user/2156ce13-ae9d-4c0e-a543-89b2bddcd2f6/project/5b0c4b85-4c80-47c8-a9ae-26af61b969a9)

### Server
Listens for connections & messages, logs in real-time.
![](https://github.com/Sieep-Coding/simple-chat-csharp/blob/main/Public/image-server-logged.png)

### Client
Steve sends a message to the server.
![](https://github.com/Sieep-Coding/simple-chat-csharp/blob/main/Public/client1.png)

Nick sends a message to the server.
![](https://github.com/Sieep-Coding/simple-chat-csharp/blob/main/Public/client2.png)
# Guide

#### Requirements
`.NET 9 SDK`

`Compatible terminal environment (Linux, macOS, Windows)`

#### Running the Server
- Open a terminal and navigate to the project root directory.
- Run:
```bash
dotnet run --project CSharpStream server
```
- The server listens on port 8000 by default.
- Press `Ctrl+C` to gracefully stop the server.

#### Running the Client
- Open a new terminal window.
- Run: 
```bash
dotnet run --project CSharpStream client
```
- When prompted, enter your username.
- Type messages and press Enter to send them to the chat.

#### Project Structure
- `CSharpStream.Server` – TCP server implementation handling client connections and message broadcasting.
- `CSharpStream.Client` – TCP client implementation for sending and receiving chat messages.
- `CSharpStream.Models` – Contains the `Message` and `User` models, used for structured data transfer and managing user identity information.
- `ChatServer.cs` - Works as the service layer.
- `Program.cs` - Starts client/server based on command line arguments.

#### Troubleshooting
- Ensure no firewall or network restrictions block port 8000.
- If connection is refused, verify the server is running before starting clients.
- Use `Ctrl+C` to exit clients and server cleanly.

## License
[MIT License](https://github.com/Sieep-Coding/simple-chat-csharp/tree/main?tab=MIT-1-ov-file)