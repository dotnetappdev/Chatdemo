# ChatDemo – gRPC Peer-to-Peer WPF Chat

A clean .NET 10 demo that showcases **gRPC**, **WPF**, **REST API**, and **Entity Framework** principles.

---

## Architecture

```
┌────────────────────────────────────────────────────────┐
│  ChatDemo.Wpf  (WPF .NET 10)                           │
│  ┌──────────────────────┐  ┌───────────────────────┐   │
│  │  Embedded Kestrel    │  │  gRPC Client          │   │
│  │  gRPC Server         │◄─┤  (GrpcPeerClient)     │   │
│  │  (PeerChatService)   │  └──────────┬────────────┘   │
│  └──────────────────────┘             │                 │
│                                       │ HTTP/2 gRPC     │
└───────────────────────────────────────┼─────────────────┘
                                        │
        ┌───────────────────────────────▼──────────────────┐
        │  Another ChatDemo.Wpf instance (another machine) │
        └──────────────────────────────────────────────────┘
                          │
              REST  HTTP  │
                          ▼
          ┌───────────────────────────┐
          │  ChatDemo.Api (.NET 10)   │
          │  ASP.NET Core REST API    │
          │  EF Core + SQL Server /   │
          │  SQLite (dev)             │
          └───────────────────────────┘
```

### Projects

| Project | Framework | Description |
|---------|-----------|-------------|
| `ChatDemo.Protos` | net10.0 | Shared `.proto` definitions (code-generated gRPC stubs) |
| `ChatDemo.Api` | net10.0 | REST Web API – user registration, message history |
| `ChatDemo.Wpf` | net10.0-windows | WPF chat client – hosts its own gRPC server AND acts as a client |

---

## gRPC Design (Peer-to-Peer)

Each WPF instance starts an **in-process Kestrel HTTP/2 server** on startup, making itself directly reachable by other peers — no central message broker needed.

```protobuf
service ChatService {
  rpc SendMessage       (ChatMessageRequest)         returns (ChatMessageReply);       // unary
  rpc SubscribeToMessages (SubscribeRequest)          returns (stream ChatMessageRequest); // server-streaming
  rpc Chat              (stream ChatMessageRequest)   returns (stream ChatMessageRequest); // bidirectional
  rpc Ping              (PingRequest)                returns (PingReply);              // health check
}
```

Messaging flow:

1. User A enters User B's `host:port` and clicks **Connect**
2. A `Ping` RPC confirms B is reachable
3. User A types a message → `SendMessage` RPC fires directly to B's gRPC server
4. B's `PeerChatService` raises the `MessageReceived` event → message appears in B's UI

---

## REST API Endpoints

Base URL: `http://localhost:5000`

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET`  | `/api/users` | List all users |
| `GET`  | `/api/users/online` | List online users |
| `GET`  | `/api/users/{id}` | Get user by ID |
| `POST` | `/api/users/register` | Register / re-register a user |
| `PUT`  | `/api/users/{id}/presence` | Update online status |
| `DELETE` | `/api/users/{id}` | Remove user |
| `GET`  | `/api/messages/conversation?userA=&userB=` | Get conversation history |
| `POST` | `/api/messages` | Save a message |
| `DELETE` | `/api/messages/{id}` | Delete a message |

OpenAPI schema available at `GET /openapi/v1.json` (development mode).

---

## Database

- **SQL Server** (production): configure `ConnectionStrings:DefaultConnection` in `appsettings.json`
- **SQLite** (development / demo): automatically used when no SQL Server connection string is provided

The schema is created automatically on API startup via `EnsureCreated()`.

**Entity Framework migration commands:**
```bash
cd src/ChatDemo.Api
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## Prerequisites

- .NET 10 SDK
- Windows (for WPF client)
- SQL Server LocalDB *or* leave default (SQLite is used automatically)

---

## Running the Demo

### 1 — Start the REST API
```bash
cd src/ChatDemo.Api
dotnet run
# API starts at http://localhost:5000
```

### 2 — Start the first WPF client (User A)
```bash
cd src/ChatDemo.Wpf
dotnet run --port 5100
```

### 3 — Start the second WPF client (User B)
Open a second terminal or copy the exe:
```bash
cd src/ChatDemo.Wpf
dotnet run --port 5101
```

### 4 — Connect the clients
In **User A's** window:
1. Set your display name and port (`5100`)
2. Enter the API URL (`http://localhost:5000`)
3. Click **Go Online**
4. In the "Peer" field enter `localhost:5101` and click **Connect**

In **User B's** window do the same, connecting to `localhost:5100`.

You can now exchange messages in real-time via gRPC!

---

## Project Structure

```
ChatDemo/
├── ChatDemo.sln
└── src/
    ├── ChatDemo.Protos/
    │   ├── ChatDemo.Protos.csproj
    │   └── Protos/
    │       └── chat.proto               ← gRPC service definition
    │
    ├── ChatDemo.Api/
    │   ├── ChatDemo.Api.csproj
    │   ├── Program.cs                   ← App bootstrap, EF auto-migrate
    │   ├── Data/
    │   │   └── ChatDbContext.cs         ← EF Core DbContext
    │   ├── Models/
    │   │   ├── User.cs
    │   │   ├── Message.cs
    │   │   └── Dtos.cs                  ← Request/response records
    │   └── Controllers/
    │       ├── UsersController.cs
    │       └── MessagesController.cs
    │
    └── ChatDemo.Wpf/
        ├── ChatDemo.Wpf.csproj
        ├── App.xaml / App.xaml.cs       ← Starts gRPC Kestrel host
        ├── MainWindow.xaml              ← XAML chat UI
        ├── MainWindow.xaml.cs
        ├── Converters/
        │   └── Converters.cs            ← Value converters for bindings
        ├── Models/
        │   ├── Contact.cs
        │   └── ChatMessage.cs
        ├── Services/
        │   ├── PeerChatService.cs       ← gRPC server implementation
        │   ├── GrpcPeerClient.cs        ← gRPC client wrapper
        │   └── ChatApiService.cs        ← REST API HTTP client
        └── ViewModels/
            └── MainViewModel.cs         ← MVVM ViewModel
```

---

## Key Technologies Demonstrated

| Technology | How it's used |
|------------|--------------|
| **gRPC (unary)** | `SendMessage` – fire-and-forget message delivery |
| **gRPC (server streaming)** | `SubscribeToMessages` – push messages to subscriber |
| **gRPC (bidirectional streaming)** | `Chat` – real-time duplex session |
| **Embedded Kestrel** | Each WPF app hosts its own HTTP/2 server |
| **WPF + MVVM** | `CommunityToolkit.Mvvm`, data bindings, commands |
| **REST API** | User presence, message history via JSON API |
| **Entity Framework Core** | Code-first models, SQL Server + SQLite |
| **.NET 10** | Top-level programs, primary constructors, collection expressions |
