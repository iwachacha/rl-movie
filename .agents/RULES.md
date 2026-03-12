# RL Movie プロジェクト — AI エージェント向けルール

> **このファイルは AI（MCP経由）がプロジェクトを編集する際に必ず遵守すべきルールです。**
> すべてのファイル作成・編集・削除・コマンド実行の前に、このルールを確認してください。

---

## 1. フォルダ構成ルール

### 1.1 触ってよいフォルダ

| フォルダ | 用途 | 注意事項 |
|---------|------|---------|
| `Assets/_RLMovie/Environments/<シナリオ名>/` | シナリオ固有ファイル | 新規シナリオはここに作成 |
| `Assets/_RLMovie/Common/Scripts/` | 共通スクリプト | 変更時は全シナリオへの影響を確認 |
| `Assets/_RLMovie/Common/Editor/` | エディタ拡張 | Editor 専用コードのみ |
| `Assets/_RLMovie/Common/Materials/` | 共通マテリアル | |
| `Assets/_RLMovie/Common/Shaders/` | 共通シェーダー | |
| `Assets/_RLMovie/Common/Prefabs/` | 共通プレハブ | |
| `Assets/_RLMovie/Recording/` | 録画設定 | |
| `Assets/ThirdParty/` | 外部アセット | README.md に登録必須 |
| `Notebooks/` | Colab ノートブック | |
| `.agents/workflows/` | AI ワークフロー定義 | |
| `docs/` | プロジェクトドキュメント | |

### 1.2 絶対に触ってはいけないフォルダ

| フォルダ | 理由 |
|---------|------|
| `Library/` | Unity 自動生成。手動編集すると壊れる |
| `Temp/` | Unity 一時ファイル |
| `Logs/` | Unity ログファイル |
| `UserSettings/` | ユーザー固有設定 |
| `Packages/` | パッケージマニフェスト以外は触らない（`manifest.json` のみ編集可） |
| `ProjectSettings/` | 原則触らない。変更が必要な場合はユーザーに確認 |
| `Assets/ML-Agents/` | ML-Agents パッケージのサンプル。変更禁止 |

### 1.3 新規シナリオのフォルダ構成（必須）

```
Assets/_RLMovie/Environments/<シナリオ名>/
├── Scenes/          # Unity シーン
├── Scripts/         # シナリオ固有の C# スクリプト
├── Prefabs/         # シナリオ固有のプレハブ
├── Config/          # ML-Agents YAML 設定
├── Materials/       # シナリオ固有のマテリアル（必要に応じて）
└── Editor/          # シナリオ固有のエディタスクリプト（必要に応じて）
```

> ⚠️ `Scenes/`, `Scripts/`, `Prefabs/`, `Config/` の 4 フォルダは**必須**。省略不可。

---

## 2. ファイル編集ルール

### 2.1 `.meta` ファイル

- **`.meta` ファイルを手動で作成・編集・削除してはいけない**
- `.meta` は Unity が自動生成・管理する
- ファイルを移動する場合は Unity Editor 経由で行うこと
- Git へのコミット時には対応する `.meta` ファイルを含めること

### 2.2 C# スクリプトの基本ルール

- 新しいエージェントスクリプトは**必ず `BaseRLAgent` を継承**する
- `Agent` クラスを直接継承してはいけない
- namespace は必ず指定する（後述の命名規則を参照）
- `using RLMovie.Common;` を忘れないこと

### 2.3 シーンファイル

- `.unity` ファイルをテキストエディタで直接編集しない
- シーンの変更は Unity Editor（または UnityMCP）経由で行う
- シーンを変更したら**必ず保存**してから次の操作に進む

### 2.4 YAML 設定ファイル

- `template_config.yaml` のオリジナルは**絶対に変更しない**（テンプレート保全）
- 新しい設定は必ずコピーしてから編集する
- `behaviors:` のキー名はエージェントの **Behavior Name** と完全一致させる

---

## 3. 命名規則

### 3.1 C# クラス名 / ファイル名

| 種別 | 規則 | 例 |
|------|------|---|
| エージェント | `<シナリオ名>Agent` | `RollerBallAgent`, `MazeRunnerAgent` |
| ビジュアル | `<対象>Visual` | `FloorVisual`, `TargetVisual` |
| マネージャー | `<機能>Manager` | `EnvironmentManager` |
| エディタ拡張 | 動作を表す名前 | `BuildForColab`, `ImportTrainedModel` |
| シーンビルダー | `<シナリオ名>SceneBuilder` | `RollerBallSceneBuilder` |

### 3.2 namespace 規約

```
RLMovie.Common           — 共通スクリプト
RLMovie.Environments.<シナリオ名>  — シナリオ固有スクリプト
RLMovie.Editor           — エディタ拡張（Common/Editor 内）
```

### 3.3 YAML / Config

| 項目 | 規則 | 例 |
|------|------|---|
| ファイル名 | `<snake_case>_config.yaml` | `roller_ball_config.yaml` |
| Behavior キー | エージェントクラス名と同じ | `RollerBallAgent` |

### 3.4 シーン名

- シーン名 = シナリオフォルダ名（PascalCase）
- 例: `RollerBall`, `MazeRunner`, `CartPole`

### 3.5 Behavior Name

- Behavior Name = エージェントクラス名と同じにする
- YAML の `behaviors:` キーと完全一致させること
- 例: クラス名 `RollerBallAgent` → Behavior Name `RollerBallAgent` → YAML キー `RollerBallAgent:`

---

## 4. コーディング規約

### 4.1 C# スタイル

