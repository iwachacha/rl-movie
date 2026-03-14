# RL Movie — コーディング規約

## 1. C# コーディングスタイル

### 1.1 基本ルール

- **言語**: C# 9.0+（Unity 2021.2+）
- **インデント**: スペース 4 つ
- **中括弧**: 次の行に配置（Allman スタイル）
- **改行**: CRLF（Windows）

### 1.2 命名規則

| 種別 | 形式 | 例 |
|------|------|---|
| クラス / struct | PascalCase | `ScenarioAgent`, `EnvironmentManager` |
| public メソッド | PascalCase | `GetRandomPosition()`, `OnRecordingStart()` |
| private / protected メソッド | PascalCase | `ApplyColor()`, `NextCamera()` |
| public プロパティ | PascalCase | `SuccessRate`, `TotalEpisodes` |
| private フィールド | `_camelCase` | `_agentRenderer`, `_flashTimer` |
| protected フィールド | camelCase | `totalEpisodes`, `episodeReward` |
| SerializeField | camelCase | `moveForce`, `goalDistance` |
| ローカル変数 | camelCase | `distToTarget`, `randomCircle` |
| 定数 | PascalCase | `BuildRootDir`, `DriveUploadDir` |
| Enum | PascalCase | `BehaviorType` |
| パラメータ | camelCase | `reward`, `yOffset` |

### 1.3 namespace 規約

```csharp
// 共通スクリプト
namespace RLMovie.Common { }

// シナリオ固有スクリプト
namespace RLMovie.Environments.MyScenario { }
namespace RLMovie.Environments.MazeRunner { }

// エディタ拡張
namespace RLMovie.Editor { }
```

> ⚠️ namespace なしのスクリプト作成は**禁止**。

---

## 2. Unity 固有のルール

### 2.1 Inspector 属性

すべての `[SerializeField]` に `[Header]` と `[Tooltip]` を付ける:

```csharp
[Header("=== セクション名 ===")]
[Tooltip("このフィールドの説明")]
[SerializeField] private float moveForce = 1.0f;

[Tooltip("別のフィールドの説明")]
[Range(0f, 1f)]
[SerializeField] private float randomizationStrength = 0.5f;
```

- `[Header]` は `=== 名前 ===` の形式で統一
- `[Range]` は数値の制約がある場合に使用
- デフォルト値を必ず設定する

### 2.2 MonoBehaviour ライフサイクル順序

```csharp
// 1. Awake
// 2. OnEnable
// 3. Start
// 4. Update
// 5. FixedUpdate (物理)
// 6. LateUpdate
// 7. OnDisable
// 8. OnDestroy
```

コード内のメソッド定義もこの順序に従う。

### 2.3 #region の使い方

```csharp
#region Unity Lifecycle
// Awake, Update 等
#endregion

#region ML-Agents Overrides
// Initialize, OnEpisodeBegin 等
#endregion

#region Abstract Methods
// 子クラスで実装するメソッド
#endregion

#region Helper Methods
// ユーティリティメソッド
#endregion

#region Debug GUI
// OnGUI, Gizmos 等
#endregion
```

### 2.4 FindObjectsByType

`FindObjectsOfType` は非推奨。代わりに `FindObjectsByType` を使用:

```csharp
// ✅ 正しい
var agents = FindObjectsByType<BaseRLAgent>(FindObjectsSortMode.None);

// ❌ 非推奨
var agents = FindObjectsOfType<BaseRLAgent>();
```

---

## 3. ログ出力の規約

### 3.1 フォーマット

```csharp
Debug.Log($"[{クラス名}] 絵文字 メッセージ {変数}");
```

### 3.2 絵文字プレフィックス

| 絵文字 | 用途 | 例 |
|--------|------|---|
| ✅ | 成功・完了 | `✅ Build succeeded` |
| ❌ | エラー・失敗 | `❌ FAIL! Episode 42` |
| 📋 | 情報・コピー | `📋 Config copied` |
| 📦 | パッケージ・ZIP | `📦 ZIP created` |
| 📁 | ファイル・フォルダ | `📁 Upload this ZIP` |
| 📥 | インポート・ダウンロード | `📥 Model copied to` |
| 🔨 | ビルド | `🔨 Building for Linux` |
| 🧠 | AI・モデル | `🧠 Behavior Type → Inference` |
| 🎬 | 録画 | `🎬 Recording started` |
| 📊 | 統計・グラフ | `📊 Training Monitor` |
| 🎯 | ゴール・ターゲット | `🎯 Scenario: MyScenario` |
| ⚠️ | 警告 | `⚠️ Config file not found` |

---

## 4. YAML ファイルの規約

### 4.1 フォーマットルール

```yaml
# 冒頭にコメントで概要と使い方を記載
# ML-Agents トレーニング設定: <シナリオ名>
# 使い方: mlagents-learn <config>.yaml --run-id=<実行名>

behaviors:
  <BehaviorName>:        # エージェントクラス名と一致
    trainer_type: ppo

    hyperparameters:
      batch_size: 128    # インラインコメントで補足
      buffer_size: 2048
```

### 4.2 必須セクション

| セクション | 必須? | 説明 |
|-----------|-------|------|
| `behaviors:` | ✅ | エージェント設定 |
| `trainer_type:` | ✅ | `ppo` or `sac` |
| `hyperparameters:` | ✅ | 学習パラメータ |
| `network_settings:` | ✅ | ネットワーク構造 |
| `reward_signals:` | ✅ | 報酬信号設定 |
| `max_steps:` | ✅ | 学習ステップ上限 |

### 4.3 コメントルール

- 日本語・英語どちらも OK
- `template_config.yaml` に倣い、各パラメータにインラインコメントを付ける
- 上級パラメータ（curiosity 等）はコメントアウトして例示する

---

## 5. XML ドキュメントコメント

### public メソッド・プロパティ

```csharp
/// <summary>環境エリア内のランダムな位置を返す</summary>
public Vector3 GetRandomPosition(float yOffset = 0f)
```

### クラス

```csharp
/// <summary>
/// 🎬 Scenario Agent
/// 対象シナリオ内で行動する代表的な RL エージェント。
/// </summary>
public class ScenarioAgent : BaseRLAgent
```

- 絵文字をクラスの `<summary>` 先頭に付けると、シナリオの雰囲気が分かりやすい
- 長い説明は改行して記載
