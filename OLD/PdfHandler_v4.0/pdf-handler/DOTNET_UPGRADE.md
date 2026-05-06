# .NET バージョンアップガイド

## ⚠️ 警告メッセージについて

```
ターゲット フレームワーク 'net6.0-windows' はサポートされていません。
今後、セキュリティ更新プログラムを受け取ることはありません。
```

## 📅 .NET サポート状況

### .NET 6.0（現在のバージョン）
- **リリース**: 2021年11月
- **サポート終了**: 2024年11月12日 ⚠️
- **ステータス**: **サポート終了済み**
- **影響**: セキュリティパッチが提供されなくなる

### .NET 8.0（LTS推奨）
- **リリース**: 2023年11月
- **サポート終了**: 2026年11月10日
- **ステータス**: **現在サポート中（LTS）**
- **推奨**: ✅ 本番環境での使用に推奨

### .NET 9.0（最新）
- **リリース**: 2024年11月
- **サポート終了**: 2026年5月（STS）
- **ステータス**: **現在サポート中**
- **注意**: LTSではない（短期サポート）

## 🎯 推奨対応

### 選択肢1: .NET 8.0へアップグレード（推奨） ✅

**理由:**
- ✅ 長期サポート（LTS）
- ✅ 2026年11月まで完全サポート
- ✅ 安定性が高い
- ✅ 本番環境に最適

### 選択肢2: .NET 9.0へアップグレード

**理由:**
- ✅ 最新機能
- ⚠️ 短期サポート（2026年5月まで）
- 検証環境や新規プロジェクト向け

### 選択肢3: そのまま使用（非推奨）

**条件:**
- ⚠️ セキュリティリスクを承知の上
- ⚠️ インターネットに公開しない
- ⚠️ 個人的な使用のみ

## 🔧 .NET 8.0へのアップグレード手順

### 手順1: .NET 8.0 SDKのインストール

