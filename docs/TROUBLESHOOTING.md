# RL Movie — トラブルシューティング

## 目次

1. [Colab ビルドの問題](#1-colab-ビルドの問題)
2. [ML-Agents の問題](#2-ml-agents-の問題)
3. [YAML 設定の問題](#3-yaml-設定の問題)
4. [モデルインポートの問題](#4-モデルインポートの問題)
5. [録画の問題](#5-録画の問題)
6. [Git / .meta の問題](#6-git--meta-の問題)
7. [Unity エディタの問題](#7-unity-エディタの問題)

---

## 1. Colab ビルドの問題

### ❌ 「シーンが保存されていません」エラー

**原因**: 未保存のシーンで `Build for Colab` を実行した。

**対処**:
1. `Ctrl+S` でシーンを保存
2. 再度 `RLMovie > Build for Colab` を実行

### ❌ ビルドが失敗する（Build Failed）

**原因**: コンパイルエラー、ターゲットプラットフォーム未対応など。

**対処**:
1. Console ウィンドウのエラーを確認
2. `File > Build Settings` で `Linux` が選択可能か確認
3. Linux Build Support モジュールがインストールされているか確認:
   - `Unity Hub > Installs > 対象バージョン > Add Modules > Linux Build Support`

### ❌ ZIP が Google Drive で見つからない

**対処**:
1. `ColabBuilds/_ReadyToUpload/` フォルダを確認
2. ZIP ファイルが生成されているか確認
3. Google Drive の `RL-Movie/Builds/` に正しくアップロードされているか確認

---

## 2. ML-Agents の問題

### ❌ Colab で `mlagents-learn` がバージョンエラー

**原因**: protobuf のバージョン不整合。

**対処**: ノートブックの Step 1 で以下が実行されているか確認:
```bash
!pip install mlagents==1.1.0
!pip install protobuf==3.20.3
```

### ❌ 「No Behavior found」エラー

**原因**: Behavior Name の不一致。

**対処**:
1. エージェントの `Behavior Parameters > Behavior Name` を確認
2. YAML の `behaviors:` キーと完全一致しているか確認
3. 大文字小文字に注意（`RollerBallAgent` ≠ `rollerBallAgent`）

### ❌ 学習が進まない（報酬が増えない）

**対処**:
1. Heuristic モードで手動テストして、報酬設計が正しいか確認
2. `max_steps` が十分に大きいか確認（最低 50 万推奨）
3. `learning_rate` が適切か確認（デフォルト 3e-4）
4. Observation が正しく設定されているか確認（次元数の一致）
5. 報酬のスケールが極端でないか確認（-1.0〜+1.0 の範囲推奨）

### ❌ Colab のランタイムが切断される

**対処**:
1. Colab Pro の使用を検討（長時間セッション）
2. `RESUME_TRAINING = True` に設定してセルを再実行
3. `max_steps` を分割して複数回に分けて実行

---

## 3. YAML 設定の問題

### ❌ YAML パースエラー

**原因**: インデントの不整合やタブ文字の混入。

**対処**:
1. **タブ文字を使わない**（スペース 2 つでインデント）
2. `behaviors:` のキーの後にスペースが正しく入っているか確認
3. コロン `:` の後にスペースが入っているか確認

### ❌ Config ファイルが見つからない

**原因**: `BuildForColab.cs` が Config フォルダからファイルを見つけられない。

**対処**:
1. YAML ファイルが `Environments/<シナリオ名>/Config/` フォルダ内にあるか確認
2. フォルダ名とシナリオ名（シーン名）が一致しているか確認
3. 拡張子が `.yaml` であること（`.yml` は検索対象外）

---

## 4. モデルインポートの問題

### ❌ 「Model にドラッグしても反映されない」

**対処**:
1. `.onnx` ファイルが `Assets/StreamingAssets/` にあるか確認
2. `AssetDatabase.Refresh()` が実行されたか確認（Import Trained Model で自動実行）
3. 手動の場合: `Assets` フォルダを右クリック > `Reimport All`

### ❌ Inference モードで動きがおかしい

**対処**:
1. Observation のサイズが学習時と一致しているか確認
2. Action のサイズ（Continuous / Discrete）が一致しているか確認
3. 学習が十分に進んでいるか TensorBoard で確認（成功率が安定しているか）
4. 別の `run_id` のモデルを試す

---

## 5. 録画の問題

### ❌ 録画が開始されない

**対処**:
1. `Window > General > Recorder > Recorder Window` が開いているか確認
2. Recording Settings が設定されているか確認
3. 出力フォルダが存在し、書き込み権限があるか確認

### ❌ 録画中に UI が表示される

**対処**:
1. `RecordingHelper` の `hideUIWhenRecording` を `true` に設定
2. `RecordingHelper.OnRecordingStart()` が呼ばれているか確認
3. `BaseRLAgent` の `showDebugInfo` を `false` に設定

---

## 6. Git / .meta の問題

### ❌ 「Missing script」警告

**原因**: `.meta` ファイルと C# スクリプトの不整合。

**対処**:
1. スクリプトのファイル名とクラス名が一致しているか確認
2. `.meta` ファイルが Git に含まれているか確認
3. Unity エディタで `Assets` を右クリック > `Reimport All`

### ❌ 「Merge conflict in .meta file」

**対処**:
1. 競合している `.meta` ファイルのどちらかを選択（通常は最新を採用）
2. Unity エディタを再起動して `.meta` を再生成
3. 可能であれば、同じファイルを同時に編集しない運用にする

### ❌ ThirdParty のアセットが崩れた

**対処**:
1. `Assets/ThirdParty/<アセット名>/` を削除
2. Asset Store から再インポート
3. `.meta` ファイルも含めて再コミット

---

## 7. Unity エディタの問題

### ❌ UnityMCP が接続できない

**対処**:
1. `Window > MCP for Unity` > Start Server が押されているか確認
2. Python / uv がインストールされているか確認
3. ファイアウォールでブロックされていないか確認
4. Unity を再起動して再試行

### ❌ コンパイルエラーが消えない

**対処**:
1. Console の Clear ボタンを押してモジュールキャッシュをクリア
2. `Library/` フォルダを削除して再起動（最終手段）
3. `using` ステートメントの不足がないか確認
4. パッケージが正しくインストールされているか `Window > Package Manager` で確認

### ❌ Play モードで例外が発生

**対処**:
1. `NullReferenceException`: Inspector でフィールドが割り当てられているか確認
2. `MissingComponentException`: 必要なコンポーネント（Rigidbody 等）がアタッチされているか確認
3. `IndexOutOfRangeException`: Observation / Action のサイズが Behavior Parameters と一致しているか確認