- **Header 属性**: Inspector のセクション分けに `[Header("=== セクション名 ===")]` を使用
- **Tooltip 属性**: すべての `[SerializeField]` に `[Tooltip("説明")]` を付ける
- **XML ドキュメント**: public メソッドには `/// <summary>` を付ける
- **ログ出力**: `Debug.Log($"[{クラス名}] 絵文字 メッセージ")` の形式
  - 成功: ✅, 失敗: ❌, 情報: 📋/📦/📁, 録画: 🎬, ビルド: 🔨, AI: 🧠

### 4.2 BaseRLAgent 実装パターン

新しいエージェントでは以下の 5 つの abstract メソッドを**すべて**実装すること:

```csharp
protected override void OnAgentInitialize() { }        // 初期化
protected override void OnEpisodeReset() { }            // エピソードリセット
protected override void CollectAgentObservations(VectorSensor sensor) { }  // 観測
protected override void ExecuteActions(ActionBuffers actions) { }          // アクション
protected override void ProvideHeuristicInput(in ActionBuffers actionsOut) { }  // 手動操作
```

### 4.3 報酬設計のルール

- 成功時は `Success(reward)` を呼ぶ（`EndEpisode()` を直接呼ばない）
- 失敗時は `Fail(penalty)` を呼ぶ（`EndEpisode()` を直接呼ばない）
- 中間報酬は `AddTrackedReward(reward)` を使う（`AddReward()` を直接呼ばない）
- 報酬値の目安: 成功 `+1.0`, 失敗 `-1.0`, ステップペナルティ `-0.0005` 程度

### 4.4 観測データのコメント

`CollectAgentObservations` 内では、各 observation の次元数をコメントで明記する:

```csharp
sensor.AddObservation(transform.localPosition);  // (3)
sensor.AddObservation(target.localPosition);      // (3)
// 合計: 6 observations
```

---

## 5. Asset Store アセットの管理ルール

1. Asset Store からインポートしたアセットは**必ず `Assets/ThirdParty/` 配下**に配置
2. `Assets/ThirdParty/README.md` の一覧テーブルに登録する（アセット名、ソース、ライセンス、用途）
3. `docs/ASSET_CATALOG.md` にも詳細を記録する
4. ThirdParty 内のファイルを**直接編集しない**。カスタマイズが必要な場合はコピーを作る
5. 無料 & 商用利用可能なアセットのみ使用
6. プレファブとして使う場合は、シナリオの `Prefabs/` フォルダに Prefab Variant を作成

---

## 6. ビルド & デプロイのルール

### 6.1 Colab ビルド前チェックリスト

1. ✅ シーンが保存されているか
2. ✅ Behavior Name と YAML キーが一致しているか
3. ✅ 対応する YAML 設定ファイルが `Config/` フォルダにあるか
4. ✅ エージェントの Observation / Action サイズが正しいか
5. ✅ Heuristic モードでテスト済みか

### 6.2 Google Drive フォルダ構成

```
Google Drive/
└── RL-Movie/
    ├── Builds/    ← Unity から ZIP をアップロードする場所
    ├── Results/   ← 学習結果（TensorBoard ログ）が自動保存される場所
    └── Models/    ← 学習済み .onnx モデルが自動コピーされる場所
```

---

## 7. Git ルール

- コミットメッセージは日本語 OK
- 書式: `[カテゴリ] 変更内容`
  - カテゴリ例: `[Scenario]`, `[Common]`, `[Config]`, `[Editor]`, `[Docs]`, `[Notebook]`
- `.gitignore` に記載されているフォルダ（Library, Temp, etc.）のファイルはコミットしない
- 新規ファイル追加時は対応する `.meta` ファイルも必ずコミットする
- **大きなバイナリファイル**（50MB超）は Git にコミットしない

---

## 8. Unity 操作の安全ルール

- **Play モード中はファイルを編集しない**（変更が失われる可能性がある）
- Prefab を変更する前に、変更内容をユーザーに説明する
- シーン内の既存 GameObject を削除する前にユーザーに確認する
- **Common スクリプトを変更する場合は、全シナリオへの影響を必ず説明する**
- `ProjectSettings/` の変更が必要な場合は、必ずユーザーの許可を得る

---

## 9. 禁止事項チェックリスト

- ❌ `Library/`, `Temp/`, `Logs/` フォルダの編集
- ❌ `.meta` ファイルの手動作成・編集
- ❌ `Agent` クラスの直接継承（`BaseRLAgent` を使う）
- ❌ `AddReward()` / `EndEpisode()` の直接呼び出し（ヘルパーメソッドを使う）
- ❌ `template_config.yaml` の直接編集
- ❌ `_Template` フォルダ内のファイルの直接変更（コピーして使う）
- ❌ `ThirdParty/` 内ファイルの直接編集
- ❌ namespace なしの C# スクリプト作成
- ❌ Behavior Name と YAML キーの不一致
- ❌ Play モード中のスクリプト変更

---

## 10. 新規シナリオ作成時のクイックリファレンス

```
1. /create-scenario ワークフローに従う
2. フォルダ: Assets/_RLMovie/Environments/<PascalCase名>/
3. 必須フォルダ: Scenes/, Scripts/, Prefabs/, Config/
4. エージェント: BaseRLAgent を継承
5. namespace: RLMovie.Environments.<シナリオ名>
6. YAML: <snake_case>_config.yaml
7. Behavior Name = クラス名 = YAML キー
8. Heuristic テスト → ビルド → Colab 学習 → モデル適用 → 録画
```
