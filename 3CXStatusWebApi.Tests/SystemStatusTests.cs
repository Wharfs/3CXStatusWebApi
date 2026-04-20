using WebAPI.Functions.PhoneSystem;

public class SystemStatusTests
{
    [Theory]
    [InlineData("0", "Automatic Office Hours")]
    [InlineData("1", "Forced Night Mode")]
    [InlineData("2", "Forced Day Mode")]
    [InlineData("99", "Unknown")]
    [InlineData(null, "Unknown")]
    public void MapOverrideOfficeTime_returns_expected_label(string? input, string expected)
    {
        Assert.Equal(expected, SystemStatus.MapOverrideOfficeTime(input));
    }
}
