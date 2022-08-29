global using FastEndpoints;
using System;
using System.Collections.Generic;
using System.Text;
using TCX.Configuration;
using TCX.PBXAPI;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using WebAPI.Functions;

namespace WebAPI
{
    class Program
    {

        static Dictionary<string, Dictionary<string, string>> iniContent =
            new Dictionary<string, Dictionary<string, string>>(
                StringComparer.InvariantCultureIgnoreCase);

        public static bool Stop { get; private set; }
        public static String Debugger;
        static void ReadConfiguration(string filePath)
        {
            var content = File.ReadAllLines(filePath);
            Dictionary<string, string> CurrentSection = null;
            string CurrentSectionName = null;
            for (int i = 1; i < content.Length + 1; i++)
            {
                var s = content[i - 1].Trim();
                if (s.StartsWith("["))
                {
                    CurrentSectionName = s.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[0];
                    CurrentSection = iniContent[CurrentSectionName] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                }
                else if (CurrentSection != null && !string.IsNullOrWhiteSpace(s) && !s.StartsWith("#") && !s.StartsWith(";"))
                {
                    var res = s.Split("=").Select(x => x.Trim()).ToArray();
                    CurrentSection[res[0]] = res[1];
                }
                else
                {
                    //Logger.WriteLine($"Ignore Line {i} in section '{CurrentSectionName}': '{s}' ");
                }
            }
            instanceBinPath = Path.Combine(iniContent["General"]["AppPath"], "Bin");
        }

        static void Bootstrap(string[] args)
        {

            String Port = "0";
            Program.Debugger = "off";
            PhoneSystem.CfgServerHost = "127.0.0.1";
            PhoneSystem.CfgServerPort = int.Parse(iniContent["ConfService"]["ConfPort"]);
            PhoneSystem.CfgServerUser = iniContent["ConfService"]["confUser"];
            PhoneSystem.CfgServerPassword = iniContent["ConfService"]["confPass"];
            var ps = PhoneSystem.Reset(
                PhoneSystem.ApplicationName + new Random(Environment.TickCount).Next().ToString(),
                "127.0.0.1",
                int.Parse(iniContent["ConfService"]["ConfPort"]),
                iniContent["ConfService"]["confUser"],
                iniContent["ConfService"]["confPass"]);
            ps.WaitForConnect(TimeSpan.FromSeconds(30));

            try
            {
                var builder = WebApplication.CreateBuilder();
                builder.Services.AddFastEndpoints();

                var app = builder.Build();
                builder.WebHost.UseUrls("http://*:8889");

                app.UseAuthorization();
                app.UseFastEndpoints();
                app.Run();

            }

            finally
            {
                ps.Disconnect();
            }
        }

        static string instanceBinPath;

        static void Main(string[] args)
        {

            try
            {
                var filePath = @"3CXPhoneSystem.ini";
                if (!File.Exists(filePath))
                {
                    Logger.WriteLine(filePath);
                    throw new Exception("Cannot find 3CXPhoneSystem.ini");
                }
                ReadConfiguration(filePath);
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                Bootstrap(args);
                Logger.WriteLine("exited gracefully");
            }
            catch (Exception ex)
            {
                Logger.WriteLine(ex.ToString());
            }
        }
        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name).Name;
            try
            {
                return Assembly.LoadFrom(Path.Combine(instanceBinPath, name + ".dll"));
            }
            catch
            {
                return null;
            }
        }
    }

}
