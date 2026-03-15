# Reactor Core Delivery Direction

## Concept

`Reactor Core Delivery` は、人間型の単体エージェントが発光コアを搬送し、
研究所の危険区画を抜けて炉心ソケットまで運び込むシナリオ案です。
見た目は大規模なSF施設での緊急任務ですが、学習の芯は
`危険地帯を見て、安全なタイミングと経路を選び、ゴールへ到達する` に絞ります。

## Why This Idea

- 1本目として学習ループが素直で、実装が膨らみにくい
- 回転レーザー、帯電床、開閉バリアだけでも高度な任務に見える
- 成功も失敗も分かりやすく、動画でルールが伝わりやすい
- 無料アセットで研究所の密度感、発光演出、緊急感を盛りやすい

## Core Fantasy

視聴者に見せたいのは、
小さな人型エージェントが危険な未来施設の中で
「わずかな隙を読んで重要物資を届け切る」瞬間です。

映像としては:

- 赤い警告灯の中でコアを抱えて走る
- レーザーの切れ目で一瞬止まり、次の区画へ滑り込む
- 失敗すると感電やノックバックでコア搬送が崩れる
- 成功時は炉心が起動して空間全体が光る

## Recommended First Version

V1 は、1ループが明快な短中距離搬送に絞る。

- 単一の研究所通路アリーナ
- スタート地点でコアを取得済み、またはすぐ拾える配置
- 危険要素は 2〜3 種だけに制限
- ゴールは 1 つの明確な炉心ソケット
- エピソードは短めで、失敗時は即リセット

V1 の危険要素候補:

- `Sweeping Laser`: 一定速度で往復する可視レーザー
- `Shock Floor`: 一定周期で通電する床
- `Blast Door`: 開閉タイミングだけ読む必要がある遮断扉

## Why This Scope Is Important

初回から「探索」「長距離ルート」「複数目的地」を入れると、
学習も調整も一気に重くなります。

この案の面白さは、
`危険の可視化`
`タイミング判断`
`安全ルート選択`
`ゴール直前の緊張感`
にあります。

まずは短い任務をしっかり成立させる方が、
動画としても、次段階の拡張としても強いです。

## Emergence Levers

意外な動きを引き出したいなら、次の競合を仕込む。

- `Speed vs Safety`: 最短距離を取るか、安全な遠回りを取るか
- `Commit vs Wait`: 早めに突っ込むか、1周期待つか
- `Lane Choice`: 危険種別の違うレーンをどちらで抜けるか
- `Recovery`: 軽いノックバック後に立て直すか、そのまま押し切るか

## Visual Hooks

- 発光コアが常に画面の主役になる
- 無料SFアセットで配管、端末、発光床、警告灯を密に置ける
- レーザーや通電床で危険がひと目で分かる
- 成功時に炉心が起動し、空間が一気に明るくなる
- 失敗時も「危険施設に弾かれた」絵になる

## Asset Plan

今回の案は、無料アセット込みで豪華さを出す前提で確定した。
2026-03-14 時点の前提では、まず次の 4 つを採用候補の正本とする。

- `FREE Demo: Low Poly Sci-Fi Station / Cosmic Retro`
- `Creative Characters FREE - Animated Low Poly 3D Models`
- `Human Basic Motions FREE`
- `Simple FX - Cartoon Particles`

参考リンク:

