# PDFハンドラ ライセンス・クレジット情報

**作成日**: 2024年12月27日  
**バージョン**: 3.4.2

---

## ⚠️ 重要: 商用利用についての注意事項

このアプリケーションには**商用利用に制限のあるライブラリ**が含まれています。

---

## 📋 使用しているライブラリとライセンス

### 1. **iText 7（PDF操作ライブラリ）**

#### ライセンス
- **AGPL v3.0**（オープンソース版）
- または **商用ライセンス**（有償）

#### 制限事項
**❌ AGPL v3.0での商用利用は事実上不可能です**

理由：
- AGPL v3.0は「コピーレフト」ライセンス
- このライブラリを使ったソフトウェアも**すべてAGPL v3.0で公開**する必要がある
- ソースコードの完全公開が必須
- ネットワーク経由で使用する場合も公開義務あり

#### 商用利用する場合
**商用ライセンスの購入が必須**

**価格（2024年時点の参考）:**
- 個人/スタートアップ: 約 $1,500/年
- 企業ライセンス: 約 $5,000～/年
- プロジェクトベース: 要問合せ

**購入先:**
- 公式サイト: https://itextpdf.com/
- 日本代理店: 株式会社ワールドリンク等

#### 現在の使用箇所
```
PdfHandler.Infrastructure/Services/
├─ PdfMergeService.cs   ← PDF結合
└─ PdfSplitService.cs   ← PDF分割
```

---

### 2. **Docnet.Core（PDFレンダリング）**

#### ライセンス
- **MIT License**

#### 商用利用
**✅ 完全に自由に商用利用可能**

条件：
- ライセンス表記が必要
- 無償・有償問わず利用可能

#### クレジット表記例
```
This application uses Docnet.Core
https://github.com/GowenGit/docnet
Licensed under MIT License
```

---

### 3. **CommunityToolkit.Mvvm**

#### ライセンス
- **MIT License**

#### 商用利用
**✅ 完全に自由に商用利用可能**

条件：
- ライセンス表記が必要

---

### 4. **.NET / WPF**

#### ライセンス
- **MIT License**

#### 商用利用
**✅ 完全に自由に商用利用可能**

---

## 🚨 結論: 現状での商用利用

### 現在の構成では

**❌ 無償での商用利用: 不可**
**❌ 有償での商用利用: 不可（iTextの商用ライセンス購入が必須）**

### 理由

iText 7が**AGPL v3.0**であるため：
- ソースコードを完全公開しない限り使用不可
- 社内利用でもネットワーク経由の場合は公開義務
- 顧客に配布する場合も公開義務

---

## 💡 商用利用を可能にする方法

### 方法1: iTextの商用ライセンスを購入（推奨）

#### 手順
1. **iText社に問い合わせ**
   - 公式サイト: https://itextpdf.com/
   - 日本代理店に相談

2. **見積もり取得**
   - 使用目的を説明
   - 配布予定数を伝える

3. **ライセンス購入**
   - 年間ライセンス: 約 $1,500～
   - 永続ライセンス: 要問合せ

4. **ライセンスキーの実装**
   - コードにライセンスキーを設定
   - 配布可能に

#### メリット
- 既存コードをそのまま使える
- 安定した動作
- サポートあり

#### デメリット
- ライセンス費用が必要
- 年間更新が必要な場合も

---

### 方法2: iTextを別のライブラリに置き換え

#### 代替ライブラリの選択肢

##### A. **PdfSharp / MigraDoc**
- **ライセンス**: MIT License
- **商用利用**: ✅ 無料で可能
- **機能**: PDF作成、結合、分割
- **欠点**: iTextより機能が少ない

##### B. **QuestPDF**
- **ライセンス**: MIT License (Community版)
- **商用利用**: Community版は制限あり、Professionalは有償
- **機能**: PDF作成、レイアウト
- **欠点**: 結合・分割機能は限定的

##### C. **PdfPig**
- **ライセンス**: Apache 2.0
- **商用利用**: ✅ 完全に自由
- **機能**: PDF読み取り、テキスト抽出
- **欠点**: PDF編集機能は弱い

##### D. **Aspose.PDF**
- **ライセンス**: 商用（有償）
- **価格**: 約 $1,000～/年
- **機能**: 非常に豊富
- **欠点**: iTextと同様に有償

#### 推奨: PdfSharp + MigraDoc

**理由:**
- 完全無料
- 商用利用可能
- MIT License（制限が少ない）
- PDF結合・分割が可能

**実装工数:**
- 約2-3日（PdfMergeService、PdfSplitServiceの書き換え）

---

### 方法3: AGPL v3.0で完全公開

#### 条件
- ソースコードを完全公開（GitHub等）
- AGPL v3.0ライセンスで配布
- 改変版も公開義務

#### メリット
- ライセンス費用不要
- オープンソースコミュニティに貢献

