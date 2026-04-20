# 3CXStatusWebApi

This is one half of a two-component system. Project-wide context, use case, 3CX v20 platform facts, modernisation tracks, and known issues are all in the **parent folder's CLAUDE.md** — read that first.

Parent `CLAUDE.md` lives at `../CLAUDE.md` when the user has both repos cloned side-by-side under `Code/3cx/`. If you only see this single repo cloned standalone, the context is:

- This is a small .NET 8 HTTP service hosted on a 3CX v20 PBX server, exposing three endpoints (get extension profile, set one, set all) that wrap the legacy `3cxpscomcpp2.dll` COM Call Control API via `TCX.Configuration` / `TCX.PBXAPI`.
- The sibling repo `github.com/Wharfs/3CXStatusTray` is a per-desktop tray applet that polls these endpoints every 5s.
- Used daily in a small office (~8 people) to flip everyone's extension to "Out of office" at lunch.
- Do NOT push to origin without the user pushing manually.
