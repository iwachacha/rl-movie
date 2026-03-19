# Active Ragdoll キャラクターシステム設計

AI Warehouse 風の「ふにゃふにゃ物理キャラ」を**Common 層の共通部品**として設計し、各シナリオで「移動の仕方」を再学習せずにタスクテーマに集中できるようにする。

## コア設計思想

```
学習対象 = タスク判断（どこへ行くか / 何をするか）のみ
面白い動き = 物理シミュレーション × Active Ragdoll 関節バネ が自動生成
```

「ふにゃふにゃ」の面白さは **ConfigurableJoint の低バネ定数** が生む物理的な不安定さと、衝突・慣性によるリアクションから自然に発生する。歩行を学習させる必要はない。

---

## アーキテクチャ

```
┌──────────────────────────────────────────────┐
│  シナリオ Agent（各回固有、BaseRLAgent 継承）   │
│  ─ 学習: 移動方向、インタラクション判断         │
│  ─ Action → character.Move(dir)              │
│  ─ タスク報酬 / 成功・失敗判定                  │
├──────────────────────────────────────────────┤
│  ActiveRagdollController（Common 共通）        │
│  ─ Move(Vector2) : 方向入力 → 力適用           │
│  ─ 起き上がりバイアス（自動姿勢補正）            │
│  ─ 転倒検知 → Agent に通知                     │
│  ─ ふにゃふにゃ度パラメータ公開                  │
├──────────────────────────────────────────────┤
│  Ragdoll Prefab（物理ボディ）                   │
│  ─ 腰(Root) + 胸 + 頭 + 上腕×2 + 前腕×2       │
│    + 太腿×2 + 脛×2 = 10パーツ                  │
│  ─ ConfigurableJoint × 9                      │
│  ─ Rigidbody × 10                             │
│  ─ CapsuleCollider / SphereCollider × 10      │
└──────────────────────────────────────────────┘
```

---

## 提案する変更

### Common 層の新規追加

---

#### [NEW] [ActiveRagdollController.cs](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/ActiveRagdollController.cs)

Active Ragdoll の物理制御を司る MonoBehaviour。**Agent から分離**し、どのシナリオからでも同じキャラを使える。

**主な責務:**

