namespace WebAPI.Functions.PhoneSystem;

public static class ProfileMapping
{
    // Converts the wire short-code ("available", "out_of_office", ...) used in the
    // URL path to the 3CX-internal profile display name ("Available", "Out of office", ...).
    // Returns null if the short-code is not recognised so callers can surface a clean error
    // instead of throwing.
    public static string? ShortCodeToProfileName(string? shortCode) => shortCode switch
    {
        "available"     => "Available",
        "away"          => "Away",
        "out_of_office" => "Out of office",
        "custom1"       => "Custom 1",
        "custom2"       => "Custom 2",
        _               => null
    };
}
