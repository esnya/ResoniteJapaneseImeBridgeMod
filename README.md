# ResoniteMozcInputMod

Mozc Input is an unofficial [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that routes Resonite VirtualKeyboard input through a local Japanese input bridge.

Code identity: `MozcInput`

## Installation

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Ensure your Resonite launch options include `-LoadAssembly Libraries/ResoniteModLoader.dll`.
3. Put `MozcInput.dll` and `MozcInput.Bridge.exe` into `rml_mods`.
   Standard Steam path: `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods`
4. Launch Resonite and confirm `Mozc Input` loads in the log.

## Mod Settings

- `Enabled`: enables VirtualKeyboard input routing. Defaults to `true`.
- `BridgePath`: path to `MozcInput.Bridge.exe`. Empty means the bridge executable next to `MozcInput.dll`.
- `AutoStartBridge`: starts the local bridge on demand. Defaults to `true`.
- `ShowCandidatePanel`: shows preedit and candidate text through the VirtualKeyboard text preview. Defaults to `true`.
- `DefaultImeActive`: starts each VirtualKeyboard target in Mozc IME mode. Defaults to `true`.
- `ImeToggleKeyCombos`: semicolon-separated `Renderite.Shared.Key` combos that toggle IME mode. Defaults to `LeftWindows;Alt+BackQuote`.
- `ImeOnKeyCombos`: semicolon-separated `Renderite.Shared.Key` combos that enable IME mode. Defaults include `Control+CapsLock` and `Alt+CapsLock`.
- `ImeOffKeyCombos`: semicolon-separated `Renderite.Shared.Key` combos that disable IME mode. Defaults to `Shift+CapsLock`.
- `ImeToggleTextKeys`, `ImeOnTextKeys`, `ImeOffTextKeys`: fallback virtual key text labels for custom keyboards that do not emit useful `Key` combos. Defaults to `半角/全角;Hankaku/Zenkaku;Kanji`, `Kana`, and `Eisu`.

## Notes

- This is not an official Google, Mozc, or Google Japanese Input product.
- The bridge detects `C:\Program Files (x86)\Google\Google Japanese Input` and talks to `GoogleIMEJaConverter.exe` through the local Google Japanese Input session pipe when available. If that runtime is missing or IPC fails, it falls back to a minimal romaji-to-kana engine.
- Bundled Mozc runtime files, when added under `third_party/mozc/runtime`, must retain the upstream license and notices.

## Development

### Requirements

- .NET 10 SDK
- A Resonite install, or fallback assemblies under `./Resonite`
- Optional: [ResoniteHotReloadLib](https://github.com/Nytra/ResoniteHotReloadLib)

### Build

```sh
dotnet build .\ResoniteMozcInputMod.slnx -c Release -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

### Test

```sh
dotnet test .\ResoniteMozcInputMod.slnx -c Release -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

### Copy to `rml_mods`

```sh
dotnet build .\ResoniteMozcInputMod.slnx -c Release -p:CopyToMods=true -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

### Hot Reload

When built with `EnableHotReloadLibs=true` and `ResoniteHotReloadLib` is present in `rml_libs`, `Mozc Input` registers itself during the initial `rml_mods` load. The hot-reloaded DLL is loaded from `rml_mods\HotReloadMods`.

```sh
dotnet build .\ResoniteMozcInputMod.slnx -c Release -p:EnableHotReloadLibs=true -p:CopyToMods=true -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

## License

[MIT](./LICENSE). See [THIRD_PARTY_NOTICES.md](./THIRD_PARTY_NOTICES.md) for Mozc-related notices.
