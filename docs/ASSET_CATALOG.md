# RL Movie — アセットカタログ

## 概要

このドキュメントは、プロジェクトで使用する全アセットを管理するカタログです。
Asset Store や外部ソースからダウンロードしたアセットを登録し、ライセンスと使用状況を追跡します。

---

## アセット管理ルール

1. **配置先**: すべての外部アセットは `Assets/ThirdParty/<アセット名>/` に配置
2. **登録**: 新規アセット追加時は**このファイル**と `Assets/ThirdParty/README.md` の両方に登録
3. **ライセンス**: 無料 & 商用利用可能なもののみ使用
4. **直接編集禁止**: ThirdParty 内のファイルは直接編集しない。カスタマイズ時はコピーする
5. **プレファブ化**: シナリオで使用する場合は、シナリオの `Prefabs/` に Prefab Variant を作成

---

## アセット一覧

### 🏗️ 3D モデル / 環境

| アセット名 | ソース | ライセンス | 用途 | 配置パス | 登録日 |
|-----------|--------|-----------|------|---------|-------|
| *(テンプレート)* | Asset Store | Unity EULA | ― | `ThirdParty/<名前>/` | YYYY-MM-DD |
| Polytope Studio | Asset Store | Unity EULA | 環境モデル | `ThirdParty/Polytope Studio/` | 2026-03-13 |
| StylizedCharacter | Asset Store | Unity EULA | キャラクター | `ThirdParty/StylizedCharacter/` | 2026-03-13 |
| ithappy | Asset Store | Unity EULA | 動物・キャラクター | `ThirdParty/ithappy/` | 2026-03-13 |
| DoubleL | Asset Store | Unity EULA | アニメーション・モデル | `ThirdParty/DoubleL/` | 2026-03-13 |
| _Tanks | Asset Store | Unity EULA | サンプルプロジェクト | `ThirdParty/_Tanks/` | 2026-03-13 |

### 🎨 マテリアル / テクスチャ

| アセット名 | ソース | ライセンス | 用途 | 配置パス | 登録日 |
|-----------|--------|-----------|------|---------|-------|
| *(テンプレート)* | Asset Store | Unity EULA | ― | `ThirdParty/<名前>/` | YYYY-MM-DD |
| BOXOPHOBIC | Asset Store | Unity EULA | Skybox | `ThirdParty/BOXOPHOBIC/` | 2026-03-13 |

### ✨ パーティクル / エフェクト

| アセット名 | ソース | ライセンス | 用途 | 配置パス | 登録日 |
|-----------|--------|-----------|------|---------|-------|
| *(テンプレート)* | Asset Store | Unity EULA | ― | `ThirdParty/<名前>/` | YYYY-MM-DD |
| JMO Assets | Asset Store | Unity EULA | エフェクト (Cartoon FX) | `ThirdParty/JMO Assets/` | 2026-03-13 |

### 🖼️ UI / HUD

| アセット名 | ソース | ライセンス | 用途 | 配置パス | 登録日 |
|-----------|--------|-----------|------|---------|-------|
| *(テンプレート)* | Asset Store | Unity EULA | ― | `ThirdParty/<名前>/` | YYYY-MM-DD |

### 🔊 オーディオ / BGM

| アセット名 | ソース | ライセンス | 用途 | 配置パス | 登録日 |
|-----------|--------|-----------|------|---------|-------|
| *(テンプレート)* | Asset Store | Unity EULA | ― | `ThirdParty/<名前>/` | YYYY-MM-DD |

### 🔧 ツール / ユーティリティ

| アセット名 | ソース | ライセンス | 用途 | 配置パス | 登録日 |
|-----------|--------|-----------|------|---------|-------|
| *(テンプレート)* | Asset Store | Unity EULA | ― | `ThirdParty/<名前>/` | YYYY-MM-DD |
| Demigiant | Asset Store | Unity EULA | DOTween (アニメーション) | `ThirdParty/Demigiant/` | 2026-03-13 |
| PrimeTween | Asset Store | Unity EULA | アニメーション | `ThirdParty/PrimeTween/` | 2026-03-13 |

---

## パッケージ（Package Manager 経由）

以下はパッケージマネージャーで管理されているアセットです。`Packages/manifest.json` で定義。

| パッケージ | バージョン/ソース | 用途 |
|-----------|------------------|------|
| `com.unity.ml-agents` | v4.0.0 (git / release_23) | 強化学習コア |
| `com.coplaydev.unity-mcp` | git / main | AI → Unity 直接操作 |
| `com.unity.recorder` | 5.1.1 | 動画録画 |
| `com.unity.cinemachine` | 2.10.3 | カメラワーク |

---

## アセット追加手順

### Asset Store から追加する場合

1. Unity の `Window > Package Manager` を開く
2. `My Assets` からアセットをインポート
3. インポート先を `Assets/ThirdParty/<アセット名>/` に変更
4. インポート完了後、以下を更新:
   - ✅ このファイル（`ASSET_CATALOG.md`）の該当カテゴリに行を追加
   - ✅ `Assets/ThirdParty/README.md` のテーブルに行を追加

### 手動で追加する場合

1. `Assets/ThirdParty/<アセット名>/` フォルダを作成
2. ファイルをコピー
3. 上記と同じドキュメント更新を行う

### シナリオで使用する場合

1. ThirdParty 内のプレファブを**直接シーンに配置しない**
2. シナリオの `Prefabs/` フォルダに **Prefab Variant** を作成
3. Variant 側でカスタマイズ（マテリアル変更、コンポーネント追加等）

---

## アセット棚卸しチェックリスト

定期的に以下を確認:

- [ ] 使っていないアセットが ThirdParty に残っていないか
- [ ] ライセンス情報が最新か
- [ ] このカタログと README.md の情報が一致しているか
- [ ] プロジェクトサイズが無駄に大きくなっていないか
