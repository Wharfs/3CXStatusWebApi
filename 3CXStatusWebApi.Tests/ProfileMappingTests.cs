using WebAPI.Functions.PhoneSystem;

public class ProfileMappingTests
{
    [Theory]
    [InlineData("available",     "Available")]
    [InlineData("away",          "Away")]
    [InlineData("out_of_office", "Out of office")]
    [InlineData("custom1",       "Custom 1")]
    [InlineData("custom2",       "Custom 2")]
    public void Recognised_short_codes_map_to_profile_names(string input, string expected)
    {
        Assert.Equal(expected, ProfileMapping.ShortCodeToProfileName(input));
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("AVAILABLE")]  // case-sensitive by design — matches existing switch behaviour
    public void Unknown_short_codes_return_null(string? input)
    {
        Assert.Null(ProfileMapping.ShortCodeToProfileName(input));
    }
}
