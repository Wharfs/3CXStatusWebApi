using TCX.Configuration;

namespace WebAPI.Functions;

public class System
{
    public static ApiQueryResponse showSystemStatus()
    {
        var ps = global::TCX.Configuration.PhoneSystem.Root;
        Tenant tenant = ps.GetTenant();
        string? overrideOfficeTime = tenant.GetPropertyValue("OVERRIDEOFFICETIME");
        string systemStatus = PhoneSystem.SystemStatus.MapOverrideOfficeTime(overrideOfficeTime);
        return new ApiQueryResponse(systemStatus, "OK");
    }
}
