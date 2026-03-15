**Findings**
- `High`: エピソード統計が 1 エピソードぶんずれており、`TrainingVisualizer` のグラフと `SuccessRate` が実態を正しく表していません。`BaseRLAgent` は `OnEpisodeBegin()` で先に `totalEpisodes++` と `episodeReward = 0` を行い、その後 `TrainingVisualizer` は `TotalEpisodes` が増えた瞬間の `CurrentEpisodeReward` を履歴に積んでいます。結果として完了エピソードではなく新規エピソード側の値を見ています。 [BaseRLAgent.cs:94](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/BaseRLAgent.cs#L94) [BaseRLAgent.cs:201](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/BaseRLAgent.cs#L201) [TrainingVisualizer.cs:67](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/TrainingVisualizer.cs#L67)

- `High`: Colab notebook は training YAML が複数ある場合でも無言で先頭 1 件だけを選びますが、validator/build 側は「1 つ以上」を許容しています。今後 baseline 用、curriculum 用、ablation 用の YAML を並べ始めると、意図しない設定で学習を回す事故が起きやすいです。 [sync_training_notebook.py:220](C:/rl-movie/Notebooks/sync_training_notebook.py#L220) [rl_movie_training.ipynb:396](C:/rl-movie/Notebooks/rl_movie_training.ipynb#L396) [BuildForColab.cs:322](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/BuildForColab.cs#L322)

- `Medium`: 新規シナリオ作成フローの説明がまだ揺れています。skill / repo-map は `_Template` を「コピー元」と読める書き方ですが、実装側の正規ルートは `Create Golden Scenario Starter Files` と生成された scene builder です。ここが揃っていないと、本番運用で手コピー派と generator 派に分かれます。 [scenario-build/SKILL.md:13](C:/rl-movie/.agents/skills/scenario-build/SKILL.md#L13) [repo-map.md:8](C:/rl-movie/.agents/references/repo-map.md#L8) [README.md:5](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Environments/_Template/README.md#L5)

- `Medium`: `randomizePositions` がスターター契約どおりに効いていません。`GetRandomPosition()` はフラグを見る一方、`GetRandomEdgePosition()` は常にランダムで、テンプレート agent は goal 配置にこちらを使っています。つまり randomization を切ってもエピソード条件が変わります。 [EnvironmentManager.cs:26](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/EnvironmentManager.cs#L26) [EnvironmentManager.cs:36](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Scripts/EnvironmentManager.cs#L36) [TemplateAgent.cs.txt:32](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Environments/_Template/Scripts/TemplateAgent.cs.txt#L32)

- `Medium`: `Import Trained Model` は import を完了させず、しかも対象が広すぎます。全 `BehaviorParameters` を `Inference Only` に切り替える一方で、モデル参照自体は手動設定のままです。シーンが複雑化すると「無関係な agent まで切り替わる」「主 agent には model 未設定」という半端な状態になりやすいです。 [ImportTrainedModel.cs:47](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ImportTrainedModel.cs#L47) [ImportTrainedModel.cs:61](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/ImportTrainedModel.cs#L61)

- `Medium`: `Build All Scenes for Colab` は、通常の `Build for Colab (Current Scene)` が通している validator / behavior-type gate を踏まずに `Build()` を呼びます。なので bulk build だけは、ふだんなら止まるシーンでも zip を吐けます。 [BuildForColab.cs:69](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/BuildForColab.cs#L69) [BuildForColab.cs:95](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/BuildForColab.cs#L95)

- `Low`: 生成物の ignore が足りません。`.gitignore` に `AI-RL-Movie/ColabBuilds/`、`.tmp-training-results/`、`.venv-mlagents-1.1.0/` が入っていないので、本格運用に入ると untracked ノイズがかなり増えます。 [BuildForColab.cs:24](C:/rl-movie/AI-RL-Movie/Assets/_RLMovie/Common/Editor/BuildForColab.cs#L24) [run_local_training.ps1:8](C:/rl-movie/Notebooks/run_local_training.ps1#L8) [.gitignore:1](C:/rl-movie/.gitignore#L1)

**Improvement Gap**
- `run-ingest-archive` は考え方は良いですが、実 repo 内の保存先がまだ固定されていません。scale 前に「repo に戻すときはここへ置く」を 1 つ決めておくと、比較履歴がかなり安定します。 [run-ingest-archive/SKILL.md:27](C:/rl-movie/.agents/skills/run-ingest-archive/SKILL.md#L27) [archive-layout.md:21](C:/rl-movie/.agents/skills/run-ingest-archive/references/archive-layout.md#L21)

`ThirdParty` を除外して静的レビューしました。Unity 実行や Colab 再実行はしていません。

次に着手するなら、順番は `1) 統計/visualizer 修正`、`2) notebook の config 選択を明示化`、`3) starter/import/workflow 文言の一本化` が一番効果的です。