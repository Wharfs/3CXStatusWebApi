using System.Threading.Tasks;
using System.Threading;
using System;
using WebAPI.Functions;

public class GetExtensionEndpoint : Endpoint<GetExtensionRequest>
{
    public override void Configure()
    {
        Get("/status/extension/{ExtensionID}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetExtensionRequest req, CancellationToken ct)
    {
        string ExtensionID = Route<int>("ExtensionID").ToString();

        var PsStatus = new Extensions();
        var PsResponse = Extensions.getExtensionProfile(ExtensionID);
        
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