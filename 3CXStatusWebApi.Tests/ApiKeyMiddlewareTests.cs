using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WebAPI.ApiKey;

public class ApiKeyMiddlewareTests
{
    private static HttpClient BuildClient(string? configuredKey)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureAppConfiguration(c =>
                {
                    c.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "ApiKey", configuredKey }
                    });
                });
                webHost.Configure(app =>
                {
                    app.UseApiKey();
                    app.Run(async ctx => await ctx.Response.WriteAsync("ok"));
                });
            }).Start();
        return host.GetTestClient();
    }

    [Fact]
    public async Task Requests_pass_through_when_no_key_is_configured()
    {
        var client = BuildClient(configuredKey: "");
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Requests_are_rejected_when_key_configured_but_header_missing()
    {
        var client = BuildClient(configuredKey: "secret");
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Requests_are_rejected_when_header_value_does_not_match()
    {
        var client = BuildClient(configuredKey: "secret");
        client.DefaultRequestHeaders.Add("X-API-Key", "wrong");
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Requests_pass_when_header_matches()
    {
        var client = BuildClient(configuredKey: "secret");
        client.DefaultRequestHeaders.Add("X-API-Key", "secret");
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
