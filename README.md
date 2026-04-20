# 3CXStatusWebApi

Small HTTP service, runs on the 3CX server, that wraps a narrow subset of the 3CX Call Control API so remote clients can query and flip extension profiles over plain HTTP.

Designed as the server half of a two-component tool; the desktop half is [3CXStatusTray](https://github.com/Wharfs/3CXStatusTray). Together they provide a one-click "phones on / phones off" control for a small office.

## Endpoints

All endpoints are `AllowAnonymous` by default. Optional API-key auth can be enabled by setting `ApiKey` in `appsettings.json`; when set, all requests must carry an `X-API-Key` header matching the configured value.

```
GET /status/extension/{id}                        # current forwarding profile of one extension
GET /status/extension/{id}/profile/{shortcode}    # set one extension's profile
GET /status/extensions/profile/{shortcode}        # set every extension's profile in one call
```

`shortcode` is one of: `available`, `away`, `out_of_office`, `custom1`, `custom2`.

Responses are JSON:

```json
{ "message": "Available", "status": "OK", "timeStamp": "..." }
```

## Prerequisites

- 3CX v20 Phone System (Debian or Windows-hosted; tested against the standard v20 Debian install).
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for building.
- Access to the `3cxpscomcpp2.dll` managed assembly that ships with 3CX (on a v20 Debian install, at `/usr/lib/3cxpbx/3cxpscomcpp2.dll`).

## Build

Cross-platform-buildable — the tray half is Windows-only, but this service isn't:

```bash
# On the 3CX server (default PbxBinPath already correct):
dotnet build -c Release

# On any Linux dev box, with the DLL copied to ~/lib/3cx/:
dotnet build -c Release -p:PbxBinPath=/home/you/lib/3cx

# On Windows with a legacy 3CX-for-Windows install:
dotnet build -c Release
```

`PbxBinPath` has per-OS defaults (Linux: `/var/lib/3cxpbx/Instance1/Bin`, Windows: `C:\Program Files\3CX Phone System\Bin`). Override at build time if your install lives elsewhere.

Tests:

```bash
dotnet test
```

Covers the profile-name mapping, `OVERRIDEOFFICETIME` string mapping, and the API-key middleware — everything that doesn't depend on a running 3CX instance.

## Deploy

Publish and copy to the 3CX server:

```bash
dotnet publish -c Release -o publish
# then: scp -r publish/* root@<3cx>:/opt/3CXWebApi/
```

The service expects `3CXPhoneSystem.ini` in its working directory to discover the ConfService host, port, and credentials — these are written by 3CX itself during PBX install. Run it from the 3CX install directory, or set `PbxIniPath` in `appsettings.json` to point elsewhere.

### Configuration

`appsettings.json` at runtime:

```json
{
  "PbxIniPath": "3CXPhoneSystem.ini",
  "Urls": "http://*:8889",
  "ApiKey": "",
  "Logging": { "LogLevel": { "Default": "Information" } }
}
```

Set `ApiKey` to a random secret to require an `X-API-Key` header on every request. Leave empty to accept unauthenticated requests (fine on a trusted LAN).

## Credits

Originally adapted from [Montesuma80/3cx-web-API](https://github.com/Montesuma80/3cx-web-API) and examples at [fzany/3CXCallControlAPI_v16](https://github.com/fzany/3CXCallControlAPI_v16). Thanks to both.

Uses [FastEndpoints](https://fast-endpoints.com/) for the HTTP layer.

## License

MIT. See `LICENSE.md`.
