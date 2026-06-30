# ResoniteJapaneseImeBridgeMod

Japanese IME Bridge is an unofficial [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod that routes Resonite VirtualKeyboard input through a local Japanese IME backend.

Code identity: `JapaneseImeBridge`

## Install

1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Ensure your Resonite launch options include `-LoadAssembly Libraries/ResoniteModLoader.dll`.
3. Put `JapaneseImeBridge.dll` into `rml_mods`.
   Standard Steam path: `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods`
4. Install Google Japanese Input locally if you want conversion. The mod does not bundle it.
5. Launch Resonite and confirm `Japanese IME Bridge` loads in the log.

## Configuration

- `Enabled`: enables the mod. Defaults to `true`.
- `GoogleJapaneseInputDirectory`: path to the Google Japanese Input install directory containing `GoogleIMEJaConverter.exe`. Empty uses the standard Windows install path.
- `ShowCandidatePanel`: displays composition and candidates in `VirtualKeyboard.TextPreview` when available. Defaults to `true`.
- `DefaultImeActive`: starts each VirtualKeyboard target in Japanese IME mode. Defaults to `true`.
- `ImeToggleKeyCombos`: semicolon-separated `Renderite.Shared.Key` combos that toggle Japanese IME mode. Defaults include `LeftWindows` and common OS IME-like combinations.
- `ImeOnKeyCombos` / `ImeOffKeyCombos`: semicolon-separated key combos that force IME on/off.
- `ImeToggleTextKeys` / `ImeOnTextKeys` / `ImeOffTextKeys`: fallback virtual key text names such as `半角/全角`, `Kana`, and `Eisu`.

## Backend

Japanese IME Bridge does not bundle Mozc or Google Japanese Input.

The current backend is experimental and Windows-only. It detects a local Google Japanese Input installation and talks to `GoogleIMEJaConverter.exe` through the local `\\.\pipe\googlejapaneseinput.*.session` pipe using a Mozc-derived command protocol.

This is local IPC, not network communication. This mod is not an official Google, Google Japanese Input, or Mozc product, and the converter pipe is not a public stable API. Google Japanese Input updates may break this backend.

If the converter is missing, unavailable, or stops responding, Japanese IME handling is disabled and VirtualKeyboard input is left in pass-through mode. There is no romaji-kana fallback engine in the public build.

The backend implementation references Mozc command protocol concepts:
https://github.com/google/mozc/blob/master/src/protocol/commands.proto

## Build

### Requirements

- .NET 10 SDK
- A Resonite install, or fallback assemblies under `./Resonite`
- Optional: [ResoniteHotReloadLib](https://github.com/Nytra/ResoniteHotReloadLib)

### Build and test

```powershell
dotnet build .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

```powershell
dotnet test .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

### Copy to `rml_mods`

```powershell
dotnet build .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:CopyToMods=true -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

### Hot Reload

When built with `EnableHotReloadLibs=true` and `ResoniteHotReloadLib` is present in `rml_libs`, `Japanese IME Bridge` registers itself during the initial `rml_mods` load. The hot-reloaded DLL is loaded from `rml_mods\HotReloadMods`.

```powershell
dotnet build .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:EnableHotReloadLibs=true -p:CopyToMods=true -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

## License

[MIT](./LICENSE).
