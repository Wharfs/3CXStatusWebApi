using System.Threading.Tasks;
using System.Threading;
using System;
using WebAPI.Functions;

public class SetExtensionProfileEndpoint : Endpoint<SetExtensionProfileRequest>
{
    public override void Configure()
    {
        Get("/status/extension/{ExtensionID}/profile/{ProfileName}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetExtensionProfileRequest req, CancellationToken ct)
    {

        string ExtensionID = Route<int>("ExtensionID").ToString();
        string ProfileName = Route<string>("ProfileName");

        var PsStatus = new Extensions();
        var PsResponse = Extensions.setExtensionProfile(ExtensionID, ProfileName);

        System.Diagnostics.Debug.WriteLine(ExtensionID);

        var response = new Response()
        {
            Message = PsResponse.Message,
            Status = PsResponse.Status,
            TimeStamp = PsResponse.TimeStamp,
        };

        await SendAsync(response);
    }
}