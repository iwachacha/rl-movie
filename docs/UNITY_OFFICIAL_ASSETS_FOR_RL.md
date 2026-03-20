# Unity公式アセット・プロジェクト総まとめ（RL動画化向け）

Unity公式が提供する、UCL（Unity Companion License）や標準ライセンス下で商用利用・動画配信が可能な高品質アセット・プロジェクトの一覧です。
当プロジェクト（強化学習の動画化）での用途と親和性についても解説します。

## 1. キャラクター操作・アクション系

### Starter Assets (Third Person / First Person)
* **ライセンス**: Unity Asset Store EULA (無料/商用可)
* **リンク**: [Third Person](https://assetstore.unity.com/packages/essentials/starter-assets-third-person-character-controller-196526) / [First Person](https://assetstore.unity.com/packages/essentials/starter-assets-first-person-character-controller-196525)
* **概要**: CinemachineとInput Systemを標準搭載した、最新の軽量キャラクターコントローラー。歩行・ジャンプ・ダッシュといった高品質なアニメーションが即座に使えます。
* **RL動画化の観点**: 
  * プロジェクトの「Common（共通基盤）」としてそのまま採用できる最高の素材です。
  * ML-Agentsに「Input Systemの入力（Move, Jumpなど）」を上書きさせるだけで、「人間のようにリアルな挙動でアスレチックを攻略するAI」の動画が簡単に作れます。

### 3D Game Kit 
* **ライセンス**: Unity Asset Store EULA (無料/商用可)
* **リンク**: [3D Game Kit](https://assetstore.unity.com/packages/essentials/tutorial-projects/3d-game-kit-116347)
* **概要**: ノーコードでアクションプラットフォーマーゲームを作れる、敵やギミック（動く床、ダメージ床等）、高品質な環境（SF遺跡風）が含まれた特大パッケージ。
* **RL動画化の観点**: 
  * 「市販ゲームをAIに攻略させてみた」系の企画に直結します。
  * 非常にリッチな絵面が揃っているため、背景をそのまま使い回しつつ、エージェントに「敵を避けてゴールへ向かう」学習をさせるだけで、YouTuberのゲーム実況動画のような見栄えを得られます。

### Boss Room (Multiplayer Sample)
* **ライセンス**: Unity Companion License (UCL)
* **リンク**: [GitHub (Boss Room)](https://github.com/Unity-Technologies/com.unity.multiplayer.samples.coop)
* **概要**: Netcode for GameObjectsを使用した、小規模なCo-opRPGの公式サンプル。複数クラス（魔法使い、弓使いなど）とボス戦のギミックが完全に実装されています。
* **RL動画化の観点**: 
  * 「複数AIによる協力プレイ（Multi-Agent Cooperation）」の検証環境として極めて優秀です。
  * ヘイト（敵の狙い）管理や役割分担（タンク・ヒーラー・アタッカー）をAIが自発的に獲得していく過程を動画化できれば、非常にエンタメ性の高いコンテンツになります。

## 2. 環境・乗り物・ナビゲーション系

### Unity Microgames (Kart)
* **ライセンス**: Unity Asset Store EULA (無料/商用可)
* **リンク**: [Kart Microgame](https://assetstore.unity.com/packages/essentials/tutorial-projects/kart-microgame-150956)
* **概要**: Unity初心者向けに提供されている、1ステージ完結の簡素なカートレースゲーム。
* **RL動画化の観点**: 
  * 学習要素がシンプル（アクセルとステアリングのみ）なため、自作する手間が省けます。
  * Kart環境はML-Agentsの公式サンプルから既に流用実績があり、数時間Colabで回すだけで「絶対に壁にぶつからないドリフトAI」のプレイ映像が作れます。

### Megacity Metro
* **ライセンス**: Unity Companion License (UCL)
* **リンク**: [GitHub (Megacity Metro)](https://github.com/Unity-Technologies/Megacity-Metro)
* **概要**: DOTS（Data-Oriented Technology Stack）を使った、無数の車が飛び交う超巨大都市のデモ。100人以上のマルチプレイもサポートした最新公式サンプルです。
* **RL動画化の観点**: 
  * 圧倒的なサイバーパンク感と都市のスケール感を誇ります。
  * （ゲームロジックは使わず）広大な環境のみを流用し、「何千もの空飛ぶ車を避けながら目的地まで飛行するドローンAI」などを学習・撮影することで、映画のワンシーンのようなシミュレーション動画が作れます。

## 3. シミュレーション・ロボティクス系

### Unity Robotics Hub / Articulation Body Samples
* **ライセンス**: Apache License 2.0 / UCL
* **リンク**: [GitHub (Robotics Hub)](https://github.com/Unity-Technologies/Unity-Robotics-Hub)
* **概要**: 産業用ロボットアーム、AGV（自動搬送車）、ベルトコンベアなどの専門的なロボティクスシミュレーションに向けた公式パッケージ。
* **RL動画化の観点**: 
  * 「AI研究デモ動画」としての見栄えに特化しています。
  * ゲームキャラではなく「物理的に妥当な関節を持つロボット」が、最初はメチャクチャな動きをしつつも徐々に整った歩行やピッキングを獲得していく動画は、海外のAI・シミュレーション系チャンネルでも鉄板の伸びるコンテンツです。

### ML-Agents Sample Environments
* **ライセンス**: Apache License 2.0
* **リンク**: [GitHub (ML-Agents Examples)](https://github.com/Unity-Technologies/ml-agents/tree/main/Project/Assets/ML-Agents/Examples)
* **概要**: ML-Agents公式リポジトリに同梱されている、3D Balance Ball、Walker（二足歩行）、Soccer、Push Blockなどの標準化された学習環境群。
* **RL動画化の観点**: 
  * **独自のシナリオを作る際の「辞書」として役立ちます**。自前でScenario Specを考える前に、このサンプル群にあるReward（報酬）やRaycast（視覚センサー）の配置を真似ることで、極端に学習に失敗する確率を減らすことができます。

---

### 総括（当プロジェクトへの適用ガイド）
動画としての見栄え（視認性）の確保が最優先である当プロジェクトにおいて、これらのアセットは「無料・高品質・高安定性」の3拍子が揃っています。
まずは **「Starter Assets」** を下敷きにして「人間型AIの動作」から着手し、背景に **「3D Game Kit」** などのリッチな3Dモデルを配置して動画化する手法が、最もコストパフォーマンス良く「視聴者に刺さるAI動画」を量産する近道となります。
