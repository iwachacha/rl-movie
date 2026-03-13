---
name: agents-maintenance
description: Maintain the local instruction system for this repository. Use when editing `AGENTS.md`, `.agents/`, local skills, references, workflow aliases, or the overall instruction routing and loading policy.
---

# Agents Maintenance

- 主モードは通常 `Build` として扱う
- `AGENTS.md` は薄いルータとして保つ
- `.agents/core/SKILL.md` に repo 共通ガードレールを集約する
- `.agents/RULES.md` は互換 shim としてのみ扱う
- タスク固有手順は `.agents/skills/*/SKILL.md` に置く
- 長い事実、契約、チェックリスト、命名規約は `.agents/references/*.md` に置く
- 各 skill は必要な reference を直接リンクし、深い参照チェーンを作らない
- 固有ルールの重複を避け、正本の場所を 1 つに保つ
- `.agents/workflows/*.md` は legacy alias / checklist に留め、固有ルールを持たせない
- `.agents/**` は UTF-8 で保つ
- skill の frontmatter は `name` と `description` だけにする
- ルーティングを変えたら `AGENTS.md`、該当 skill、workflow alias、必要な docs を同時に更新する
- コード由来のルールは、書く前に repo 実装で裏取りする
- 大きな instruction 変更では `../../references/instruction-authoring.md` を読む
- 配置先に迷ったら `../../references/repo-map.md` を読む