#### Windows
1. [.NET 8.0ダウンロードページ](https://dotnet.microsoft.com/download/dotnet/8.0)にアクセス
2. **SDK 8.0.x**をダウンロード
3. インストーラーを実行
4. インストール完了

#### 確認
```bash
dotnet --list-sdks
# 8.0.xxx が表示されることを確認
```

### 手順2: プロジェクトファイルの更新

以下の3つのcsprojファイルを更新します。

#### 1. PdfHandler.Core.csproj
```xml
<!-- 変更前 -->
<TargetFramework>net6.0</TargetFramework>

<!-- 変更後 -->
<TargetFramework>net8.0</TargetFramework>
```

#### 2. PdfHandler.Infrastructure.csproj
```xml
<!-- 変更前 -->
<TargetFramework>net6.0</TargetFramework>

<!-- 変更後 -->
<TargetFramework>net8.0</TargetFramework>
```

#### 3. PdfHandler.UI.csproj
```xml
<!-- 変更前 -->
<TargetFramework>net6.0-windows</TargetFramework>

<!-- 変更後 -->
<TargetFramework>net8.0-windows</TargetFramework>
```

### 手順3: NuGetパッケージの更新（オプション）

最新の安定版に更新することを推奨します。

```bash
# プロジェクトディレクトリで実行
dotnet list package --outdated

# 更新が必要なパッケージを更新
dotnet add package CommunityToolkit.Mvvm --version 8.3.2
dotnet add package itext7 --version 8.0.5
# 他のパッケージも同様に
```

### 手順4: ビルドとテスト

```bash
# クリーンビルド
dotnet clean
dotnet restore
dotnet build

# 実行テスト
cd src/PdfHandler.UI
dotnet run
```

## 📝 自動アップグレードスクリプト

プロジェクトルートで以下を実行:

```bash
# Linuxの場合
sed -i 's/net6.0/net8.0/g' src/PdfHandler.Core/PdfHandler.Core.csproj
sed -i 's/net6.0/net8.0/g' src/PdfHandler.Infrastructure/PdfHandler.Infrastructure.csproj
sed -i 's/net6.0-windows/net8.0-windows/g' src/PdfHandler.UI/PdfHandler.UI.csproj

# Windows PowerShellの場合
(Get-Content src/PdfHandler.Core/PdfHandler.Core.csproj) -replace 'net6.0', 'net8.0' | Set-Content src/PdfHandler.Core/PdfHandler.Core.csproj
(Get-Content src/PdfHandler.Infrastructure/PdfHandler.Infrastructure.csproj) -replace 'net6.0', 'net8.0' | Set-Content src/PdfHandler.Infrastructure/PdfHandler.Infrastructure.csproj
(Get-Content src/PdfHandler.UI/PdfHandler.UI.csproj) -replace 'net6.0-windows', 'net8.0-windows' | Set-Content src/PdfHandler.UI/PdfHandler.UI.csproj
```

## ✅ アップグレード後の確認事項

### 1. ビルドの確認
```bash
dotnet build
# → Build succeeded. 0 Warning(s) 0 Error(s)
```

### 2. 警告メッセージの消失
- Visual Studioで再度slnを開く
- ⚠️ 警告メッセージが表示されないことを確認

### 3. 機能テスト
- [ ] アプリケーションが起動する
- [ ] フォルダが開ける
- [ ] PDFファイルが表示される
- [ ] F2でファイル名変更ができる
- [ ] PDF結合が動作する
- [ ] PDF分割が動作する

## 🔍 互換性について

### .NET 6.0 → .NET 8.0

**基本的に互換性があります:**
- ✅ このプロジェクトで使用しているすべてのライブラリは.NET 8.0対応
- ✅ コード変更は不要
- ✅ WPFアプリケーションは完全互換
- ✅ NuGetパッケージは自動的に適切なバージョンを使用

**注意点:**
- `CommunityToolkit.Mvvm` 8.2.2 → 8.3.2（最新）を推奨
- `itext7` 8.0.2 → 8.0.5（最新）を推奨

## 📊 使用しているNuGetパッケージの対応状況

| パッケージ | 現在 | 最新 | .NET 8対応 |
|-----------|------|------|-----------|
| CommunityToolkit.Mvvm | 8.2.2 | 8.3.2 | ✅ |
| itext7 | 8.0.2 | 8.0.5 | ✅ |
| PdfSharpCore | 1.3.65 | 1.3.65 | ✅ |
| System.Drawing.Common | 8.0.0 | 9.0.0 | ✅ |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | 9.0.0 | ✅ |

## ⚡ クイックアップグレード（推奨手順）

### Windows（Visual Studio使用）

1. **.NET 8.0 SDKをインストール**
   - https://dotnet.microsoft.com/download/dotnet/8.0

2. **プロジェクトファイルを一括置換**
   - Visual Studioで「編集」→「検索と置換」→「フォルダーを指定して置換」
   - 検索: `net6.0`
   - 置換: `net8.0`
   - 対象: `*.csproj`
   - 「すべて置換」をクリック

3. **ソリューションを再読み込み**
   - ソリューションを閉じる
   - 再度開く

4. **ビルド**
   - `Ctrl + Shift + B`

5. **完了！**

## 🎓 詳細情報

### 公式ドキュメント
- [.NET サポートポリシー](https://aka.ms/dotnet-core-support)
- [.NET 8.0 新機能](https://learn.microsoft.com/ja-jp/dotnet/core/whats-new/dotnet-8/overview)
- [.NET アップグレードガイド](https://learn.microsoft.com/ja-jp/dotnet/core/porting/)

### サポートスケジュール
```
.NET 6.0: ━━━━━━━━━━━━━━━━━●─────────────
                    2024/11/12（終了）

.NET 8.0: ─────────━━━━━━━━━━━━━━━━━━━━━━━●
          2023/11      今         2026/11/10

.NET 9.0: ───────────────━━━━━━━━━━━━━●────
                 2024/11    今   2026/5（短期）
```

## 💡 推奨アクション

### 今すぐ実行すべきこと

1. ✅ **.NET 8.0 SDKをインストール**（5分）
2. ✅ **プロジェクトファイルを更新**（1分）
3. ✅ **ビルドして確認**（2分）

**合計所要時間: 約10分**

### 長期的な対応

- 🔄 定期的に.NET SDKを最新LTSに更新
- 🔄 NuGetパッケージを定期的に更新
- 📅 次回: .NET 10.0 LTS（2025年11月リリース予定）

## ❓ FAQ

### Q: 今すぐアップグレードしないとどうなる？
**A:** 
- セキュリティパッチが提供されない
- 新しいバグ修正が受けられない
- 将来的にライブラリの互換性問題が発生する可能性

### Q: .NET 6.0のまま使い続けても大丈夫？
**A:** 
- ⚠️ 非推奨ですが、以下の条件なら問題は少ない:
  - インターネットに公開しない
  - 個人的な使用のみ
  - セキュリティリスクを理解している

### Q: アップグレードで何か壊れる可能性は？
**A:** 
- このプロジェクトでは**ほぼ問題なし**
- すべてのライブラリが.NET 8.0対応済み
- WPFは完全に互換性あり

### Q: .NET 9.0にすべき？
**A:** 
- 本番環境: ❌ .NET 8.0（LTS）を推奨
- 学習・実験: ✅ .NET 9.0でもOK

---

**推奨**: 今すぐ.NET 8.0にアップグレード！  
**所要時間**: 約10分  
**難易度**: ★☆☆☆☆（簡単）  
**リスク**: ほぼゼロ

---

**最終更新**: 2024年12月26日  
**バージョン**: 1.0
