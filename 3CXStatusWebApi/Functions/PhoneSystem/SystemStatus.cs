namespace WebAPI.Functions.PhoneSystem;

public static class SystemStatus
{
    public static string MapOverrideOfficeTime(string? overrideOfficeTime) => overrideOfficeTime switch
    {
        "0" => "Automatic Office Hours",
        "1" => "Forced Night Mode",
        "2" => "Forced Day Mode",
        _   => "Unknown"
    };
}
