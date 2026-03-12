# RL Movie — パイプライン全体像

## パイプラインフロー

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│  1. 設計     │───▶│  2. 実装     │───▶│ 3. テスト   │───▶│ 4. ビルド    │
│  シナリオ企画 │    │  C# + シーン │    │  Heuristic  │    │  Linux ZIP   │
└─────────────┘    └─────────────┘    └─────────────┘    └──────┬──────┘
                                                               │
┌─────────────┐    ┌─────────────┐    ┌─────────────┐         │
│  7. 公開     │◀───│  6. 録画     │◀───│ 5. 学習     │◀────────┘
│  動画アップ  │    │  Recorder   │    │  Google Colab│
└─────────────┘    └─────────────┘    └─────────────┘
```

---

## 各ステージの詳細

### 🎯 Stage 1: シナリオ設計

**目的**: どんな RL 環境を作るか決める

- エージェント・環境・報酬設計を検討
- 参考動画や事例を調査
- 必要な 3D アセットの洗い出し

**成果物**: シナリオ仕様（概要、観測空間、行動空間、報酬体系）

**所要時間目安**: 15〜30 分

---

### 💻 Stage 2: 実装

**目的**: Unity シーンとエージェントスクリプトを実装

**手順**:
1. `/create-scenario` ワークフローを実行
2. テンプレートからフォルダ構成をコピー
3. エージェントスクリプトを作成（`BaseRLAgent` 継承）
4. YAML 設定をカスタマイズ
5. Unity シーンを構築（環境、エージェント配置）

**使うツール**:
- Unity Editor（シーン構築）
- UnityMCP（AI 経由でのシーン操作）
- VS Code / Rider（C# 編集）

**参照ドキュメント**: `TEMPLATE_GUIDE.md`, `CODING_CONVENTIONS.md`

**所要時間目安**: 30 分〜2 時間（シナリオの複雑さによる）

---

### 🧪 Stage 3: Heuristic テスト

**目的**: 手動操作で環境が正しく動くか確認

**手順**:
1. `Behavior Parameters > Behavior Type` を `Heuristic Only` に設定
2. Play モードで WASD/矢印キー操作
3. 確認項目:
   - エージェントの動きが正しいか
   - 報酬が適切にログ出力されているか（Console で確認）
   - リセットが正しく動くか
   - 落下判定・ゴール判定が機能するか
4. `TrainingVisualizer` で報酬グラフを確認

**所要時間目安**: 10〜30 分

---

### 📦 Stage 4: Colab ビルド

**目的**: Linux 用ビルドを作成して Google Drive にアップ

**手順**:
1. Unity メニュー **`RLMovie > Build for Colab (Current Scene)`** をクリック
2. 自動で以下が実行される:
   - Linux Standalone ビルド
   - YAML 設定ファイルのコピー
   - ZIP パッケージング
3. 出力: `ColabBuilds/_ReadyToUpload/<シナリオ名>.zip`
4. ZIP を Google Drive の `RL-Movie/Builds/` にアップロード

**注意**: ビルド前に Stage 3 のテストを必ず完了させること

**所要時間目安**: 5〜15 分（ビルド時間含む）

---

### 🧠 Stage 5: Colab 学習

**目的**: Google Colab で強化学習を実行

**手順**:
1. `Notebooks/rl_movie_training.ipynb` を Google Colab で開く
2. **Step 0** の設定を変更:
   - `SCENARIO_NAME` = シナリオ名
   - `RUN_ID` = 実行 ID（例: `run_001`）
   - `MAX_STEPS` = 最大ステップ数
3. セルを**上から順に**実行
4. トレーニング完了後、TensorBoard で学習曲線を確認
5. モデルが自動的に `RL-Movie/Models/` にコピーされる

**Google Drive フォルダ構成**:
```
Google Drive/RL-Movie/
├── Builds/    ← ZIP をアップロード
├── Results/   ← 学習結果（自動保存）
└── Models/    ← 学習済みモデル（自動コピー）
```

**所要時間目安**: 30 分〜2 時間（max_steps とシナリオ複雑さによる）

---

### 🤖 Stage 6: モデル適用 & 録画

**目的**: 学習済みモデルで動かして動画を録画

**手順**:
1. Google Drive の `RL-Movie/Models/` から `.onnx` ファイルをダウンロード
2. Unity メニュー **`RLMovie > Import Trained Model`** をクリック
   - StreamingAssets にコピー
   - Behavior Type が自動で `Inference Only` に変更
3. Inspector の `Behavior Parameters > Model` にファイルをセット
4. Play して動作確認
5. Unity Recorder で録画:
   - `Window > General > Recorder > Recorder Window`
   - 録画設定は `_RLMovie/Recording/RecorderSettings/` に保存

**所要時間目安**: 15〜30 分

---

### 🎬 Stage 7: 動画公開

**目的**: 完成した動画を公開

- 録画した動画に BGM・テロップを追加（任意）
- YouTube 等にアップロード
- サムネイル作成

---

## パイプライン全体の所要時間目安

| ステージ | 目安 | 備考 |
|---------|------|------|
| 1. 設計 | 15〜30 分 | |
| 2. 実装 | 30 分〜2 時間 | AI 支援で短縮可能 |
| 3. テスト | 10〜30 分 | |
| 4. ビルド | 5〜15 分 | 自動化済み |
| 5. 学習 | 30 分〜2 時間 | Colab GPU 依存 |
| 6. 録画 | 15〜30 分 | |
| 7. 公開 | 任意 | |
| **合計** | **約 2〜6 時間** | 1 シナリオあたり |

> 💡 量産フェーズに入ると Stage 2〜4 が慣れにより大幅短縮されます。