- [`FREE Demo: Low Poly Sci-Fi Station / Cosmic Retro`](https://assetstore.unity.com/packages/3d/environments/sci-fi/free-demo-low-poly-sci-fi-station-cosmic-retro-323347)
- [`Creative Characters FREE - Animated Low Poly 3D Models`](https://assetstore.unity.com/packages/3d/characters/humanoids/creative-characters-free-animated-low-poly-3d-models-304841)
- [`Human Basic Motions FREE`](https://assetstore.unity.com/packages/3d/animations/human-basic-motions-free-154271)
- [`Simple FX - Cartoon Particles`](https://assetstore.unity.com/packages/vfx/particles/simple-fx-cartoon-particles-67834)

補助候補:

- `3D Scifi Kit Starter Kit`

補助候補リンク:

- [`3D Scifi Kit Starter Kit`](https://assetstore.unity.com/packages/3d/environments/3d-scifi-kit-starter-kit-92152)

`3D Scifi Kit Starter Kit` は現在 repo 内の
`AI-RL-Movie/Assets/ThirdParty/_Creepy_Cat/` に取り込み済み。
ただし Asset Store 上では `URP not compatible` 表記で、
実際の同梱物にも Built-In / HDRP 向け package や custom shader が含まれる。

そのため本プロジェクトでは、
「主力の土台」ではなく
`通路骨格を足すための補助アセット`
として扱う。

## Asset Usage

各アセットの使いどころは、最初から役割を固定しておく。

- `Cosmic Retro`: 通路壁、配管、制御端末、炉心周辺の背景、警告灯まわりの密度出し
- `Creative Characters FREE`: 主役となる人間型エージェントの見た目ベース
- `Human Basic Motions FREE`: 待機、走行、簡易リアクションなど録画で見栄えを補強する動き
- `Simple FX - Cartoon Particles`: 通電、火花、起動成功、軽い被弾感などの視覚演出
- `3D Scifi Kit Starter Kit`: 通路骨格の壁・床・扉・警告サインを足したい時だけ補助採用する

シーン内での配置意図:

- `スタート区画`: 端末、搬送ラック、静かな照明で「任務開始前」の空気を作る
- `危険区画`: レーザー、通電床、警告灯、火花で危険を読みやすく見せる
- `ゴール区画`: 炉心ソケット、強い発光、成功時の起動演出でクライマックスを作る

`3D Scifi Kit Starter Kit` を使う場所:

- `通路の外殻`: 壁、床、ドア枠、警告サインで「施設そのもの」の骨格を作る
- `危険区画の縁`: danger sign、フェンス、床パネルで危険レーンの輪郭を強調する
- `主役以外の背景`: 当たり判定に関係しない補助壁面や天井まわりの密度出しに限定する

逆に V1 では、次は主役にしない:

- shader 依存が強そうなガラス、発光ライト、空、demo scene 全体
- hazard 判定に直結する床やレーザーの可読レイヤー
- 見た目が崩れた prefab の無理な継続採用

## Import Priority

導入順は、依存度とやり直しコストの低さを優先する。

1. `Cosmic Retro`
2. `Creative Characters FREE`
3. `Human Basic Motions FREE`
4. `Simple FX - Cartoon Particles`
5. `3D Scifi Kit Starter Kit` は取り込み済みだが、scene 採用は URP spot-check 後

この順にすると、まず環境のトーンとキャラ縮尺を固め、
その後で動きと演出を足せる。

実運用では、
`Cosmic Retro で安全に密度を出す`
`_Creepy_Cat で通路骨格を必要箇所だけ補う`
の順がよい。

## Environment Build Policy

Reactor Core Delivery の V1 では、
環境アセットの責務を次のように分ける。

- `Cosmic Retro`: 端末、ロッカー、モニター、箱、制御パネルなどの安全な背景密度
- `3D Scifi Kit Starter Kit`: 壁、床、扉、フェンス、warning sign などの大型通路モジュール
- `自前メッシュ / 自前Collider`: レーザー、通電床、失敗判定、可読性が必要な gameplay 要素

この分離が重要なのは、
URP 互換が怪しい asset をそのまま hazard 本体に使うと、
見た目の崩れが学習課題の可読性まで壊すため。

V1 の基本方針は:

- 通路の輪郭は `_Creepy_Cat` prefab を候補にする
- 通らせたいレーンと危険判定は自前実装で確定する
- `_Creepy_Cat` 側で見た目が崩れた箇所は `Cosmic Retro` またはシンプル geometry に置き換える

## Compatibility Notes

- 本プロジェクトは `URP`
- `Cosmic Retro` は URP 前提で主力候補にしやすい
- `Creative Characters FREE` と `Human Basic Motions FREE` は用途上の相性を優先して採用候補にしているため、import 後に Rig や Avatar の整合確認は必要
- `Simple FX - Cartoon Particles` は演出用で、見た目が合わない場合は一部だけ使う
- `3D Scifi Kit Starter Kit` は `URP not compatible` 表記のため、入れる場合はマテリアル修正コストを見込む
- 取り込み済みの `_Creepy_Cat` には demo scene、audio、custom shader、Built-In/HDRP 向け package も含まれる
- まずは `Walls` `Floors` `Doors` `Symbols` など静的モジュールだけ spot-check し、OK な prefab だけ使う
- `Glass` `Lights` `Skybox` `Camera Profile` `Turn_Move.cs` は V1 の必須依存にしない

## Risks To Avoid In V1

- コア搬送を物理ベースの難しい把持にしない
- 危険種類を増やしすぎない
- 通路分岐を増やしすぎて探索問題にしない
- 暗すぎる画面にしない
- SF演出を増やしすぎて、当たり判定や危険判定が読めなくならない

## Good Next Step

次にやるなら、これを `scenario-spec` として落とし、
観測、行動、報酬、失敗条件、ランダム化、カメラでの見せ方を定義する。

その際は特に、
`V1 は搬送の見た目を保ちながら、実際の学習は堅いナビゲーション課題にする`
方針を守るのが重要です。
