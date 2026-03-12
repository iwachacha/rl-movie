# RL Movie — プロジェクト構成ガイド

## ディレクトリ全体図

```
c:\rl-movie\
├── .agents/                    # AI エージェント設定
│   ├── RULES.md               # AI ルール（必読）
│   └── workflows/
│       └── create-scenario.md  # シナリオ作成ワークフロー
│
├── AI-RL-Movie/                # Unity プロジェクトルート
│   ├── Assets/
│   │   ├── _RLMovie/           # ★ メインの作業フォルダ
│   │   │   ├── Common/         # 全シナリオ共通のコード・リソース
│   │   │   │   ├── Scripts/    # BaseRLAgent, EnvironmentManager 等
│   │   │   │   ├── Editor/     # BuildForColab, ImportTrainedModel
│   │   │   │   ├── Materials/  # 共通マテリアル
│   │   │   │   ├── Shaders/    # 共通シェーダー
│   │   │   │   └── Prefabs/    # 共通プレハブ
│   │   │   │
│   │   │   ├── Environments/   # ★ シナリオ格納フォルダ
│   │   │   │   ├── _Template/  # ゴールデンテンプレート（コピー元）
│   │   │   │   │   ├── Config/     # template_config.yaml
│   │   │   │   │   ├── Scenes/     # テンプレートシーン
│   │   │   │   │   ├── Scripts/    # テンプレートスクリプト
│   │   │   │   │   └── Prefabs/    # テンプレートプレハブ
│   │   │   │   │
│   │   │   │   └── RollerBall/ # サンプルシナリオ（参考実装）
│   │   │   │       ├── Config/     # roller_ball_config.yaml
│   │   │   │       ├── Scenes/     # RollerBall.unity
│   │   │   │       ├── Scripts/    # RollerBallAgent.cs 等
│   │   │   │       ├── Materials/
│   │   │   │       └── Editor/     # RollerBallSceneBuilder.cs
│   │   │   │
│   │   │   └── Recording/     # Unity Recorder 設定
│   │   │       └── RecorderSettings/
│   │   │
│   │   ├── ThirdParty/         # Asset Store アセット置き場
│   │   │   └── README.md       # アセット一覧（登録必須）
│   │   │
│   │   ├── ML-Agents/          # ML-Agents サンプル（編集禁止）
│   │   ├── StreamingAssets/    # 学習済み .onnx モデル保存先
│   │   ├── Scenes/             # Unity デフォルトシーン
│   │   ├── Settings/           # URP 等の設定
│   │   └── Resources/          # Unity リソース
│   │
│   ├── Packages/               # パッケージ管理
│   │   └── manifest.json       # パッケージ定義（編集注意）
│   ├── ProjectSettings/        # Unity プロジェクト設定（原則触らない）
│   └── ColabBuilds/            # ビルド出力（Git 除外）
│       └── _ReadyToUpload/     # Google Drive アップロード用 ZIP
│
├── Notebooks/                  # Google Colab ノートブック
│   └── rl_movie_training.ipynb # トレーニング実行ノートブック
│
├── docs/                       # プロジェクトドキュメント
│   ├── PROJECT_STRUCTURE.md    # ← このファイル
│   ├── TEMPLATE_GUIDE.md
│   ├── PIPELINE_OVERVIEW.md
│   ├── ASSET_CATALOG.md
│   ├── CODING_CONVENTIONS.md
│   └── TROUBLESHOOTING.md
│
└── .gitignore
```

---

## 各フォルダの役割

### `_RLMovie/Common/` — 共通基盤

全シナリオで共有されるスクリプト・リソース。変更時は全シナリオへの影響を確認すること。

| ファイル | 役割 |
|---------|------|
| `Scripts/BaseRLAgent.cs` | 全エージェントの基底クラス（統計、ビジュアルフィードバック、デバッグ GUI） |
| `Scripts/EnvironmentManager.cs` | 環境管理（ランダム配置、境界判定、Gizmo 可視化） |
| `Scripts/TrainingVisualizer.cs` | リアルタイム報酬グラフ UI |
| `Scripts/RecordingHelper.cs` | 録画時カメラ切替・UI 制御 |
| `Editor/BuildForColab.cs` | メニュー: `RLMovie > Build for Colab` |
| `Editor/ImportTrainedModel.cs` | メニュー: `RLMovie > Import Trained Model` |

### `_RLMovie/Environments/` — シナリオ群

各シナリオは独立したサブフォルダとして管理される。`_Template` はコピー元のゴールデンテンプレート。

### `_RLMovie/Recording/` — 録画設定

Unity Recorder の設定ファイル（`.asset`）を保存する場所。

### `ThirdParty/` — 外部アセット

Asset Store 等からダウンロードしたアセットの格納先。直接編集禁止。

### `Notebooks/` — Colab ノートブック

Google Colab 上で実行するトレーニングノートブック。パラメータ `SCENARIO_NAME` を変更するだけで任意のシナリオを学習可能。

### `ColabBuilds/` — ビルド出力

`BuildForColab.cs` が生成するLinux ビルドと ZIP ファイルの出力先。`.gitignore` で除外済み。

---

## 新規ファイル追加時のルール

1. **シナリオ固有のファイル** → `Environments/<シナリオ名>/` の該当するサブフォルダに配置
2. **全シナリオ共通のファイル** → `Common/` の該当するサブフォルダに配置
3. **外部アセット** → `ThirdParty/<アセット名>/` に配置し README を更新
4. **ドキュメント** → `docs/` に配置
5. **ワークフロー** → `.agents/workflows/` に配置
6. 上記以外の場所にファイルを追加する場合は、ユーザーに確認する
