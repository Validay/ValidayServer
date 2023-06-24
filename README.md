# ValidayServer
  
  ### Source light server
  
  ![GitHub code size in bytes](https://img.shields.io/github/languages/code-size/Validay/ValidayServer)
  ![GitHub commit activity](https://img.shields.io/github/commit-activity/t/Validay/ValidayServer)
  ![GitHub last commit](https://img.shields.io/github/last-commit/Validay/ValidayServer)

## Roadmap:
  - [x] Caching commands

## How to use:
### 1. Create server settings
<pre>
ServerSettings settings = new ServerSettings
{
    IP = "127.0.0.1",
    BufferSize = 1024
    MaxConnection = 100,
    Port = 8888,
    ConnectingClientQueue = 10,
    ClientFactory = new ClientFactory(),
    ManagerFactory = new ManagerFactory(),
    Logger = new ConsoleLogger(LogType.Info),
};
</pre>

### 2. Create instance server
<pre>
IServer server = new Server(
  settings, 
  true);
</pre>

### 3. Registration managers
<pre>
server.RegistrationManager&ltSomeManager>();
</pre>

### 4. Start server!
<pre>
server.Start();
</pre>
