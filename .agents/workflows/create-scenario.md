---
description: 新しいRLシナリオの作成手順
---

# 新しい RL シナリオ作成ワークフロー

// turbo-all

> 📖 関連ドキュメント:
> - [RULES.md](file:///.agents/RULES.md) — AI ルール（必読）
> - [TEMPLATE_GUIDE.md](file:///docs/TEMPLATE_GUIDE.md) — テンプレート詳細
> - [CODING_CONVENTIONS.md](file:///docs/CODING_CONVENTIONS.md) — コーディング規約
> - [PIPELINE_OVERVIEW.md](file:///docs/PIPELINE_OVERVIEW.md) — パイプライン全体像

---

## 0. 事前チェック

- [ ] シナリオ名が PascalCase になっているか（例: `RollerBall`, `MazeRunner`）
- [ ] 同名のシナリオが `Environments/` に既に存在しないか
- [ ] `.agents/RULES.md` の内容を確認したか

## 1. フォルダ作成

`Assets/_RLMovie/Environments/` 以下に新しいフォルダを作成：

```
_RLMovie/Environments/<シナリオ名>/
├── Scenes/
├── Scripts/
├── Prefabs/
└── Config/
```

> ⚠️ 4 つのサブフォルダは**必須**。省略不可（RULES.md §1.3）。

## 2. エージェントスクリプト作成

`_Template/Scripts/` からテンプレートをコピーするか、以下の構造で新規作成：

```csharp
using RLMovie.Common;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RLMovie.Environments.<シナリオ名>
{
    /// <summary>
    /// シナリオの概要説明
    /// </summary>
    public class <シナリオ名>Agent : BaseRLAgent
    {
        [Header("=== <シナリオ名> Settings ===")]
        // [SerializeField] フィールドには必ず [Tooltip] を付ける

        protected override void OnAgentInitialize() { }
        protected override void OnEpisodeReset() { }
        protected override void CollectAgentObservations(VectorSensor sensor) { }
        protected override void ExecuteActions(ActionBuffers actions) { }
        protected override void ProvideHeuristicInput(in ActionBuffers actionsOut) { }
    }
}
```

**チェックポイント:**
- [ ] `BaseRLAgent` を継承しているか（`Agent` 直接継承は禁止）
- [ ] namespace が `RLMovie.Environments.<シナリオ名>` になっているか
- [ ] `using RLMovie.Common;` があるか
- [ ] クラス名が `<シナリオ名>Agent` になっているか
- [ ] 報酬は `Success()` / `Fail()` / `AddTrackedReward()` を使っているか
- [ ] Observation のコメントに次元数が記載されているか

## 3. ビジュアル演出スクリプト作成（推奨）

動画の見栄えを良くするため、以下のビジュアル演出スクリプトを作成を検討:

- **`FloorVisual.cs`** — 床の色フラッシュ（成功: 緑、失敗: 赤）
- **`TargetVisual.cs`** — ターゲットのパルス/回転/グロー演出
- その他のビジュアルエフェクト

> 参考実装: `Environments/RollerBall/Scripts/FloorVisual.cs`, `TargetVisual.cs`

## 4. YAML 設定作成

`_Template/Config/template_config.yaml` をコピーして以下を変更：

- [ ] `behaviors:` のキーをエージェントの **Behavior Name** に合わせる
- [ ] キー名 = クラス名 = Behavior Name が**3つとも一致**しているか
- [ ] `max_steps` をシナリオの複雑さに応じて調整
- [ ] ファイル名が `<snake_case>_config.yaml` になっているか

## 5. シーン構築

1. 新規シーンを作成、または `_Template/Scenes/` のシーンを複製
2. 地面（Plane）・壁・ゴール等を配置
3. エージェント GameObject に以下をアタッチ:
   - 作成したエージェントスクリプト
   - `Behavior Parameters`（Behavior Name = クラス名）
   - `Decision Requester`（Decision Period = 5 推奨）
4. 空の GameObject に `EnvironmentManager` を追加
5. ビジュアル演出スクリプトを対象オブジェクトにアタッチ
6. Post-Processing Volume でビジュアルを調整
7. `RecordingHelper` 用のカメラ位置プリセットを配置

**チェックポイント:**
- [ ] シーン名がシナリオ名と一致しているか
- [ ] EnvironmentManager が配置されているか
- [ ] すべての SerializeField が Inspector で設定されているか

## 6. Heuristic テスト

1. `Behavior Parameters > Behavior Type` を `Heuristic Only` に設定
2. Play モードで手動操作テスト
3. 確認項目:
   - [ ] エージェントの動きが正しいか
   - [ ] 報酬ログが Console に出力されているか
   - [ ] リセットが正しく動くか
   - [ ] 落下判定 / ゴール判定が正しいか
4. `TrainingVisualizer` を配置して確認

## 7. Colab ビルド & 学習

1. Unity メニュー **`RLMovie > Build for Colab (Current Scene)`** をクリック
   - 自動で Linux ビルド → YAML コピー → ZIP パッケージング
   - 出力先: `ColabBuilds/_ReadyToUpload/<シナリオ名>.zip`
2. ZIP を Google Drive の `RL-Movie/Builds/` にアップロード
3. `Notebooks/rl_movie_training.ipynb` を Colab で開く
4. Step 0 の `SCENARIO_NAME` を変更してセルを上から順に実行

## 8. モデル適用 & 録画

1. Google Drive の `RL-Movie/Models/` から .onnx をダウンロード
2. Unity メニュー **`RLMovie > Import Trained Model`** をクリック
   - StreamingAssets にコピー + Behavior Type を自動切替
3. Inspector で Behavior Parameters > Model にファイルをドラッグ
4. Play して動作確認
5. Unity Recorder で録画
   - `RecordingHelper` の `hideUIWhenRecording = true` を設定
