# Reactor Core Delivery Direction

## 目的

`Reactor Core Delivery` は、暴走直前の炉心コアを回収し、障害を突破しながら最終ソケットまで運ぶシナリオとして再設計する。

目標は次の 3 点を同時に満たすこと。

- 学習として破綻しない
- 動画で見て気持ちいい
- 見た目はリッチでも、学習上のルールは明快

## 現状の問題

旧バージョンは scene 自体は壊れていないが、コース設計が散らかっていた。

- 分岐レーン、合流、サポートカート、最終スラロームが同時に入り、学習の本筋が見えにくい
- `BlastDoor` は脇を抜けられる
- `ShockFloor` は divider 側に事実上の安全帯が残る
- Laser / Shock が agent には効く一方で、運搬対象の core を十分に統制していない
- HUD が常に強く、通常プレイ時の見やすさを損ねる
- カメラがシェル全体を見せすぎて、実際の攻略ラインより外装が目立つ

## V2 の方針

V2 では分岐と補助ギミックを削り、1 本の mandatory spine に集約する。

`Pickup Bay -> Laser Gate -> Shock Gate -> Full-Width Blast Door -> Short Socket Funnel`

この一本ルートの中で、

- core を転がしてもよい
- grab して持ち運んでもよい
- ただし hazard は core の運搬状態そのものに干渉する

という構造にする。

## コース設計

### 1. Pickup Bay

- 開始直後は loose core が pickup dock に置かれている
- agent はまず core に接近し、押し始めるか grab する
- ここは読みやすさ優先で、最初の接触が分かりやすい平場にする

### 2. Laser Gate

- 一本道の corridor 全体を横切る beam にする
- beam は待つか、タイミングを見て抜けるか、分かりやすい判断になるようにする
- beam 接触は agent だけでなく core にも意味を持たせる
- core が当たった場合は release / knockback が起き、雑な carry を許さない

### 3. Shock Gate

- レーン全幅が危険領域になるようにして、恒久的な safe strip をなくす
- shock は単なる減点ではなく transport disruption として機能させる
- 持っている最中に被弾したら forced release
- loose core にも impulse が入り、運搬の再建が必要になる

### 4. Blast Door

- side bypass を完全に潰した full-width gate にする
- 扉前には短い ante-room を置き、待ち判断が動画でも読みやすい構図にする
- 開いている時だけ素直に通せる、明快な timing gate にする

### 5. Socket Funnel

- ゴール直前は短く絞った funnel で締める
- ここでは細かい slalom ではなく、core を安定して seat させることに集中させる
- 成功条件は `core seats stably in ReactorSocket`

## 操作と観測

### Action

- hybrid action
- continuous 2:
  - strafe
  - forward/back
- discrete branch size 3:
  - `0 = no-op`
  - `1 = grab`
  - `2 = release`

toggle ではなく explicit command にすることで、policy の挙動を安定させる。

### Core Carry

- `CarryAnchor` を agent 側に持つ
- core は rigidbody のまま維持する
- 持っている時は joint で拘束する
- release 後はすぐに通常物理へ戻る
- shock 時は auto-release する

### Observation

vector observation は 39 に絞る。

- agent / core / socket の相対情報
- current mandatory checkpoint
- laser / shock / door の位相
- dock readiness
- countdown
- shock recovery
- `isHoldingCore`
- `grabCooldown01`
- gate progress

support cart の観測は削除する。

## 報酬設計

報酬は「正しい本筋」に沿って整理する。

- approach reward
- core-to-socket progress reward
- dock readiness reward
- core seat bonus
- success + time bonus
- laser, shock, lost core, timeout の明確な penalty
- core が laser / shock / blast-door の 3 gate を順に越えた時の bonus

サポートカート由来の報酬や zone ロジックは削除する。

## Curriculum

lesson は 3 段階にする。

### PickupAndGrab

- core へ近づく
- 押す / grab する
- pickup bay を安定して出る

### MandatoryGates

- laser
- shock
- blast door

この 3 つを順番に突破する流れを学ばせる。

### DeliveryPressure

- timing を本番寄りにする
- deadline を詰める
- 最後の dock まで安定して完走させる

`support_prop_jitter` は削除し、randomization は spawn jitter と 3 gate の timing 系に限定する。

## ビジュアルと録画

### Scene

- 外装シェルは残しつつ、実際の playable corridor は scenario-owned collider で細くする
- 視線が route に自然と乗るよう、床ラインと frame を整理する
- props は off-lane dressing に留め、攻略ノイズにはしない

### Camera

- default view は corridor 内部の high oblique
- recording は 2 follow cuts に絞る
- 外装よりも courier と glowing core が常に主役に見えることを優先する

### HUD

- 通常時は corner countdown だけ
- critical phase または meltdown 時だけ siren 演出を強める
- 常時フル画面警報はやめる

## 受け入れ基準

- heuristic で roll only と grab/release carry の両方で 1 回ずつクリアできる
- core は laser, shock, blast door を順に越えないと socket に届かない
- laser contact with core が意味を持つ
- shock が held transport を壊せる
- blast door に side bypass がない
- reset を繰り返しても courier / core / hazard phase / hold state がきれいに戻る
- `Validate Current Scenario` が通る
- `Build for Colab (Current Scene)` に最新 manifest / config が含まれる
- `Inference Only` 録画で courier と core が読みやすく、debug UI が出ない
