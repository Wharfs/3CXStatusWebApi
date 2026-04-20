using System.Linq;
using TCX.Configuration;
using WebAPI.Functions.PhoneSystem;

namespace WebAPI.Functions;

public class Extensions
{
    public static ApiQueryResponse getExtensionProfile(string extensionId)
    {
        var ps = global::TCX.Configuration.PhoneSystem.Root;
        if (ps.GetDNByNumber(extensionId) is not Extension extension)
        {
            return new ApiQueryResponse($"extension {extensionId} not found", "NOT_FOUND");
        }
        return new ApiQueryResponse(extension.CurrentProfile?.Name ?? "unknown", "OK");
    }

    public static ApiQueryResponse setExtensionProfile(string extensionId, string shortCode)
    {
        var profileName = ProfileMapping.ShortCodeToProfileName(shortCode);
        if (profileName is null)
        {
            return new ApiQueryResponse($"unknown profile short-code: {shortCode}", "BAD_REQUEST");
        }

        var ps = global::TCX.Configuration.PhoneSystem.Root;
        if (ps.GetDNByNumber(extensionId) is not Extension extension)
        {
            return new ApiQueryResponse($"extension {extensionId} not found", "NOT_FOUND");
        }

        var profile = extension.FwdProfiles.FirstOrDefault(p => p.Name == profileName);
        if (profile is null)
        {
            return new ApiQueryResponse($"profile '{profileName}' not configured on extension {extensionId}", "NOT_FOUND");
        }

        extension.CurrentProfile = profile;
        extension.Save();
        return new ApiQueryResponse(profileName, "OK");
    }

    public static ApiQueryResponse setAllExtensionsProfile(string shortCode)
    {
        var profileName = ProfileMapping.ShortCodeToProfileName(shortCode);
        if (profileName is null)
        {
            return new ApiQueryResponse($"unknown profile short-code: {shortCode}", "BAD_REQUEST");
        }

        var ps = global::TCX.Configuration.PhoneSystem.Root;
        int applied = 0;
        int skipped = 0;
        foreach (Extension extension in ps.GetExtensions())
        {
            var profile = extension.FwdProfiles.FirstOrDefault(p => p.Name == profileName);
            if (profile is null)
            {
                skipped++;
                continue;
            }
            extension.CurrentProfile = profile;
            extension.Save();
            applied++;
        }

        return new ApiQueryResponse($"{profileName} (applied to {applied}, skipped {skipped})", "OK");
    }
}