| 機能 | 説明 |
|---|---|
| `Move(Vector2 direction)` | 入力方向に腰 Rigidbody へ力を加える。Agent の `ExecuteActions` から呼ぶ |
| `ApplyUprightBias()` | 毎フレーム、腰に起き上がりトルクを適用（完全に倒れなくする） |
| [HasFallen](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/EnvironmentManager.cs#105-110) プロパティ | 腰の傾きが閾値を超えたら `true`。Agent が [Fail()](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/BaseRLAgent.cs#197-218) の判定に使う |
| `ResetPose()` | 全パーツを初期位置・初期回転・速度ゼロにリセット |
| `Wobbliness` (0–1) | ふにゃふにゃ度。0 = 硬い（学習テーマ集中）、1 = 最大フラフラ（動画映え） |

**公開パラメータ（Inspector / ランダム化対応）:**

```csharp
[Header("=== Active Ragdoll Settings ===")]
[Range(0f, 1f)]   float wobbliness = 0.5f;        // ふにゃふにゃ度
[Range(50, 500)]  float moveForce = 200f;          // 移動力
[Range(10, 300)]  float uprightSpringForce = 100f;  // 起き上がりバネ力
[Range(0, 50)]    float uprightDamper = 10f;        // 姿勢ダンパー
                  float fallAngleThreshold = 70f;   // 転倒判定角度
```

**`Wobbliness` の内部マッピング:**

```
wobbliness = 0.0 → jointSpring = 500, uprightForce = 300  （ほぼ剛体）
wobbliness = 0.5 → jointSpring = 150, uprightForce = 100  （AI Warehouse 風）
wobbliness = 1.0 → jointSpring =  30, uprightForce =  30  （最大ふにゃふにゃ）
```

---

#### [NEW] [RagdollBodyPartLabel.cs](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/RagdollBodyPartLabel.cs)

各ボディパーツに付けるタグ用の軽量コンポーネント。衝突判定やパーツ操作時にパーツの役割を識別する。

```csharp
public enum BodyPartType { Hips, Chest, Head, UpperArmL, UpperArmR, ForearmL, ForearmR, ThighL, ThighR, ShinL, ShinR }
```

---

#### [NEW] [ActiveRagdollPrefab](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Prefabs/) — Prefab (Unity Editor / MCP で作成)

**パーツ構成（10パーツ・最小構成）:**

```
Hips (Root, Rigidbody, CapsuleCollider)
 ├── Chest (ConfigurableJoint → Hips)
 │    ├── Head (ConfigurableJoint → Chest, SphereCollider)
 │    ├── UpperArmL (ConfigurableJoint → Chest)
 │    │    └── ForearmL (ConfigurableJoint → UpperArmL)
 │    └── UpperArmR (ConfigurableJoint → Chest)
 │         └── ForearmR (ConfigurableJoint → UpperArmR)
 ├── ThighL (ConfigurableJoint → Hips)
 │    └── ShinL (ConfigurableJoint → ThighL)
 └── ThighR (ConfigurableJoint → Hips)
      └── ShinR (ConfigurableJoint → ThighR)
```

**ConfigurableJoint 共通設定:**

| パラメータ | 値 |
|---|---|
| X/Y/Z Motion | Locked |
| Angular X/Y/Z Motion | Limited |
| Angular X Limit | ±45° (膝: 0〜90°) |
| Angular YZ Limit | ±30° |
| Angular X Drive Spring | `Wobbliness` で制御 |
| Angular X Drive Damper | Spring の 0.2倍 |
| Target Rotation | 初期ポーズ |

**Rigidbody 質量配分:**

| パーツ | 質量 (kg) | 備考 |
|---|---|---|
| Hips | 8.0 | 重心＝安定性の源 |
| Chest | 6.0 | |
| Head | 3.0 | 重め = リアクション大 |
| UpperArm | 1.5 | |
| Forearm | 1.0 | |
| Thigh | 3.0 | |
| Shin | 2.0 | |

> [!TIP]
> 重心を低く保つ（Hips が最重）ことで、起き上がりバイアスが弱くても自然に立ちやすくなる。

---

### 共通バックボーン計画

| 区分 | 対象 |
|---|---|
| **そのまま再利用** | [BaseRLAgent](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/BaseRLAgent.cs#15-366), [EnvironmentManager](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/EnvironmentManager.cs#18-232), `ScenarioGoldenSpine`, `RecordingHelper`, `ScenarioBroadcastOverlay`, `ScenarioHighlightTracker`, `TrainingVisualizer`, `InWorldDisplay` 群 |
| **設定のみ変更** | [scenario_manifest.yaml](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Environments/_Template/Config/scenario_manifest.yaml) テンプレート（`observation_contract` にラグドール姿勢情報を追加、`randomization_knobs` に `wobbliness` を追加） |
| **Common に新規追加** | `ActiveRagdollController.cs`, `RagdollBodyPartLabel.cs`, Active Ragdoll Prefab |
| **シナリオローカル** | 各シナリオの Agent クラス（タスク固有の報酬・観測・成功判定） |

---

## シナリオ Agent からの使い方（例）

```csharp
public class FoodCollectorAgent : BaseRLAgent
{
    ActiveRagdollController ragdoll;

    protected override void OnAgentInitialize()
    {
        ragdoll = GetComponentInChildren<ActiveRagdollController>();
    }

    protected override void OnEpisodeReset()
    {
        ragdoll.ResetPose();
    }

    protected override void CollectAgentObservations(VectorSensor sensor)
    {
        // タスク固有の観測（食べ物の位置等）
        sensor.AddObservation(targetRelativePos);
        // ラグドール姿勢（共通）
        sensor.AddObservation(ragdoll.HipsUp);          // 腰の上方向 (3)
        sensor.AddObservation(ragdoll.HipsVelocity);    // 腰の速度 (3)
        sensor.AddObservation(ragdoll.TiltAngle / 90f); // 傾き (1)
    }

    protected override void ExecuteActions(ActionBuffers actions)
    {
        // 学習するのは「どこに向かうか」だけ
        ragdoll.Move(new Vector2(
            actions.ContinuousActions[0],
            actions.ContinuousActions[1]));

        // 転倒チェック
        if (ragdoll.HasFallen)
            Fail(-0.5f, "fell_down", "転倒した！");
    }
}
```

---

## 観測空間への影響

ラグドール使用時、Agent は以下の**共通姿勢観測**を追加する（各シナリオ共通）：

| 観測 | 次元 | 説明 |
|---|---|---|
| `hipsUp` | 3 | 腰の上方向ベクトル（World） |
| `hipsVelocity` | 3 | 腰の速度 |
| `tiltAngle` | 1 | 正規化された傾き角度 (0=直立, 1=完全転倒) |

合計 **+7 次元**（シナリオ固有の観測に追加される）

---

## ランダム化ノブ

[scenario_manifest.yaml](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Environments/_Template/Config/scenario_manifest.yaml) に追加される新しいノブ：

```yaml
randomization_knobs:
  - name: ragdoll.wobbliness
    purpose: "ふにゃふにゃ度。低い=安定、高い=面白い動き"
    default: "0.5"
  - name: ragdoll.moveForce
    purpose: "移動力。高いと勢いで転びやすい"
    default: "200"
```

---

## 動画映えのポイント

| AI Warehouse 的面白さ | この設計でどう実現するか |
|---|---|
| ぐにゃぐにゃ感 | `wobbliness=0.5` での低バネ ConfigurableJoint |
| 転びそうで転ばない | 起き上がりバイアス + 低重心設計 |
| 衝突リアクション | 各パーツの Collider が環境と物理衝突 |
| 過剰な力で転ぶ | `moveForce` が高いと慣性で振り回される |
| 失敗のコミカルさ | 転倒 → Fail → リセット のサイクル自体がハイライト |

---

## User Review Required

> [!IMPORTANT]
> **パーツ数の判断**: 10パーツは「最小限で面白い動きが出る」構成です。5パーツ（腰+胸+脚×2+頭）にするとシンプルだが腕がない分リアクションが減ります。逆に指や足先を追加すると物理計算コストが増えます。10パーツで進めてよいですか？

> [!IMPORTANT]
> **ビジュアル**: Prefab のメッシュ表現について：
> - **A. プリミティブ構成**（Capsule/Sphere の組み合わせ）→ すぐ作れるがシンプル
> - **B. 無料アセット調査**（`unity-free-asset-research` で Ragdoll 向きキャラを探す）→ 見栄えが期待できるが時間がかかる
> - **C. ProBuilder で簡易モデリング**→ 中間的な選択肢
>
> 動画映えを考えると B が理想ですが、まずプリミティブで動きを確認し後でビジュアルを差し替える（A→B）の段階アプローチもあります。

> [!IMPORTANT]
> **歩行アニメーションの有無**: 現設計は「歩行モーションなし・力で滑るように移動」です。AI Warehouse の一部動画のように「脚を交互に動かして歩こうとする」見た目を出すには、脚にも周期的な力を加える仕組みが必要です。これは追加のオプション機能として後から入れることもできますが、初期スコープに入れますか？

---

## 検証計画

### Unity Editor 上の動作確認

1. Prefab をシーンに配置して Play
2. `ActiveRagdollController` の `Move()` をキー入力で呼び出すテストシーン
3. `wobbliness` を 0 / 0.5 / 1.0 に変えて動きの変化を確認
4. 衝突オブジェクトを置いてリアクションを確認
5. 転倒検知が正しく動くことを確認

### ユーザーによる手動確認

- Prefab を Play モードで操作して「面白い動き」が出ているかの主観評価
- `wobbliness` スライダーの変更でふにゃふにゃ度が直感的に変わることの確認
- 衝突時のリアクションが自然かどうかの確認

> [!NOTE]
> ユニットテストは物理挙動の主観評価が中心のため、自動テストより手動確認が主体です。ただし `ResetPose()` が正しく元の姿勢に戻るか等の基本動作は PlayMode テストで検証できます。
