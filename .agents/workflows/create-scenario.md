---
description: 新しいRLシナリオの作成手順
---

# 新しい RL シナリオ作成ワークフロー

// turbo-all

## 1. フォルダ作成

`Assets/_RLMovie/Environments/` 以下に新しいフォルダを作成：

```
_RLMovie/Environments/<シナリオ名>/
├── Scenes/
├── Scripts/
├── Prefabs/
└── Config/
```

## 2. エージェントスクリプト作成

`_Template/Scripts/` からテンプレートをコピーするか、以下の構造で新規作成：

```csharp
using RLMovie.Common;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class MyAgent : BaseRLAgent
{
    protected override void OnAgentInitialize() { }
    protected override void OnEpisodeReset() { }
    protected override void CollectAgentObservations(VectorSensor sensor) { }
    protected override void ExecuteActions(ActionBuffers actions) { }
    protected override void ProvideHeuristicInput(in ActionBuffers actionsOut) { }
}
```

## 3. YAML 設定作成

`_Template/Config/template_config.yaml` をコピーして以下を変更：
- `behaviors:` のキーをエージェントの Behavior Name に合わせる
- `max_steps` をシナリオの複雑さに応じて調整

## 4. シーン構築

1. `_Template/Scenes/` のシーンを複製するか、新規シーン作成
2. 地面（Plane）・壁・ゴール等を配置
3. エージェント GameObject に `Behavior Parameters` と `Decision Requester` をアタッチ
4. `EnvironmentManager` を空の GameObject に追加
5. Post-Processing Volume でビジュアルを調整

## 5. Heuristic テスト

1. Play モードで手動操作テスト
2. エージェントの動きと報酬が正しいか確認
3. `TrainingVisualizer` を配置して確認

## 6. Colab ビルド & 学習

1. Unity メニュー **`RLMovie > Build for Colab (Current Scene)`** をクリック
   - 自動で Linux ビルド → YAML コピー → ZIP パッケージング
   - 出力先: `ColabBuilds/_ReadyToUpload/<シナリオ名>.zip`
2. ZIP を Google Drive の `RL-Movie/Builds/` にアップロード
3. `Notebooks/rl_movie_training.ipynb` を Colab で開く
4. Step 0 の `SCENARIO_NAME` を変更してセルを上から順に実行

## 7. モデル適用 & 録画

1. Google Drive の `RL-Movie/Models/` から .onnx をダウンロード
2. Unity メニュー **`RLMovie > Import Trained Model`** をクリック
   - StreamingAssets にコピー + Behavior Type を自動切替
3. Inspector で Behavior Parameters > Model にファイルをドラッグ
4. Unity Recorder で録画
