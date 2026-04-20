using System.Reflection;
using FastEndpoints;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebAPI.ApiKey;

namespace WebAPI;

public class Program
{
    // Paths to search for the 3CX assemblies at runtime, in priority order.
    // Populated from config + 3CX ini during Main() before AssemblyResolve is
    // registered, which in turn happens before we call any method that uses
    // a TCX type.
    private static readonly List<string> _assemblySearchPaths = new();
    private static ILogger<Program>? _logger;

    public static void Main(string[] args)
    {
        // IMPORTANT: This method must not reference any type from the TCX.*
        // namespaces, not even via a using-alias. The .NET JIT resolves all
        // type references in a method body before executing any of its IL,
        // which means the TCX assembly would have to be loadable BEFORE
        // AssemblyResolve is registered - chicken-and-egg. The actual PBX
        // connection lives in RunWithPhoneSystem() below so the TCX assembly
        // load only gets triggered when we call that method, by which time
        // AssemblyResolve is wired up and can hand over the right DLL.

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
        _logger = app.Services.GetRequiredService<ILogger<Program>>();

        // Build the assembly search path list before wiring up AssemblyResolve.
        // Priority:
        //   1. PbxAssemblyPath from appsettings.json if set (override for
        //      unusual 3CX layouts or testing).
        //   2. /usr/lib/3cxpbx/ on Linux - system-shared, kept current by
        //      3CX package upgrades. This is the copy the WebApi build
        //      references at build time, so versions match at runtime.
        //   3. {General:AppPath}/Bin from the PBX ini - the legacy per-
        //      instance location. On a multi-year-old install this is
        //      often stale (e.g. a 2022 DLL with a 2026 PBX), which fails
        //      with a runtime assembly-version mismatch. Kept as a fallback
        //      for installs where /usr/lib/3cxpbx isn't populated, such
        //      as 3CX-for-Windows.
        var overridePath = builder.Configuration["PbxAssemblyPath"];
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            _assemblySearchPaths.Add(overridePath);
        }
        if (OperatingSystem.IsLinux())
        {
            _assemblySearchPaths.Add("/usr/lib/3cxpbx");
        }
        var appPath = builder.Configuration["General:AppPath"]
            ?? throw new InvalidOperationException($"Cannot read General:AppPath from {pbxIniPath}");
        _assemblySearchPaths.Add(Path.Combine(appPath, "Bin"));

        _logger.LogInformation("3CX assembly search order: {Paths}",
            string.Join(" -> ", _assemblySearchPaths));

        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        // Read ConfService settings via IConfiguration strings (not TCX types)
        // so this method still doesn't need the TCX assembly resolved.
        var confPort = int.Parse(builder.Configuration["ConfService:ConfPort"]
            ?? throw new InvalidOperationException("Cannot read ConfService:ConfPort"));
        var confUser = builder.Configuration["ConfService:confUser"]
            ?? throw new InvalidOperationException("Cannot read ConfService:confUser");
        var confPass = builder.Configuration["ConfService:confPass"]
            ?? throw new InvalidOperationException("Cannot read ConfService:confPass");

        // First call into TCX-using code happens here. The JIT will compile
        // RunWithPhoneSystem at this moment and the TCX assembly load will
        // be served by AssemblyResolve we just wired up.
        RunWithPhoneSystem(app, confPort, confUser, confPass);
    }

    private static void RunWithPhoneSystem(WebApplication app, int confPort, string confUser, string confPass)
    {
        TCX.Configuration.PhoneSystem.CfgServerHost = "127.0.0.1";
        TCX.Configuration.PhoneSystem.CfgServerPort = confPort;
        TCX.Configuration.PhoneSystem.CfgServerUser = confUser;
        TCX.Configuration.PhoneSystem.CfgServerPassword = confPass;

        var ps = TCX.Configuration.PhoneSystem.Reset(
            TCX.Configuration.PhoneSystem.ApplicationName + new Random(Environment.TickCount).Next().ToString(),
            "127.0.0.1",
            confPort,
            confUser,
            confPass);
        ps.WaitForConnect(TimeSpan.FromSeconds(30));
        _logger?.LogInformation("Connected to 3CX ConfService on port {Port}", confPort);

        app.UseApiKey();
        app.UseAuthorization();
        app.UseFastEndpoints();

        try
        {
            app.Run();
            _logger?.LogInformation("exited gracefully");
        }
        finally
        {
            ps.Disconnect();
        }
    }

    private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name).Name + ".dll";
        foreach (var dir in _assemblySearchPaths)
        {
            var candidate = Path.Combine(dir, name);
            if (!File.Exists(candidate)) continue;
            try
            {
                _logger?.LogInformation("Loading {Name} from {Candidate}", name, candidate);
                return Assembly.LoadFrom(candidate);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load {Name} from {Candidate}, trying next path", name, candidate);
            }
        }
        _logger?.LogError("Could not resolve assembly {Name}; searched: {Paths}", name, string.Join(", ", _assemblySearchPaths));
        return null;
    }
}
