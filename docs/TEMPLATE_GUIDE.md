# RL Movie — テンプレート活用ガイド

## テンプレートとは

`Assets/_RLMovie/Environments/_Template/` は新しい RL シナリオを作成する際の**ゴールデンテンプレート**です。ファイルを直接編集せず、必ずコピーして使用してください。

---

## テンプレートの構成

```
_Template/
├── Config/
│   └── template_config.yaml   # ML-Agents YAML 設定のテンプレート
├── Scenes/
│   └── (.gitkeep)             # テンプレートシーン置き場
├── Scripts/
│   └── (.gitkeep)             # テンプレートスクリプト置き場
└── Prefabs/
    └── (.gitkeep)             # テンプレートプレハブ置き場
```

---

## 新しいシナリオの作成手順

> 💡 `/create-scenario` ワークフローも参照してください。

### Step 1: フォルダのコピー

`_Template` フォルダをコピーして、シナリオ名（PascalCase）にリネーム:

```
Assets/_RLMovie/Environments/<シナリオ名>/
├── Scenes/
├── Scripts/
├── Prefabs/
└── Config/
```

### Step 2: エージェントスクリプトの作成

`Scripts/` フォルダに新しい C# スクリプトを作成:

```csharp
using RLMovie.Common;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

namespace RLMovie.Environments.<シナリオ名>
{
    /// <summary>
    /// シナリオの説明をここに記載
    /// </summary>
    public class <シナリオ名>Agent : BaseRLAgent
    {
        [Header("=== <シナリオ名> Settings ===")]
        // SerializeField をここに追加

        protected override void OnAgentInitialize()
        {
            // Rigidbody 取得などの初期化処理
        }

        protected override void OnEpisodeReset()
        {
            // 位置リセット、ターゲット再配置など
        }

        protected override void CollectAgentObservations(VectorSensor sensor)
        {
            // sensor.AddObservation(...);
            // 各 observation の次元数をコメントで明記
            // 合計: X observations
        }

        protected override void ExecuteActions(ActionBuffers actions)
        {
            // アクションの実行
            // 報酬の計算
            // Success() / Fail() / AddTrackedReward() を使用
        }

        protected override void ProvideHeuristicInput(in ActionBuffers actionsOut)
        {
            // キーボード入力のマッピング
        }
    }
}
```

### Step 3: YAML 設定のカスタマイズ

`template_config.yaml` を `Config/` フォルダにコピーし `<snake_case>_config.yaml` にリネーム:

| パラメータ | 説明 | 変更の目安 |
|-----------|------|-----------|
| `behaviors:` キー | **必ずクラス名に変更** | `<シナリオ名>Agent` |
| `max_steps` | 最大学習ステップ | 簡単: 50万、複雑: 100万〜 |
| `batch_size` | ミニバッチサイズ | 通常 128〜512 |
| `buffer_size` | バッファサイズ | batch_size × 4〜16 |
| `hidden_units` | ネットワーク幅 | 簡単: 128、複雑: 256〜512 |
| `num_layers` | ネットワーク深さ | 通常 2〜3 |
| `learning_rate` | 学習率 | 通常 3e-4 |
| `beta` | エントロピー | 探索促進: 大きく |
| `gamma` | 割引率 | 通常 0.99 |
| `time_horizon` | 時間範囲 | 報酬が遅延する場合は大きく |

### Step 4: シーンの構築

1. 新規シーンを作成して `Scenes/<シナリオ名>.unity` として保存
2. 地面（Plane）・壁・ゴール等の環境オブジェクトを配置
3. エージェント GameObject に以下をアタッチ:
   - 作成したエージェントスクリプト
   - `Behavior Parameters`（Behavior Name = クラス名）
   - `Decision Requester`（Decision Period = 5 推奨）
4. 空の GameObject に `EnvironmentManager` を追加
5. 必要に応じてビジュアル演出スクリプトを追加

### Step 5: Heuristic テスト

1. `Behavior Parameters` の `Behavior Type` を `Default` に設定
2. Play モードで手動操作テスト
3. 報酬が正しく動いているか Console ログで確認
4. `TrainingVisualizer` を配置してグラフを確認

---

## BaseRLAgent のヘルパーメソッド一覧

| メソッド | 用途 | 補足 |
|---------|------|------|
| `Success(reward)` | 成功報酬 + エピソード終了 | 色フラッシュ付き |
| `Fail(penalty)` | 失敗ペナルティ + エピソード終了 | 色フラッシュ付き |
| `AddTrackedReward(reward)` | 中間報酬の追加（追跡あり） | エピソード報酬に自動加算 |
| `FlashColor(color, duration)` | エージェント色を一時変更 | ビジュアルフィードバック |
| `SuccessRate` | 成功率（プロパティ） | 0.0〜1.0 |
| `CurrentEpisodeReward` | 現在エピソードの累積報酬 | |
| `TotalEpisodes` | 総エピソード数 | |

---

## EnvironmentManager の使い方

シーンに空の GameObject を作り、`EnvironmentManager` をアタッチ:

```csharp
[SerializeField] private EnvironmentManager envManager;

protected override void OnEpisodeReset()
{
    // エージェントをランダム位置に
    transform.localPosition = envManager.GetRandomPosition(yOffset: 0.5f);

    // ターゲットをエリア端に
    target.localPosition = envManager.GetRandomEdgePosition(yOffset: 0.5f);
}

protected override void ExecuteActions(ActionBuffers actions)
{
    // 落下判定
    if (envManager.HasFallen(transform))
    {
        Fail(-1.0f);
        return;
    }
}
```

Inspector で設定可能なパラメータ:

| パラメータ | デフォルト | 説明 |
|-----------|-----------|------|
| `areaRadius` | 5.0 | 環境エリアの半径 |
| `fallThreshold` | -5.0 | 落下判定の高さ |
| `randomizePositions` | true | ランダム配置の有効化 |
| `randomizationStrength` | 0.5 | ランダム化の強度 (0〜1) |

---

## 参考実装について

同梱サンプルシナリオは撤去済みです。新しいシナリオを作るときは `_Template/` を起点にし、自分のシナリオ名で `Environments/<ScenarioName>/` を作成してください。
