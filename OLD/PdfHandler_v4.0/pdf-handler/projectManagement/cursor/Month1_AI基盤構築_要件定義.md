# Month 1: AI基盤構築 要件定義

**作成日**: 2025年1月1日  
**対象**: Week 3-4（基本機能と並行）

---

## 1. Claude API統合基盤

### 1.1 概要
Claude API（Anthropic）を統合し、PDFハンドラからAI機能を利用できる基盤を構築する。

### 1.2 機能要件

#### 基本機能
- Claude APIへの接続
- テキスト送信・受信
- エラーハンドリング
- リトライロジック
- タイムアウト処理

#### 技術要件
- Anthropic SDK for .NETの統合
- 非同期処理（async/await）
- HttpClientの適切な使用
- JSON シリアライゼーション

### 1.3 アーキテクチャ設計

```
┌─────────────────┐
│  UI Layer       │
│  (ViewModels)   │
└────────┬────────┘
         │
┌────────▼────────┐
│  Core Layer     │
│  (Interfaces)   │
└────────┬────────┘
         │
┌────────▼────────┐
│ Infrastructure │
│  (Services)    │
│                │
│  - ClaudeClient│
│  - ApiService  │
└────────┬────────┘
         │
┌────────▼────────┐
│  Claude API    │
│  (External)    │
└────────────────┘
```

### 1.4 受け入れ基準
- [ ] Claude APIへの接続が正常に動作する
- [ ] テキスト送信・受信が正常に動作する
- [ ] エラーハンドリングが適切に動作する
- [ ] リトライロジックが正常に動作する
- [ ] タイムアウト処理が適切に動作する

---

## 2. APIキー管理基盤

### 2.1 概要
ユーザーのAPIキーを安全に保存・管理する機能。

### 2.2 機能要件

#### 基本機能
- APIキーの保存（暗号化）
- APIキーの読み込み（復号化）
- APIキーの削除
- APIキーの検証（接続テスト）

#### セキュリティ要件
- Windows Data Protection API (DPAPI) を使用
- ユーザープロファイルで暗号化
- 他のユーザーやアプリから読み取り不可
- APIキーの平文表示を避ける

#### UI要件
- 設定画面の実装
- APIキー入力フィールド
- APIキー接続テストボタン
- APIキー表示/非表示トグル

### 2.3 技術的設計

```csharp
// セキュアストレージの実装例
public class SecureKeyStorage
{
    public void SaveApiKey(string keyName, string apiKey)
    {
        // Windows DPAPIで暗号化
        byte[] encryptedData = ProtectedData.Protect(
            Encoding.UTF8.GetBytes(apiKey),
            null,
            DataProtectionScope.CurrentUser
        );
        // レジストリまたはローカルファイルに保存
    }
    
    public string LoadApiKey(string keyName)
    {
        // 暗号化データを読み込み
        byte[] encryptedData = LoadEncryptedData(keyName);
        // DPAPIで復号化
        byte[] decryptedData = ProtectedData.Unprotect(
            encryptedData,
            null,
            DataProtectionScope.CurrentUser
        );
        return Encoding.UTF8.GetString(decryptedData);
    }
}
```

### 2.4 受け入れ基準
- [ ] APIキーが安全に保存される（暗号化）
- [ ] APIキーが正しく読み込まれる（復号化）
- [ ] APIキーが他のユーザーから読み取れない
- [ ] APIキーの接続テストが正常に動作する
- [ ] 設定UIが適切に動作する

---

## 3. エラーハンドリング

### 3.1 エラー種別

#### API接続エラー
- ネットワークエラー
- タイムアウトエラー
- 認証エラー（無効なAPIキー）
- レート制限エラー

#### データ処理エラー
- JSON解析エラー
- データ形式エラー
- サイズ制限エラー

### 3.2 エラーハンドリング戦略

#### リトライロジック
- 一時的なエラー（ネットワークエラー等）は自動リトライ
- 最大リトライ回数: 3回
- 指数バックオフ（1秒、2秒、4秒）

#### エラーメッセージ
- ユーザーフレンドリーなエラーメッセージ
- 技術的詳細はログに記録
- エラーの種類に応じた適切なメッセージ

### 3.3 受け入れ基準
- [ ] ネットワークエラーが適切に処理される
- [ ] 認証エラーが適切に処理される
- [ ] リトライロジックが正常に動作する
- [ ] エラーメッセージが適切に表示される

---

## 4. トークン数管理（将来拡張）

### 4.1 概要
API使用量を追跡し、トークン数を管理する機能（Month 2で実装予定）。

### 4.2 基本設計
- リクエストごとのトークン数カウント
- 使用量の追跡（日次、月次）
- 使用量の表示（UI）

---

## 5. 実装優先順位

1. **高優先度**: Claude API統合基盤
2. **高優先度**: APIキー管理基盤
3. **中優先度**: エラーハンドリング強化
4. **低優先度**: トークン数管理（Month 2）

---

## 6. 技術的考慮事項

### 6.1 Anthropic SDK
- Anthropic SDK for .NETを使用
- 最新バージョンの確認
- NuGetパッケージの追加

### 6.2 非同期処理
- async/awaitパターンの使用
- キャンセレーショントークンの対応
- 進捗報告の実装

### 6.3 セキュリティ
- APIキーの安全な保存
- HTTPS通信の強制
- 機密情報のログ出力禁止

---

## 7. 次のステップ

1. Anthropic SDK for .NETの調査・選定
2. アーキテクチャ設計の詳細化
3. Claude APIクライアントの実装
4. APIキー管理の実装
5. エラーハンドリングの実装
6. 統合テスト

---

## 8. 参考資料

- [Anthropic API Documentation](https://docs.anthropic.com/)
- [Anthropic SDK for .NET](https://github.com/anthropics/anthropic-sdk-dotnet)
- [Windows Data Protection API](https://docs.microsoft.com/dotnet/api/system.security.cryptography.protecteddata)

