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
using System.Text.Json;

namespace WebAPI
{
    public class status
    {
        public static string showSystemStatus()
        {
            PhoneSystem ps = PhoneSystem.Root;
            Tenant tenant = ps.GetTenant();
            string overrideOfficeTime = tenant.GetPropertyValue("OVERRIDEOFFICETIME");
            //Logger.WriteLine($"status={overrideOfficeTime}");
            string systemStatus = null;
            switch (overrideOfficeTime)
            {

                case "0":
                    {
                        systemStatus = "Automatic Office Hours";
                        break;
                    }
                case "1":
                    {
                        systemStatus = "Forced Night Mode";
                        break;
                    }
                case "2":
                    {
                        systemStatus = "Forced Day Mode";
                        break;
                    }
            }

            return JsonSerializer.Serialize<object>(new
            {
                message = systemStatus,
                status = "OK",
                timestamp = DateTime.Now
            }
            );
        }

        public static string getExtensionProfile(string args1)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var extension = ps.GetDNByNumber(args1) as Extension;
            //Logger.WriteLine($"    CURRENT_STATUS={extension.CurrentProfile?.Name}");

            return JsonSerializer.Serialize<object>(new
            {
                message = extension.CurrentProfile?.Name,
                status = "OK",
                timestamp = DateTime.Now
            }
            );
        }


        public static string setAllExtensionsProfile(string args1)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var extensions = ps.GetExtensions();

            string newprofile = "";
            switch (args1)
            {
                case "available": newprofile = "Available"; break;
                case "away": newprofile = "Away"; break;
                case "out_of_office": newprofile = "Out of office"; break;
                case "custom1": newprofile = "Custom 1"; break;
                case "custom2": newprofile = "Custom 2"; break;
            }

            foreach (Extension extension in extensions)
            {
                Logger.WriteLine($"{extension.Number}");

                var profile = extension.FwdProfiles.Where(x => x.Name == newprofile).First();
                //var profile = extension.FwdProfiles.ElementAt(i);
                extension.CurrentProfile = profile;
                extension.Save();
                Logger.WriteLine($"CURRENT_STATUS={extension.CurrentProfile?.Name}");
                //return ($"{extension.CurrentProfile?.Name}");

            }
            return JsonSerializer.Serialize<object>(new
            {
                message = newprofile,
                status = "OK",
                timestamp = DateTime.Now
            }
            );
        }


    }
}