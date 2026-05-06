# HMACハイブリッドライセンス管理 実装ガイド

PDF Handler 向けに HMAC ハイブリッド方式を適用した実装ガイドです。

**関連仕様**: [license-code-specification.md](license-code-specification.md)（ライセンスキー形式・4桁区切り表示）

---

## 設計概要

### ライセンス認証の仕組み

```
購入者
  ↓ ライセンスキー発行
Supabase（licenses テーブル）
  ↓ 起動時オンライン検証（5秒タイムアウト）
アプリ → 成功：キャッシュ保存して起動
         失敗：HMACオフライン検証 → 7日以内なら起動
                                   → 期限切れなら拒否

撤退時：最終パッチでオフライン永続化
```

### 選定理由

| 検証方法 | Supabase必要 | 不正防止 | 撤退しやすさ |
|---|---|---|---|
| Supabaseのみ | ✅ 必要 | ✅ 強い | ❌ 撤退困難 |
| HMACローカルのみ | ❌ 不要 | △ 中程度 | ✅ |
| **HMACハイブリッド（本設計）** | 起動時だけ | ✅ 実用的 | ✅✅ |

---

## Step 1 ｜ Supabase テーブル設計

```
以下のSupabaseテーブルを作成してください。

テーブル名: licenses

カラム:
- id: uuid, primary key, default gen_random_uuid()
- license_key: text, unique, not null
- user_email: text
- app_id: text, not null
- max_devices: integer, default 2
- activated_devices: jsonb, default '[]'
- is_active: boolean, default true
- created_at: timestamptz, default now()
- revoked_at: timestamptz, nullable

RLSは有効にして、サービスロールキーからのみ操作可能にしてください。
```

---

## Step 2 ｜ ライセンスキー発行ロジック（サーバー側）

### PDF Handler 固有の仕様

1. **ライセンスキーの形式**（[license-code-specification.md](license-code-specification.md) 準拠）:
   ```
   PDFH-<形態4文字>-<シリアル部28文字>-<HMAC署名>
   例: PDFH-P101-A1B2C3D4E5F6G7H8I9J0K1L2M3-1A2B3C4D
   ```

2. **HMAC署名の生成**:
   - アルゴリズム: SHA-256
   - 署名対象: `PDFH:P101:{シリアル部}`（形態コードは実態に応じて P101/S100/S200 等）
   - 秘密鍵: 環境変数 `LICENSE_SECRET_KEY` から取得

3. **表示形式（ユーザーへの出力）**:
   - メール・UI・コピペで同一フォーマット: 4桁区切り
   - 例: `PDFH-P101-A1B2-C3D4-E5F6-G7H8-I9J0-K1L2-M3`

4. **関数一覧**:
   - `generateLicenseKey()`: ライセンスキーを生成して Supabase に保存
   - `verifyLicenseKeyOffline(licenseKey)`: HMAC署名のみでローカル検証（Supabase不要）
   - `verifyLicenseKeyOnline(licenseKey, deviceId)`: Supabaseで台数・有効性を確認

5. **既存スキーマ**: Supabase `licenses` テーブルは既存を維持（`app_id` は `PDFH` 固定）

---

## Step 3 ｜ アプリ側のライセンス認証フロー

```
以下の仕様でアプリ側のライセンス認証を実装してください。

仕様:
1. 起動時の認証フロー:
   - ローカルストレージからライセンスキーとキャッシュを取得
   - Supabaseにオンライン検証を試みる（タイムアウト: 5秒）
   - 成功 → 結果をローカルにキャッシュして起動許可
   - 失敗（タイムアウト・接続不可）→ HMACオフライン検証にフォールバック
   - オフライン検証も失敗 → 起動拒否

2. キャッシュの仕様:
   - キャッシュキー: license_cache
   - 保存内容: { isValid: boolean, cachedAt: timestamp }
   - 有効期限: 7日間（Supabaseに繋がらなくても7日は使える）

3. デバイスIDの生成:
   - ブラウザの場合: fingerprintjsなどで生成してlocalStorageに保存
   - 同じデバイスでは常に同じIDになること

4. エラーメッセージ:
   - 無効なキー: "ライセンスキーが無効です"
   - 台数オーバー: "登録可能なデバイス数を超えています"
   - オフライン時: "オフラインモードで動作中（残り〇日）"
```

---

## Step 4 ｜ 撤退時の最終パッチ（将来用）

```
事業撤退時に配布する最終パッチを実装してください。

仕様:
- verifyLicenseKeyOnline() を完全に無効化する
- 常に verifyLicenseKeyOffline() のみで認証する
- キャッシュの有効期限を無期限に変更する
- 起動時に「サービス終了のお知らせ」ダイアログを1回だけ表示する
```

---

## 段階的撤退フロー

```
1. 販売停止（新規購入のみ停止）
      ↓
2. ユーザーに告知（3〜6ヶ月前）
      ↓
3. 最終パッチ配布（Step 4 を実装・配布）
      ↓
4. Supabase停止
      ↓
5. 完全撤退
```

---

## 注意事項

| 項目 | 内容 |
|---|---|
| 秘密鍵の管理 | アプリに埋め込むためリバースエンジニアリングで抽出される可能性がある |
| 台数制限 | ローカル検証だけでは台数管理不可 → Supabaseで補う |
| 完璧ではない | 個人開発レベルでは十分な抑止力になる |

---

*プラットフォームが Electron / モバイル の場合は、Step 3 のローカルストレージ部分を適宜変更すること。*
