# Boss Room × RL Movie マルチエージェント統合戦略 (v3)

Boss Room（UCL）をRL Movieプロジェクトに取り込み、**ボスアリーナ戦**にフォーカスしたマルチエージェント強化学習を動画化する。

---

## 1. 調査結果サマリ

| 項目 | 内容 |
|------|------|
| ライセンス | UCL — 動画利用OK（[前回会話で確認済み](file:///c:/rl-movie/docs)) |
| Unity版 | Unity 6 LTS — 現プロジェクト 6000.3.10f1 と互換 |
| コード構造 | Client / Server / Shared 3アセンブリ（domain-based） |
| キャラ | Archer, Mage, Rogue, Warrior（各2スキン） |
| 敵 | Imp（ウェーブスポーン）, Boss |
| 主要依存 | **NGO**, **VContainer** (DI), AI Navigation, Cinemachine, Input System, UGS (Relay/Lobby/Auth) |
| アート | スタイライズドRPGダンジョン（高品質） |
| ゲーム速度 | FixedUpdate 30Hz, server-authoritative click-to-move |

---

## 2. 戦略的方針

### 2.1 ネットワーク層のオフライン化

**方針**: NGOを除去せず、**Host モード固定**でローカル完結させる。

- `NetworkManager.StartHost()` 直接呼出し → UGS/Relay/Lobby/Auth バイパス
- `NetworkVariable` / RPC / `NetworkTransform` は Host モードで正常動作 → ゲームロジック改変最小
- 接続 UI / マッチメイキングシーンは除外

> [!WARNING]
> **学習速度リスク**: NGO の `NetworkManager` が Host モードでもフレームごとに同期処理を走らせるため、高速シミュレーションのボトルネックになる可能性がある。そのため **2モード構成** を採用する:
> - **学習時**: NGO 除去した軽量ビルド（ゲームロジックのみ）
> - **録画時**: NGO Host モードのフルビジュアル版（アニメ・エフェクトが正常に動く）

### 2.2 選択的ファイル取り込み

**取り込む**:

| カテゴリ | Boss Room パス | 理由 |
|----------|---------------|------|
| ゲームロジック | `Assets/Scripts/Gameplay/` | Action system, Character, AI, GameplayObjects |
| ScriptableObjects | `Assets/ScriptableObjects/` | キャラ・アクション定義データ |
| AI | `Assets/Scripts/Gameplay/GameplayObjects/Character/AI/` | Imp/Boss AI（ベースライン比較） |
| アート・シーン | `Assets/Scenes/`, `Assets/Prefabs/`, `Assets/Art/`, `Assets/Animations/` | ビジュアル |
| VContainer 設定 | DI の LifetimeScope 定義 | ゲームロジック起動に必須 |

**取り込まない**:

| カテゴリ | 理由 |
|----------|------|
| `Assets/Scripts/ConnectionManagement/` | ネットワーク接続不要 |
| `Assets/Scripts/UnityServices/` | UGS 不要 |
| `Assets/Scripts/ApplicationLifecycle/` | セッション管理不要 |
| ネットワーク UI 一式 | マッチメイキング不要 |
| `Packages/com.unity.multiplayer.samples.coop/` | 必要分のみローカルコピー |

### 2.3 追加パッケージ

| パッケージ | 必須? | 備考 |
|-----------|-------|------|
| `com.unity.netcode.gameobjects` | 必須 | Boss Room コード基盤 |
| `jp.hadashikick.vcontainer` (or OpenUPM) | 必須 | Boss Room の DI フレームワーク |
| `com.unity.ai.navigation` | 必須 | Imp/Boss のパスファインディング |
| `com.unity.cinemachine` | 推奨 | カメラ演出（録画品質向上） |
| `com.unity.inputsystem` | 要確認 | 既存プロジェクトで使用中か確認 |

> [!NOTE]
> VContainer は Boss Room のコード全体に深く浸透している DI ライブラリ。取り除くには大規模リファクタが必要なため、パッケージごと導入するのが現実的。既存シナリオには影響しない。

### 2.4 Common 基盤との関係

**結論: Common の Agent/Spine テンプレートは使わず、BossRoom 専用スキャフォールドを採用する。**

理由:
1. Boss Room は既製ゲーム → Common の「ゼロから作るテンプレート」が合わない
2. 4エージェント同時 → 現 Common は単一エージェント前提
3. Action/Character/AI が独自アーキテクチャ → Common wiring に押し込めると逆に複雑

**Common から部分再利用するもの**:
- [RecordingHelper](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs#700-758) — 録画
- `ScenarioBroadcastOverlay` — 視聴者オーバーレイ
- `BuildForColab` / `ImportTrainedModel` — パイプライン

> [!WARNING]
> [ScenarioValidator](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs#15-1283) は `BaseRLAgent` と `ScenarioGoldenSpine` をハードコードで検索するため、**そのままでは BossRoom シーンで動かない**。Phase 2 で BossRoom専用の簡易バリデーションを作るか、Agent が `BaseRLAgent` を継承する薄いラッパーを作るかを検討する。

**配置**:
```
Assets/_RLMovie/Environments/BossRoom/
├── Config/
│   ├── scenario_manifest.yaml
│   └── BossRoom.yaml          # training YAML (MA-POCA)
├── Scenes/
│   └── BossRoom.unity
├── Scripts/
│   ├── BossRoomAgent.cs        # ML-Agents Agent
│   ├── BossRoomGameManager.cs  # エピソードリセット・報酬・終了判定
│   └── BossRoomBootstrap.cs    # Host自動起動・UGSバイパス
├── Prefabs/
└── ThirdParty/
    └── BossRoom/               # 取り込んだコード・アセット
```

---

## 3. マルチエージェント設計

### 3.1 完全別脳アーキテクチャ

各キャラクラスが **独立したニューラルネット（脳）** を持つ。4つの異なる [BehaviorName](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs#1203-1242) で 4 つの独立ポリシーを学習する。

> [!IMPORTANT]
> `SimpleMultiAgentGroup` に異なる [BehaviorName](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs#1203-1242) のエージェントを登録可能。チーム報酬は全員に配分され、各エージェントは **自分専用のポリシー** を独自に学習する。

| BehaviorName | クラス | 行動空間 |
|-------------|--------|----------|
| `BossRoom_Archer` | Archer | 移動(連続×2), 通常射撃/AoEボレー/チャージショット/ターゲット選択 |
| `BossRoom_Mage` | Mage | 移動(連続×2), ボルト/バフ/AoE魔法/ターゲット選択 |
| `BossRoom_Rogue` | Rogue | 移動(連続×2), 近接攻撃/ステルス/ダッシュ攻撃/ターゲット選択 |
| `BossRoom_Warrior` | Warrior | 移動(連続×2), 近接攻撃/シールド/チャージ/ターゲット選択 |

各クラスのアクション分岐はそのクラスのスキルセットに **最適化** される（NOP不要）。

**利点**:
- 各クラスが **固有のプレイスタイルと個性** を発達させる → 動画映え
- 行動空間に無駄な分岐がない → 学習効率向上
- 「Warrior が前に出てタンク」「Rogue がステルスで裏取り」等の **ロール分化が自発的に起きやすい**

### 3.2 観測空間

観測空間は 4 クラス共通構造（各ポリシーが同じ情報を見る、ただし BehaviorName は別）:

| 観測 | 次元 | 正規化方法 |
|------|------|-----------|
| 自キャラ位置 | 2 | arena bounds で正規化 |
| 自キャラ向き | 2 | sin(θ), cos(θ) |
| 自HP / MaxHP | 2 | 0-1 |
| 自スキルクールダウン | 3 | 0-1 固定3枚（スキル不足分はゼロ埋め） |
| 味方3体: 相対位置+HP+クラス+生存 | 24 | 相対座標(2)+HP(1)+one-hot(4)+生存(1) = 8×3 |
| 最寄り敵5体: 相対位置+HP+タイプ | 20 | (2+1+1)×5, 敵不在はゼロ埋め |
| ボス: 相対位置+HP+存在 | 4 | (2+1+1), ボス不在はゼロ |
| **合計** | **57** | |

> [!NOTE]
> 全クラスで同じ観測次元に揃える（スキルクールダウンは3枚固定、実際のスキルが2つのクラスは3枚目をゼロ埋め）。学習結果を見て必要なら調整。

### 3.3 報酬設計

| イベント | 個別報酬 | チーム報酬 | 備考 |
|----------|---------|-----------|------|
| 敵にヒット | +0.02 | — | スパム防止で低め |
| 敵撃破 | +0.1 | +0.05 | |
| ボスにダメージ(HP 1%分) | +0.05 | +0.05 | |
| **ボス撃破** | — | **+1.0** | 最大報酬 |
| 味方リバイブ | +0.2 | — | 協力行動強化 |
| 自分が倒れた | -0.2 | — | |
| **全滅** | — | **-0.5** | 強すぎると回避学習に偏る |
| ステップペナルティ | -0.0005 | — | 緩め（長いゲームのため） |

### 3.4 カリキュラム（アリーナ方式）

> [!IMPORTANT]
> **フルダンジョン走破は廃止。ボスアリーナ戦にフォーカスする。** ダンジョンの部屋移動・ナビゲーション学習は最大の時間消費元であり、動画的にも戦闘シーンが最も映える。Boss Room のダンジョンアートはアリーナの背景・雰囲気として活用する。

| Stage | 構成 | 成功条件 | 目的 |
|-------|------|---------|------|
| **0** | 4人 vs 3 Imp（アリーナ） | 全Imp撃破 | 基本戦闘 + チーム連携の基礎 |
| **1** | 4人 vs Imp Wave (5→10体) | 全Wave撃破 | 波状攻撃への対応・ロール分化の萌芽 |
| **2** | 4人 vs Boss (+ Imp増援) | **ボス撃破** | 最終目標・協力プレイの完成形 |

### 3.5 学習時間削減の4施策

| 施策 | 効果 | 詳細 |
|------|------|------|
| **DecisionPeriod = 5** | エピソードあたり意思決定数 **5分の1** | 毎フレーム判断せず5ステップごと。RPG戦闘には十分な粒度 |
| **学習専用ビルド (NGO除去)** | シミュレーション速度 **2-4倍改善** | 学習時は NGO を完全スキップし簡易ゲームループで走らせる。録画時のみ NGO 有効 |
| **並列環境 (×8-16)** | スループット **8-16倍** | Colab で複数環境インスタンスを同時実行 |
| **小さいネットワーク** | 推論速度向上 | hidden_units=128, num_layers=2 から開始 |

---

## 4. 動画の魅力

| 要素 | 内容 |
|------|------|
| **viewer_promise** | 「AIが4人パーティでRPGボスに挑む — 役割分担は勝手に生まれた」 |
| **thumbnail_moment** | 4人AIがボスを囲んで総攻撃 / ボス撃破瞬間 |
| **visual_hooks** | ロール分担の自発的発生、Warrior前衛、Rogueステルス裏取り、リバイブ、ボス撃破 |
| **学習過程の見どころ** | 全員壁殴り → 1体ずつ戦い方を覚える → チーム行動の萌芽 → 見事な連携プレイ |

---

## 5. 改訂版の時間見積もり

| Stage | 推定ステップ | 推定時間（Colab GPU, 並列×8） |
|-------|------------|-----------------------------|
| **0**: 4人 vs 3 Imp | 2-5M | **2-6時間** |
| **1**: 4人 vs Wave | 5-20M | **6-24時間** |
| **2**: 4人 vs Boss | 20-80M | **24-72時間** |
| **合計** | — | **30-100時間（≒ 1.5-4日連続）** |

> [!TIP]
> v2 の 150-700 時間 → v3 では **30-100 時間** に圧縮。主な削減要因: アリーナ方式 (−60%), DecisionPeriod=5 (−80% 意思決定数), 並列環境 (×8 スループット)。

---

## 6. 実装フェーズ

### Phase 1: 取り込みと最小動作 (~3-5日)
1. Boss Room を clone（Git LFS 必須）
2. 選択的ファイルコピー
3. パッケージ追加（NGO, VContainer, AI Navigation, Cinemachine）
4. `BossRoomBootstrap.cs` で Host 自動起動・UGS バイパス
5. **ボスアリーナシーン** を Boss Room ダンジョンから切り出して構築
6. 速度計測: `Time.timeScale=20` + NGO 有無の比較

### Phase 2: RL ブリッジ (~3-5日)
1. `BossRoomAgent.cs` — クラス別行動空間・共通観測・MA-POCA 対応
2. `BossRoomGameManager.cs` — `SimpleMultiAgentGroup` + エピソードリセット + 報酬
3. **学習専用ビルド** — NGO 除去した軽量版を `BuildForColab` で出力
4. Training YAML — MA-POCA × 4 Behavior + curriculum
5. Stage 0 で最小学習ループの動作確認

### Phase 3: 学習 (~1-2週間, Colab)
1. Stage 0-2 のカリキュラム学習
2. 各 Stage の結果を `RunArchive/` に保存
3. ボス撃破成功の確認

### Phase 4: 録画 (~2-3日)
1. NGO 有効のフルビジュアル版シーンに .onnx インポート
2. カメラ配置（Cinemachine）
3. 学習過程比較クリップ + 最終ボス戦の撮影

---

## 7. リスクと緩和策

| リスク | 影響 | 緩和策 |
|--------|------|--------|
| NGO が高速シミュレーションのボトルネック | 学習速度低下 | Phase 1 で計測。問題なら `TickRate` 調整 or 学習時 NGO スキップモード |
| VContainer 導入で既存コードに副作用 | 他シナリオ破壊 | VContainer は BossRoom assembly のみ参照、他シナリオは触らない |
| Boss Room コードの UGS 参照が散在 | コンパイルエラー | 取り込まないコードから参照を辿り、stub / #if で隔離 |
| 4ポリシー同時学習で計算量増大 | 学習時間増 | 並列環境×8 + DecisionPeriod=5 で相殺 |
| ダンジョン全体が学習に長すぎる | 非現実的 | **アリーナ方式に切替済み** |

---

## 8. Verification Plan

### 自動検証
- 取り込み後 `Console` エラーゼロ
- `RLMovie > Validate Current Scenario` 通過
- `RLMovie > Build for Colab` 成功

### 手動検証
- Unity Play → Host → キャラ表示・Imp 動作を目視
- `Time.timeScale=20` で挙動に異常がないか確認
- `mlagents-learn` 短時間実行 → 4 エージェント同時動作・リセット確認
- [RecordingHelper](file:///c:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs#700-758) で 10 秒録画 → 映像出力確認
