<div align="center">

# 🖥️ ValidayServer

**Lightweight, extensible TCP server for .NET**

[![.NET](https://img.shields.io/badge/.NET-netstandard2.1-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![C#](https://img.shields.io/badge/C%23-8.0-239120?style=flat-square&logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow?style=flat-square)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-xUnit-blue?style=flat-square)](https://xunit.net)

---

*Simple to start. Easy to extend. Built for real projects.*

</div>

---

## 📋 Table of Contents

- [Features](#-features)
- [Quick Start](#-quick-start)
- [Architecture](#-architecture)
- [Commands](#-commands)
- [Managers](#-managers)
- [Configuration](#-configuration)
- [Logging](#-logging)

---

## ✨ Features

- 🔌 **Async TCP** — non-blocking accept and receive loop
- 🧩 **Manager system** — attach any number of independent managers to the server
- 📦 **Command pool** — reuse command instances to reduce GC pressure  
- 🛡️ **BadPacketDefender** — auto-disconnect clients that send unknown commands
- ⚙️ **Fully configurable** — IP, port, buffer size, connection limits and more
- 🔍 **ICommandRegistry** — managers can inspect registered commands without coupling to `CommandHandlerManager`
- 🧪 **Testable** — clean interfaces throughout, xUnit test suite included

---

## 🚀 Quick Start

### 1. Create a command

```csharp
using ValidayServer.Network.Interfaces;
using ValidayServer.Network.Commands.Interfaces;

public class ChatMessageCommand : IServerCommand
{
    public void Execute(IClient sender, byte[] rawData)
    {
        string message = Encoding.UTF8.GetString(rawData, 2, rawData.Length - 2);
        Console.WriteLine($"[{sender.Ip}:{sender.Port}] says: {message}");
    }
}
```

### 2. Start the server

```csharp
using ValidayServer.Network;
using ValidayServer.Managers;
using ValidayServer.Network.Interfaces;
using ValidayServer.Logging;
using ValidayServer.Logging.Interfaces;

IServer server = new Server();
ILogger logger = new ConsoleLogger(LogType.Info);

// Register managers
CommandHandlerManager commandHandler = new CommandHandlerManager(server, logger);
BadPacketDefenderManager defender    = new BadPacketDefenderManager(server, logger);

// Register commands
commandHandler.RegistrationCommand<ChatMessageCommand>(1);

// Start
server.Start();

while (server.IsRun) { }
```

### 3. Subscribe to events

```csharp
server.OnClientConnected    += client => Console.WriteLine($"{client.Ip} connected");
server.OnClientDisconnected += client => Console.WriteLine($"{client.Ip} disconnected");
server.OnRecivedData        += (client, data) => Console.WriteLine($"Got {data.Length} bytes");
```

---

## 🏗️ Architecture

```
ValidayServer
├── Network
│   ├── IServer            ← main contract
│   ├── IClient            ← connected client contract
│   ├── Server             ← TCP implementation (IDisposable)
│   ├── Client             ← wraps accepted Socket
│   ├── ClientFactory      ← creates IClient from Socket
│   ├── ServerSettings     ← typed configuration (class, not struct)
│   └── UshortConverterId  ← converts first 2 bytes of packet to command ID
│
├── Managers
│   ├── IManager              ← Start / Stop / IsActive / Name
│   ├── ICommandRegistry      ← read-only view of registered commands
│   ├── CommandHandlerManager ← routes packets to IServerCommand handlers
│   └── BadPacketDefenderManager ← disconnects misbehaving clients
│
└── Commands
    ├── IServerCommand   ← Execute(IClient sender, byte[] rawData)
    ├── IClientCommand   ← GetRawData() — for sending back to clients
    └── CommandPool      ← thread-safe object pool per command type
```

### Data flow

```
TCP socket
    │
    ▼
Server.OnDataReceived
    │  fires
    ▼
IServer.OnRecivedData  ──► BadPacketDefenderManager (counts unknown IDs)
    │
    └──► CommandHandlerManager
              │  converts first 2 bytes → ushort command ID
              │  looks up IServerCommand in CommandsMap
              ▼
         IServerCommand.Execute(sender, rawData)
              │
              ▼
         CommandPool.ReturnCommandToPool(...)
```

---

## 📨 Commands

Commands are plain classes implementing `IServerCommand`:

```csharp
public interface IServerCommand
{
    void Execute(IClient sender, byte[] rawData);
}
```

The first **2 bytes** of every packet are treated as a `ushort` command ID. The rest is payload — you decide the format.

**Register before calling `server.Start()`:**

```csharp
commandHandler.RegistrationCommand<ChatMessageCommand>(1);
commandHandler.RegistrationCommand<MoveCommand>(2);
commandHandler.RegistrationCommand<PingCommand>(3);
```

Rules:
- Each ID must be unique
- Each command type can only be registered once
- Registration after `Start()` throws `InvalidOperationException`

**Sending data back to a client:**

```csharp
public class PongClientCommand : IClientCommand
{
    public byte[] GetRawData()
    {
        // first 2 bytes = command ID, rest = payload
        byte[] id      = BitConverter.GetBytes((ushort)10);
        byte[] payload = Encoding.UTF8.GetBytes("pong");
        return id.Concat(payload).ToArray();
    }
}

// inside a server command:
server.SendToClient(sender, new PongClientCommand());
```

---

## 🧩 Managers

Managers attach to the server's event bus. Any class implementing `IManager` can be registered.

```csharp
public interface IManager
{
    string Name    { get; }
    bool   IsActive { get; }
    void   Start();
    void   Stop();
}
```

### Built-in managers

| Manager | Purpose |
|---|---|
| `CommandHandlerManager` | Routes packets to registered `IServerCommand` handlers |
| `BadPacketDefenderManager` | Disconnects clients after N unknown command IDs |

### Custom manager example

```csharp
public class MetricsManager : IManager
{
    public string Name     => nameof(MetricsManager);
    public bool   IsActive { get; private set; }

    private readonly IServer _server;
    private int _totalPackets;

    public MetricsManager(IServer server)
    {
        _server = server;
        _server.RegistrationManager(this);
    }

    public void Start()
    {
        IsActive = true;
        _server.OnRecivedData += OnData;
    }

    public void Stop()
    {
        IsActive = false;
        _server.OnRecivedData -= OnData;
    }

    private void OnData(IClient client, byte[] data)
        => Console.WriteLine($"Total packets: {++_totalPackets}");
}
```

> **Note:** `ICommandRegistry` lets your managers inspect registered commands without depending on `CommandHandlerManager` directly:
> ```csharp
> var registry = server.Managers.OfType<ICommandRegistry>().FirstOrDefault();
> bool known = registry?.CommandsMap.ContainsKey(commandId) ?? false;
> ```

---

## ⚙️ Configuration

```csharp
var settings = new ServerSettings(
    ip:                    "0.0.0.0",   // bind address
    port:                  7777,         // port
    connectingClientQueue: 20,           // accept backlog
    bufferSize:            4096,         // receive buffer bytes
    maxConnections:        500,          // max simultaneous clients
    maxDepthReadPacket:    64,           // packet read depth guard
    markerStartPacket:     new byte[] { 0xFF, 0xFE }, // packet start marker
    clientFactory:         new ClientFactory(),
    logger:                new ConsoleLogger(LogType.Info));

IServer server = new Server(settings, hideSocketError: false);
```

| Parameter | Default | Description |
|---|---|---|
| `Ip` | `127.0.0.1` | Bind address |
| `Port` | `8888` | Listen port (0–65535) |
| `ConnectingClientQueue` | `10` | OS accept backlog |
| `BufferSize` | `1024` | Receive buffer in bytes |
| `MaxConnection` | `100` | Max simultaneous clients |
| `MaxDepthReadPacket` | `64` | Framing guard depth |
| `MarkerStartPacket` | `{1,2,3}` | Packet boundary marker |

---

## 📝 Logging

Implement `ILogger` to plug in any logging backend:

```csharp
public interface ILogger
{
    void Log(string message, LogType logType);
}
```

`ConsoleLogger` is included out of the box. Filter by level:

```csharp
// Only Warning and above will be printed
var logger = new ConsoleLogger(LogType.Warning);
```

| Level | When to use |
|---|---|
| `Low` | Verbose / per-packet traces |
| `Info` | Connect / disconnect / start / stop |
| `Warning` | Unexpected but recoverable events |
| `Error` | Socket failures |
| `CriticalError` | Server cannot start or crashed |
