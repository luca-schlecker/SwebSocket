
# ![SwebSocket Logo](https://raw.githubusercontent.com/luca-schlecker/SwebSocket/main/icon.png) SwebSocket

![NuGet Downloads](https://img.shields.io/nuget/dt/SwebSocket) ![GitHub License](https://img.shields.io/github/license/luca-schlecker/SwebSocket) ![NuGet Version](https://img.shields.io/nuget/v/SwebSocket)

A handwritten, easy to use WebSocket Library that aims to:
- Follow [RFC6455](https://datatracker.ietf.org/doc/html/rfc6455)
- Be easy to use
- Allow [Client](#client) and [Server](#server) use-cases
- Integrate [Secure Connections](#secure-connection)
- Be Portable (`netstandard2.1`)

## Disclaimer

> ⚠️ Warning ⚠️
> 
> This is a fun-project and may contain severe errors. You should probably not use this in production.

## Examples
### Client
#### Echo Client (Manual)
```cs
using SwebSocket;

var ws = WebSocket
    .Connect()
    .To("wss://echo.websocket.org/")
    .Build();

// Discard first message
_ = ws.Receive();

while (true) {
    var line = Console.ReadLine();
    if (line == null) break;
    ws.Send(line);
    var answer = ws.Receive();
    if (answer is TextMessage text)
        Console.WriteLine($"Received: {text.Text}");
}
```

#### Echo Client (Background Poller)
```cs
using SwebSocket;

var ws = WebSocket
    .Connect()
    .To("wss://echo.websocket.org/")
    .Build();

ws.OnMessage += (_, message) => {
    if (message is TextMessage text)
        Console.WriteLine($"Received: {text.Text}");
};

BackgroundMessagePoller.Poll(ws);

while (true) {
    var line = Console.ReadLine();
    if (line == null) break;
    ws.Send(line);
}
```

> 💬 Important 💬 
> 
> The `WebSocket.OnMessage` event will (by default) **not** be raised.
> You can raise it manually by calling `WebSocket.EmitMessage(Message)` or use a Poller which will call it under the hood.

### Server
#### Echo Server (Manual)
```cs
using SwebSocket;

var listener = new Listener(IPAddress.Any, 3000);

while (true) {
    var ws = listener.Accept();

    try {
        while (true) {
            var message = ws.Receive();
            ws.Send(message);
        }
    }
    catch { }
}
```

#### Echo Server (Blocking Poller)
```cs
using SwebSocket;

var listener = new Listener(IPAddress.Any, 3000);

while (true) {
    var ws = listener.Accept();

    ws.OnMessage += (_, m) => ws.Send(m);
    new BlockingMessagePoller(ws).Poll();
}
```

### Secure Connection
#### Client
```cs
using SwebSocket;

var listener = new Listener(IPAddress.Any, 3000)
    .UseSsl(SslOptions.NoSsl)                // No Ssl
    .UseSsl(SslOptions.ForServer(identity)); // X509Certificate2 as Identity

// ...
```
#### Server
```cs
using SwebSocket;

var ws = WebSocket
    .Connect()
    .To("wss://127.0.0.1/")
    .UseSsl(SslOptions.ForClient(
        "custom authority", // The name to validate the certificate against
        caCertificates      // X509Certificate2Collection
    ))
    .Build();

// ...
```

## Related Projects
- [WebSocket-Sharp](https://github.com/sta/websocket-sharp)
- [System.Net.WebSockets](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets)
- [Fleck](https://github.com/statianzo/Fleck)
- [WatsonWebsockte](https://github.com/jchristn/WatsonWebsocket)