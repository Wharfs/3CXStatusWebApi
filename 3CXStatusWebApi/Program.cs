using System.Reflection;
using FastEndpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TCX.Configuration;
using TCXPhoneSystem = TCX.Configuration.PhoneSystem;
using WebAPI.ApiKey;

namespace WebAPI;

public class Program
{
    private static string _instanceBinPath = string.Empty;

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        var pbxIniPath = builder.Configuration["PbxIniPath"] ?? "3CXPhoneSystem.ini";

        // 3CXPhoneSystem.ini is itself an INI file - load it into IConfiguration
        // so values like General:AppPath and ConfService:ConfPort are available
        // via the standard key:subkey syntax.
        builder.Configuration.AddIniFile(pbxIniPath, optional: false, reloadOnChange: false);

        builder.Services.AddFastEndpoints();

        var app = builder.Build();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();

        var appPath = builder.Configuration["General:AppPath"]
            ?? throw new InvalidOperationException($"Cannot read General:AppPath from {pbxIniPath}");
        _instanceBinPath = Path.Combine(appPath, "Bin");

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        var confPort = int.Parse(builder.Configuration["ConfService:ConfPort"]
            ?? throw new InvalidOperationException("Cannot read ConfService:ConfPort"));
        var confUser = builder.Configuration["ConfService:confUser"]
            ?? throw new InvalidOperationException("Cannot read ConfService:confUser");
        var confPass = builder.Configuration["ConfService:confPass"]
            ?? throw new InvalidOperationException("Cannot read ConfService:confPass");

        TCXPhoneSystem.CfgServerHost = "127.0.0.1";
        TCXPhoneSystem.CfgServerPort = confPort;
        TCXPhoneSystem.CfgServerUser = confUser;
        TCXPhoneSystem.CfgServerPassword = confPass;

        var ps = TCXPhoneSystem.Reset(
            TCXPhoneSystem.ApplicationName + new Random(Environment.TickCount).Next().ToString(),
            "127.0.0.1",
            confPort,
            confUser,
            confPass);
        ps.WaitForConnect(TimeSpan.FromSeconds(30));
        logger.LogInformation("Connected to 3CX ConfService on port {Port}", confPort);

        app.UseApiKey();
        app.UseAuthorization();
        app.UseFastEndpoints();

        try
        {
            app.Run();
            logger.LogInformation("exited gracefully");
        }
        finally
        {
            ps.Disconnect();
        }
    }

    private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        try
        {
            var name = new AssemblyName(args.Name).Name;
            return Assembly.LoadFrom(Path.Combine(_instanceBinPath, name + ".dll"));
        }
        catch
        {
            return null;
        }
    }
}
