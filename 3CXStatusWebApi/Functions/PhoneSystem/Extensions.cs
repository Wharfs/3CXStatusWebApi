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

namespace WebAPI.Functions
{
    public class Extensions
    {

        public static ApiQueryResponse getExtensionProfile(string ExtensionID)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var extension = ps.GetDNByNumber(ExtensionID) as Extension;
            return new ApiQueryResponse(extension.CurrentProfile?.Name, "OK");
        }

        public static ApiQueryResponse setExtensionProfile(string ExtensionID, string ProfileName)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var extension = ps.GetDNByNumber(ProfileName) as Extension;

            string newprofile = "";
            switch (ExtensionID)
            {
                case "available": newprofile = "Available"; break;
                case "away": newprofile = "Away"; break;
                case "out_of_office": newprofile = "Out of office"; break;
                case "custom1": newprofile = "Custom 1"; break;
                case "custom2": newprofile = "Custom 2"; break;
            }

            var profile = extension.FwdProfiles.Where(x => x.Name == newprofile).First();
            //var profile = extension.FwdProfiles.ElementAt(i);
            extension.CurrentProfile = profile;
            extension.Save();
            // I mean really sausagewomble - you could check something no ?
            return new ApiQueryResponse(newprofile, "OK");

        }
        public static ApiQueryResponse setAllExtensionsProfile(string ProfileName)
        {
            PhoneSystem ps = PhoneSystem.Root;
            var extensions = ps.GetExtensions();

            string newprofile = "";
            switch (ProfileName)
            {
                case "available": newprofile = "Available"; break;
                case "away": newprofile = "Away"; break;
                case "out_of_office": newprofile = "Out of office"; break;
                case "custom1": newprofile = "Custom 1"; break;
                case "custom2": newprofile = "Custom 2"; break;
            }

            foreach (Extension extension in extensions)
            {
                var profile = extension.FwdProfiles.Where(x => x.Name == newprofile).First();
                //var profile = extension.FwdProfiles.ElementAt(i);
                extension.CurrentProfile = profile;
                extension.Save();
                // or do this ?
                //setExtensionProfile(extension.Number, ProfileName);
            }
            // I mean really sausagewomble - you could check something no ?
            return new ApiQueryResponse(newprofile, "OK");
        }

    }
}