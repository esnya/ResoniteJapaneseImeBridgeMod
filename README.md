# ResoniteJapaneseImeBridgeMod

Japanese IME Bridgeは、Resoniteの`VirtualKeyboard`入力をローカルの日本語IMEへ渡す、非公式の[ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader)向けModです。

コード上の名前は`JapaneseImeBridge`です。

## インストール

1. [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader)をインストールします。
2. Resoniteの起動オプションに`-LoadAssembly Libraries/ResoniteModLoader.dll`を追加します。
3. 最新リリースの[`JapaneseImeBridge.dll`](https://github.com/esnya/ResoniteJapaneseImeBridgeMod/releases/latest/download/JapaneseImeBridge.dll)をダウンロードし、`rml_mods`に配置します。
   Steam版の標準パスは`C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods`です。
4. 変換機能を使う場合は、Google 日本語入力を別途インストールします。このModには同梱されていません。
5. Resoniteを起動し、ログで`Japanese IME Bridge`が読み込まれたことを確認します。

## 設定

- `Enabled`: IMEブリッジを有効にします。既定値は`true`です。
- `GoogleJapaneseInputDirectory`: `GoogleIMEJaConverter.exe`があるGoogle 日本語入力のインストール先です。空欄では標準のインストール先を使います。
- `ShowCandidatePanel`: 利用できる場合は、入力中の文字と変換候補を`VirtualKeyboard.TextPreview`に表示します。既定値は`true`です。
- `DefaultImeActive`: 仮想キーボードごとにIMEをオンで開始するかを指定します。既定値は`false`です。
- `ImeToggleKeyCombos`: IMEを切り替える`Renderite.Shared.Key`の組み合わせです。複数指定は`;`で区切ります。既定値には`LeftWindows`などの一般的なIME切替操作が含まれます。
- `ImeOnKeyCombos` / `ImeOffKeyCombos`: IMEをオン／オフにするキー操作です。複数指定は`;`で区切ります。
- `ImeToggleTextKeys` / `ImeOnTextKeys` / `ImeOffTextKeys`: キー操作に一致しない場合に、IMEの切替／オン／オフを判定する仮想キー文字列です。`半角/全角`、`Kana`、`Eisu`などを`;`で区切ります。

## バックエンド

Japanese IME Bridgeには、MozcやGoogle 日本語入力を同梱していません。

現在のバックエンドは実験的なWindows専用機能です。ローカルのGoogle 日本語入力を検出し、Mozc由来のコマンドプロトコルを使って、名前付きパイプ`\\.\pipe\googlejapaneseinput.*.session`経由で`GoogleIMEJaConverter.exe`と通信します。

通信はローカルIPCで、ネットワークは使用しません。このModはGoogle、Google 日本語入力、Mozcの公式製品ではありません。また、この名前付きパイプは公開された安定APIではないため、Google 日本語入力の更新により動作しなくなることがあります。

コンバーターが見つからない、利用できない、または応答しない場合は、IME処理を無効にして`VirtualKeyboard`入力をそのまま通します。公開ビルドにはローマ字からかなへの代替変換機能はありません。

バックエンドの実装は[Mozcのコマンドプロトコル](https://github.com/google/mozc/blob/master/src/protocol/commands.proto)を参照しています。

## 互換性

リリースビルドは、Resonite `2026.8.27.1094`の公開ゲームアセンブリを使用してコンパイルしています。Google 日本語入力のコンバーターパイプは公開された安定APIではないため、ResoniteまたはGoogle 日本語入力を更新した後は変換機能を再確認してください。

## ビルド

### 必要なもの

- .NET 10 SDKが必要です。
- Resoniteのインストール先、または`./Resonite`以下の代替アセンブリが必要です。
- ホットリロードを使う場合は、[ResoniteHotReloadLib](https://github.com/Nytra/ResoniteHotReloadLib)が必要です。

### ビルドとテスト

```powershell
dotnet build .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

```powershell
dotnet test .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

フォールバック用の`Resonite.GameLibs`パッケージには参照アセンブリのみが含まれるため、CIではエンジン非依存のロジックテストと静的なアセンブリ契約テストを実行します。仮想キーのランタイム契約テストも実行するには、インストール済みResoniteのパスを指定します。

### `rml_mods`へのコピー

```powershell
dotnet build .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:CopyToMods=true -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

### ホットリロード

`EnableHotReloadLibs=true`でビルドし、`ResoniteHotReloadLib`が`rml_libs`にある場合は、初回の`rml_mods`読み込み時に`Japanese IME Bridge`を登録します。ホットリロード対象のDLLは`rml_mods\HotReloadMods`から読み込みます。

```powershell
dotnet build .\ResoniteJapaneseImeBridgeMod.slnx -c Release -p:EnableHotReloadLibs=true -p:CopyToMods=true -p:ResonitePath="C:\Program Files (x86)\Steam\steamapps\common\Resonite"
```

## バージョンとリリース

リリースバージョンは`vX.Y.Z`タグからMinVerが決定します。最初の公開リリースは`v0.1.0`です。このタグをpushすると、ビルド、テスト、GitHub Releaseワークフローが実行されます。

## ライセンス

このModは[MIT License](./LICENSE)で公開しています。
