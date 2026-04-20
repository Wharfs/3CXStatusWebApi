using FastEndpoints;
using WebAPI.Functions;

namespace WebAPI.Status.Extension;

public class GetExtensionEndpoint : Endpoint<GetExtensionRequest, Response>
{
    public override void Configure()
    {
        Get("/status/extension/{ExtensionID}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetExtensionRequest req, CancellationToken ct)
    {
        var psResponse = Extensions.getExtensionProfile(req.ExtensionID);
        await SendAsync(new Response
        {
            Message = psResponse.Message ?? string.Empty,
            Status = psResponse.Status ?? string.Empty,
            TimeStamp = psResponse.TimeStamp,
        }, cancellation: ct);
    }
}
