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
    public class System
    {

        public static ApiQueryResponse showSystemStatus()
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

            return new ApiQueryResponse("systemStatus", "OK");
        }

    }
}