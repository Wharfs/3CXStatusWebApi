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
                //SampleStarter.StartSample(args);
			if (!HttpListener.IsSupported)
            {
                Logger.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }
            
            // URI prefixes are required,
            if (args.Length ==  0)
                {
                    Logger.WriteLine("No Port Submitted, use Generic Port: 8889");
                    Logger.WriteLine("Debug Mode off");
                    Port = "8889";
                }
            else   
                {
                    Logger.WriteLine($"Port Submitted, use Generic Port: {args[0]}");
                    Port = args[0];
                if (args.GetUpperBound(0) == 0)
                    {
                        Logger.WriteLine("Debug Mode off");
                    }
                else 
                    {
                        if (args[1] == "debug") 
                            {
                                Program.Debugger = args[1];
                                Logger.WriteLine("Debug Mode ON");
                            }
                        else
                            {
                                Logger.WriteLine("Debug Mode off, wrong parameter");
                            }
                    }
                }
            var prefixes = new List<string>() { $"http://*:{Port}/" };
            
            

            // Create a listener.
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            foreach (string s in prefixes)
            {
                Logger.WriteLine(s);
                listener.Prefixes.Add(s);
            }
                listener.Start();
                
                Logger.WriteLine("Listening... on Port " + Port + " for Development");
                Logger.WriteLine("To Stop open URL: http://127.0.0.1:" + Port + "/stop");
                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    Logger.WriteLine(request.RawUrl);

                    string documentContents;
                    using (Stream receiveStream = request.InputStream)
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            documentContents = readStream.ReadToEnd();
                        }
                    }
                    String url = request.RawUrl;
                    String[] queryStringArray = url.Split('/');
                    try
                    {

                    string respval = "idle";
                    string text;
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        text = reader.ReadToEnd();
                    }           

                        switch (queryStringArray[1])
                        {
                            case "status_sytem":
                                {
                                    respval = status.showSystemStatus();
                                }
                                break;
                            case "status_extension":
                                {
                                    respval = status.getExtensionProfile(queryStringArray[2]);
                                }
                                break;
                            case "extensions_set_all_profile":
                                {
                                    respval = status.setAllExtensionsProfile(queryStringArray[2]);
                                }
                                break;
                            case "clear":
                                {
                                    Console.WriteLine("Clearing the screen!");
                                    Console.Clear();
                                }
                                break;
                            case "stop":
                                {
                                    respval = "<HTML><BODY> Server Stopped</BODY></HTML>";
                                    listener.Stop();
                                    break;
                                    throw new Exception("System Stopped");
                                }
                            default:
                                break;
                        }
                                // Obtain a response object.
                                Logger.WriteLine(respval);
                                HttpListenerResponse response = context.Response;
                                // Allow cross origin requests
                                response.AddHeader("Access-Control-Allow-Origin", "null");
                                // Construct a response.
                                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(respval);
                                // Get a response stream and write the response to it.
                                response.ContentLength64 = buffer.Length;
                                System.IO.Stream output = response.OutputStream;
                                output.Write(buffer, 0, buffer.Length);
                                // You must close the output stream.
                                output.Close();

                    }
                    catch (Exception ex)
                    {
                        if (queryStringArray[1] == "Stop")
                        {
                            Logger.WriteLine("system Stopped");
                            throw new Exception("System Stopped");
                        }
                        else
                        {
                            Logger.WriteLine(ex.Message);
                            continue;
                        }
                    }
                }
  
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