#### デメリット
- ソースコード公開必須
- 商用利用しにくい
- 競合に利用される可能性

---

## 📝 必要なクレジット表記

### 現在使用中のライブラリ

以下をREADME.txt、アプリのAbout画面、またはLICENSE.txtに記載する必要があります：

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
　使用ライブラリとライセンス
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

このソフトウェアは以下のオープンソースライブラリを使用しています：

1. iText 7
   ライセンス: AGPL v3.0
   https://itextpdf.com/
   Copyright (C) iText Group NV

   ※商用利用の場合は別途商用ライセンスが必要です

2. Docnet.Core
   ライセンス: MIT License
   https://github.com/GowenGit/docnet
   Copyright (c) 2019 GowenGit

3. CommunityToolkit.Mvvm
   ライセンス: MIT License
   https://github.com/CommunityToolkit/dotnet
   Copyright (c) .NET Foundation and Contributors

4. .NET / WPF
   ライセンス: MIT License
   https://github.com/dotnet
   Copyright (c) .NET Foundation and Contributors

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
　MITライセンス全文
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Permission is hereby granted, free of charge, to any person obtaining 
a copy of this software and associated documentation files (the 
"Software"), to deal in the Software without restriction, including 
without limitation the rights to use, copy, modify, merge, publish, 
distribute, sublicense, and/or sell copies of the Software, and to 
permit persons to whom the Software is furnished to do so, subject 
to the following conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE 
OR OTHER DEALINGS IN THE SOFTWARE.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## 🎯 推奨アクション

### すぐに対応すべきこと

#### 1. **使用目的の確認**

**質問1: 商用利用しますか？**
- [ ] はい → 対応必須
- [ ] いいえ（個人利用のみ） → 現状のまま可

**質問2: 配布しますか？**
- [ ] 社内のみ → グレーゾーン（AGPL解釈次第）
- [ ] 社外・顧客へ配布 → 対応必須
- [ ] 配布しない（自分だけ） → 現状のまま可

**質問3: 有償で提供しますか？**
- [ ] はい → iText商用ライセンス必須
- [ ] いいえ（無償配布） → それでもiText商用ライセンス必要

#### 2. **短期的対応（今すぐ）**

**Option A: クレジット表記を追加**
```
1. LICENSE.txtファイルを作成
2. 上記のクレジット表記を記載
3. 配布パッケージに含める
```

**Option B: 利用規約を明記**
```
README.txtに以下を追加：

【重要】商用利用について
このソフトウェアは個人利用・教育目的での使用を想定しています。
商用利用する場合は、使用ライブラリのライセンスをご確認ください。
```

#### 3. **中長期的対応（1-2週間以内）**

**Option A: iText商用ライセンスを購入**
- 見積もり依頼
- 購入手続き
- ライセンスキー実装

**Option B: PdfSharpに移行**
- PdfMergeService書き換え（1日）
- PdfSplitService書き換え（1日）
- テスト（1日）
- **総工数: 約3日**

---

## 💰 料金設定の考え方（商用利用の場合）

### パターン1: 無償配布（ライセンス費用を吸収）

```
配布形態: 無償
ライセンス費用: 自社負担（年間 $1,500～）
対象: 顧客サービス、マーケティングツール
```

### パターン2: 有償販売

```
販売価格例:
・買い切り版: 5,000円～10,000円
・年間ライセンス: 3,000円/年
・企業向け: 50,000円～（台数無制限）

価格設定の考慮点:
・iTextライセンス費用: $1,500/年
・開発・保守費用
・利益マージン
```

### パターン3: サブスクリプション

```
月額課金:
・個人プラン: 300円/月
・ビジネスプラン: 1,000円/月
・エンタープライズ: 要問合せ

メリット:
・継続的な収益
・ライセンス費用を回収しやすい
```

---

## 📞 次のステップ

### 1. 利用目的の確定
- 個人利用のみ？
- 社内利用？
- 顧客への配布？
- 販売？

### 2. ライセンス戦略の決定

**無償配布の場合:**
→ PdfSharpへの移行を推奨（3日の工数）

**有償販売の場合:**
→ iText商用ライセンス購入を検討

**オープンソース公開の場合:**
→ AGPL v3.0で全公開

### 3. クレジット表記の追加（即座に）
- LICENSE.txtファイル作成
- README.txtに記載

---

## ⚖️ 法的リスクの評価

### リスク: 高
- iTextを商用ライセンスなしで商用利用
- ソースコード非公開でAGPL違反
- **訴訟リスクあり**

### リスク: 中
- 社内利用のみ（グレーゾーン）
- クレジット表記なし

### リスク: 低
- 個人利用のみ
- 適切なクレジット表記

---

**重要**: このドキュメントは一般的な情報提供です。
正式な法的アドバイスが必要な場合は、弁護士にご相談ください。

---

最終更新: 2024年12月27日
