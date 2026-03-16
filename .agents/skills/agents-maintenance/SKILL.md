---
name: agents-maintenance
description: Maintain the local instruction system for this repository. Use when editing `AGENTS.md`, `.agents/`, local skills, references, workflow aliases, or the overall instruction routing and loading policy.
---

# Agents Maintenance

- 主モードは通常 `Build` として扱う
- `AGENTS.md` は薄いルータとして保つ
- `.agents/core/SKILL.md` に repo 共通ガードレールを集約する
- `.agents/RULES.md` は互換 shim としてのみ扱う
- 既存の知見を capture / merge / prune するだけなら `../lessons-maintenance/SKILL.md` を優先し、ここでは instruction architecture の変更に集中する
- タスク固有手順は `.agents/skills/*/SKILL.md` に置く
- 長い事実、契約、チェックリスト、命名規約は `.agents/references/*.md` に置く
- 共有の出力方針や context 節約方針は `.agents/core/SKILL.md` に 1 回だけ置き、各 skill に重複させない
- 複数 task に効く振り返りは `.agents/references/lessons-learned.md` に置き、常設ルール化は本当に必要なものだけに絞る
- 各 skill は必要な reference を直接リンクし、深い参照チェーンを作らない
- skill 冒頭の reference 指示は常時必須を最小限にし、比較・録画・handoff などの分岐は条件付きにする
- 固有ルールの重複を避け、正本の場所を 1 つに保つ
- `AGENTS.md` に載せる working style は最小限にし、詳細運用を増やしすぎない
- `.agents/workflows/*.md` は legacy alias / checklist に留め、固有ルールを持たせない
- workflow alias に `Minimum inputs` / `Phase gates` / `Done when` のような薄い型を足すのはよいが、固有ルールの重複は避ける
- `.agents/**` は UTF-8 で保つ
- Windows で skill validator を回すときは `PYTHONUTF8=1` を付けて `quick_validate.py` を実行し、既定コードページ由来の読み込み失敗を避ける
- skill の frontmatter は `name` と `description` だけにする
- ルーティングを変えたら `AGENTS.md`、該当 skill、workflow alias、必要な docs を同時に更新する
- コード由来のルールは、書く前に repo 実装で裏取りする
- 説明量や読込量を減らすときは、検証ゲートや safety 条件ではなく、背景説明や重複記述を削る
- 大きな instruction 変更では `../../references/instruction-authoring.md` を読む
- 配置先に迷ったら `../../references/repo-map.md` を読む
