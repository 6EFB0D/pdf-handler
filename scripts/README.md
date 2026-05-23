# scripts・医Ο繝ｼ繧ｫ繝ｫ髢狗匱繝ｻ繝ｪ繝ｪ繝ｼ繧ｹ陬懷勧・・
| 繝輔ぃ繧､繝ｫ | 蠖ｹ蜑ｲ |
|----------|------|
| `build-release.ps1` | `dotnet publish` 縺ｧ `artifacts/release/{dev\|prod}/PdfHandler-<Version>-win-x64/` 繧堤函謌舌Ａ-BuildNumber`・医∪縺溘・ `PDFHANDLER_BUILD_NUMBER` / `GITHUB_RUN_NUMBER`・峨〒繝薙Ν繝臥分蜿ｷ繧剃ｻ倅ｸ弱・*繧､繝ｳ繧ｹ繝医・繝ｩ縺ｮ蜑阪↓螳溯｡後・* |
| `Secrets.local.ps1` | 繝ｭ繝ｼ繧ｫ繝ｫ縺縺代・迺ｰ蠅・､画焚・・*繧ｳ繝溘ャ繝育ｦ∵ｭ｢**・峨ゅユ繝ｳ繝励Ξ繝ｼ繝医・ `Secrets.local.ps1.example`縲・|
| `Secrets.local.ps1.example` | `Secrets.local.ps1` 縺ｮ髮帛ｽ｢・医さ繝溘ャ繝亥庄・峨・|
| `run-prod.ps1` | `Secrets.local.ps1` 繧定ｪｭ縺ｿ霎ｼ繧薙□縺・∴縺ｧ `dotnet run` 縺励￣ROD 逶ｸ蠖薙・謗･邯壹〒襍ｷ蜍慕｢ｺ隱阪☆繧九・|

隧ｳ邏ｰ縺ｪ謇矩・ｸ隕ｧ縺ｯ [docs/handover/exe-installer-handover.md](../docs/handover/exe-installer-handover.md) 繧貞盾辣ｧ縲・
繧､繝ｳ繧ｹ繝医・繝ｩ逕滓・縺ｯ繝ｪ繝昴ず繝医Μ繝ｫ繝ｼ繝医・ **`tools/build-release.ps1`**・・nno Setup・峨〒縺吶・
## 繝薙Ν繝牙床蟶ｳ・・anifest・・
`build-release.ps1` 螳溯｡悟ｾ後∽ｻ･荳九↓ 1 陦・1 繝薙Ν繝峨・ JSON 縺瑚ｿｽ險倥＆繧後∪縺吶・
- `artifacts/build-manifest/build-manifest.jsonl`

菫晏ｭ倥＆繧後ｋ鬆・岼・域栢邊具ｼ・

- `target_environment` / `version` / `build_number`
- `informational_version`・井ｾ・ `1.1.1+build.42`・・- `git_commit`
- `PdfHandler.UI.exe` / `PdfHandler.runtime.json` / `README_RELEASE.txt` 縺ｮ SHA-256
