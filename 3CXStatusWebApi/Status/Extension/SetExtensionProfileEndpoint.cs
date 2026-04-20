using FastEndpoints;
using WebAPI.Functions;

namespace WebAPI.Status.Extension;

public class SetExtensionProfileEndpoint : Endpoint<SetExtensionProfileRequest, Response>
{
    public override void Configure()
    {
        Get("/status/extension/{ExtensionID}/profile/{ProfileName}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetExtensionProfileRequest req, CancellationToken ct)
    {
        var psResponse = Extensions.setExtensionProfile(req.ExtensionID, req.ProfileName);
        await SendAsync(new Response
        {
            Message = psResponse.Message ?? string.Empty,
            Status = psResponse.Status ?? string.Empty,
            TimeStamp = psResponse.TimeStamp,
        }, cancellation: ct);
    }
}
