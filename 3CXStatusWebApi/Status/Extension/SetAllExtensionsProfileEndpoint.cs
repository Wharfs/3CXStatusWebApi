using FastEndpoints;
using WebAPI.Functions;

namespace WebAPI.Status.Extension;

public class SetAllExtensionsProfileEndpoint : Endpoint<SetAllExtensionsProfileRequest, Response>
{
    public override void Configure()
    {
        Get("/status/extensions/profile/{ProfileName}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(SetAllExtensionsProfileRequest req, CancellationToken ct)
    {
        var psResponse = Extensions.setAllExtensionsProfile(req.ProfileName);
        await SendAsync(new Response
        {
            Message = psResponse.Message ?? string.Empty,
            Status = psResponse.Status ?? string.Empty,
            TimeStamp = psResponse.TimeStamp,
        }, cancellation: ct);
    }
}
