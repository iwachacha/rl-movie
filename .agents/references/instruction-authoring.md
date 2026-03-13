# Instruction Authoring Rules

- `AGENTS.md` は薄いルータとして保ち、長い手順や例を置かない
- `.agents/RULES.md` は互換 shim として保つ
- repo 共通ガードレールは `.agents/core/SKILL.md` に置く
- タスク固有手順は `.agents/skills/*/SKILL.md` に置く
- 長い事実、契約、命名規約、チェックリストは `.agents/references/*.md` に置く
- skill から reference への導線は 1 hop に保ち、深い参照チェーンを作らない
- 正本の重複を避ける
- `.agents/**` は UTF-8 を使う
- skill の frontmatter は `name` と `description` のみ
- workflow alias は薄く保ち、固有ルールを持たせない
- ルーティングを変えたら `AGENTS.md`、該当 skill、workflow alias、関連 docs を一緒に更新する
- 実装依存のルールは repo 実装で裏取りしてから書く
- 生成物 / 取込物は編集対象と区別して明記する
