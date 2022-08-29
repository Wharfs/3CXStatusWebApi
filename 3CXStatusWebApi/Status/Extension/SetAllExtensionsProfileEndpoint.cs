using System.Threading.Tasks;
using System.Threading;
using System;
using WebAPI.Functions;

public class SetAllExtensionsProfileEndpoint : Endpoint<SetExtensionProfileRequest>
{
    public override void Configure()
    {
        Get("/status/extensions/profile/{ProfileName}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetExtensionProfileRequest req, CancellationToken ct)
    {

        string ProfileName = Route<string>("ProfileName");

        var PsStatus = new Extensions();
        var PsResponse = Extensions.setAllExtensionsProfile(ProfileName);

        System.Diagnostics.Debug.WriteLine(ProfileName);

        var response = new Response()
        {
            Message = PsResponse.Message,
            Status = PsResponse.Status,
            TimeStamp = PsResponse.TimeStamp,
        };

        await SendAsync(response);
    }
}