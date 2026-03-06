using System.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ChatDemo.Wpf.Services;
using ChatDemo.Wpf.ViewModels;

namespace ChatDemo.Wpf;

/// <summary>
/// Application entry point.
/// Starts an in-process Kestrel gRPC server alongside the WPF UI.
/// Each running instance becomes both a gRPC server AND a gRPC client,
/// enabling true peer-to-peer messaging without a central broker.
/// </summary>
public partial class App : Application
{
    private IHost? _grpcHost;
    private PeerChatService? _peerService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Parse optional port from command-line: --port 5100
        int port = 5100;
        var args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length - 1; i++)
        {
            if ((args[i] == "--port" || args[i] == "-p") &&
                int.TryParse(args[i + 1], out var p))
            {
                port = p;
                break;
            }
        }

        _peerService = new PeerChatService();

        // Build and start the embedded gRPC host
        _grpcHost = BuildGrpcHost(port, _peerService);
        await _grpcHost.StartAsync();

        // Create and show the main window
        var vm = new MainViewModel(_peerService)
        {
            LocalGrpcPort = port.ToString()
        };
        var window = new MainWindow(vm);
        MainWindow = window;
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_grpcHost is not null)
        {
            await _grpcHost.StopAsync(TimeSpan.FromSeconds(3));
            _grpcHost.Dispose();
        }
        base.OnExit(e);
    }

    // ── gRPC host factory ─────────────────────────────────────────────────────

    private static IHost BuildGrpcHost(int port, PeerChatService service)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseKestrel(k =>
        {
            k.ListenLocalhost(port, o => o.Protocols =
                Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
        });

        builder.Services.AddGrpc(opt =>
        {
            opt.EnableDetailedErrors = true;
        });

        // Register the singleton service instance so the same object is used
        builder.Services.AddSingleton(service);

        var app = builder.Build();
        app.MapGrpcService<PeerChatService>();

        return app;
    }
}

