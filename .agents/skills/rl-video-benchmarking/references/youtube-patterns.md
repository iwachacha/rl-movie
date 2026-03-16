# RL / AI Video Benchmark Patterns

Last refreshed: 2026-03-17

Use this file when a request needs precedent from successful RL, AI-learns, or emergent simulation channels.
Use it to decide what kind of scenario, title, thumbnail, or opening cut is worth making in this repo.

## Benchmark Set

### Viral Spectacle

- `Code Bullet`
  - Representative hits: `A.I. learns to play Snake`, `A.I. Learns to WALK`, and `I Created a PERFECT SNAKE A.I.` all broke into the multi-million-view range.
  - Why it works: simple verbs, readable game states, personified failure, and obvious improvement montages.
  - Borrow: `AI learns to <verb>` framing, quick before/after contrast, visible exploits, and comedy from incompetence.
  - Avoid: relying on narration style or meme density as the main hook.

- `SethBling`
  - Representative hit: `MarI/O - Machine Learning for Video Games` reached 11.48M views on 2015-06-13.
  - Why it works: familiar game, instantly understandable success condition, and visible learning generations.
  - Borrow: use familiar rules or toy-like spaces where the audience can read progress immediately.
  - Avoid: assuming famous IP is required. The real transfer is legibility, not nostalgia.

- `Jabrils`
  - Representative angles: `WRITING MY FIRST MACHINE LEARNING GAME!`, `I MADE BETTER AI THAN NINTENDO`.
  - Why it works: creator ownership, playful challenge framing, and clear `I built this / I taught this` stakes.
  - Borrow: let the build itself feel handmade and personal instead of over-polished and generic.
  - Avoid: turning the story into a devlog with no visible training payoff.

- `Pezzza's Work`
  - Representative hits: `AI Learns To Escape (using Reinforcement Learning)` and `Much bigger simulation, AIs learn Phalanx`, both around the 1M-view class.
  - Why it works: clean arenas, fast simulation compression, satisfying many-agent choreography, and strong visual payoffs.
  - Borrow: readable arenas, sped-up generation montages, and big group behavior when formations are the spectacle.
  - Avoid: adding many agents if the viewer loses the protagonist or win condition.

### Polished Simulation Documentary

- `Sebastian Lague`
  - Representative angle: `Coding Adventure: Simulating an Ecosystem`.
  - Why it works: polished simulation visuals, elegant causal explanation, and code serving the spectacle instead of replacing it.
  - Borrow: documentary-style reveal when the simulation itself is beautiful enough to watch.
  - Avoid: front-loading implementation detail before the viewer understands why the system is interesting.

- `Primer`
  - Representative angle: evolution and multi-agent simulations explained as stories.
  - Why it works: minimal but clean visuals, strong narration of cause and effect, and visible rule changes that reshape behavior.
  - Borrow: explain rules through animation and examples rather than ML jargon.
  - Avoid: abstract systems with no visual anchor or emotional stake.

### Trusted Explainer

- `sentdex`
  - Representative angle: deep reinforcement learning tutorials and experiments.
  - Why it works: technical credibility, repeat audience trust, and practical explanations for builders.
  - Borrow: use explainer-style segments for bonus material or behind-the-scenes content.
  - Avoid: using tutorial pacing as the main entertainment format for a broad-audience upload.

- `Two Minute Papers`
  - Representative angle: short-form explainers around emergent AI behavior such as hide-and-seek or locomotion results.
  - Why it works: fast framing of why the experiment matters, compression of emergent behaviors into a short arc, and excitement around surprising strategies.
  - Borrow: highlight the one emergent twist that makes the experiment worth telling.
  - Avoid: making the whole video about the paper or method instead of the on-screen behavior.

- `AI and Games`
  - Representative angle: machine learning in commercial or experimental game AI.
  - Why it works: authority, context, and clear linkage between system design and player-visible behavior.
  - Borrow: use this lane when the audience needs a second layer of explanation after the spectacle is established.
  - Avoid: leading with analysis before the viewer has seen something memorable happen.

## Cross-Channel Win Patterns

1. Put the viewer promise before the method.
Titles and intros sell the verb, the fantasy, or the surprise, not the algorithm.

2. Use tasks that read in a glance.
`walk`, `drive`, `escape`, `hide`, `stack`, `carry`, and `survive` are much stronger than invisible optimization tasks.

3. Give the agent body language.
Legs wobbling, crashes, hesitations, greedy turns, and weird exploits make the policy feel alive.

4. Treat failure as part of the entertainment.
The best videos show crashes, collapses, and dumb strategies because they make eventual success satisfying.

5. Compress progress into visible leaps.
Show a few bad attempts, one intermediate breakthrough, and one clean payoff instead of long unbroken training footage.

6. Build around one memorable twist.
A shield wall, a door block, a broken shortcut, a ridiculous exploit, or an eerie competence spike gives the audience something to retell.

7. Make the first 10 seconds carry the whole premise.
By the end of the cold open, the viewer should understand the actor, the goal, and the danger.

8. Let novelty come from the setup, not only the method.
Most breakout examples are not novel papers. They are clear, funny, or beautiful setups with visible learning.

9. Use contrast aggressively.
Tiny agent vs huge danger, dumb start vs elegant finish, lone struggler vs crowd formation, or calm setup vs sudden disaster.

10. Keep series logic stable and episode fantasy fresh.
Recurring format helps, but each upload still needs a new verb, arena, threat, or emergent gimmick.

## Title And Thumbnail Patterns

- Strong title families:
  - `AI learns to <simple verb>`
  - `I taught an AI to <simple verb>`
  - `Can AI beat / survive / escape <readable challenge>?`
  - `Simulating <creature / ecosystem / battle / weird system>`

- Strong thumbnail ingredients:
  - one protagonist
  - one target or threat
  - one obvious contrast
  - minimal text
  - no graphs unless the graph itself is the spectacle

- Weak packaging signals:
  - algorithm acronyms in the title
  - reward-shaping jargon
  - too many tiny agents with no focal point
  - screenshots where success and failure look identical

## Repo Translation Checklist

Answer these quickly before committing to a scenario:

1. Can a non-RL viewer explain the goal after three seconds?
2. Is the danger visible without debug UI?
3. Is failure funny, tense, or dramatic enough to keep?
4. Can training progress be shown as 3-5 meaningful jumps?
5. Is there one shot that could be the thumbnail already?
6. Is there one exploit or breakthrough worth the title?
7. Can V1 train without invisible reward gymnastics?

Interpretation:
- `6-7 yes`: strong fit
- `4-5 yes`: promising but reframe or simplify
- `0-3 yes`: weak fit for this project's main video lane

## What This Project Should Borrow

- Default to tasks with readable verbs and physical stakes.
- Bias toward one hero agent in a stage the camera can understand.
- Design reset choreography that is still watchable.
- Plan a cold open before the build gets complicated.
- Save detailed ML explanation for after the viewer is already curious.
- Use comparison shots sparingly but deliberately: bad early policy, midpoint, and adopted run.

## What This Project Should Avoid

- Hidden-state tasks whose only proof of progress is a score table.
- Long setup sequences before the fantasy is established.
- Multi-goal rulesets that require narration to understand basic success.
- Videos where the `interesting part` is only that the code was hard.
- Scenarios whose best strategy looks static, cautious, or visually ambiguous.

## Source Links

- Code Bullet channel: https://www.youtube.com/@CodeBullet
- `A.I. learns to play Snake`: https://www.youtube.com/watch?v=tjQIO1rqTBE
- `A.I. Learns to WALK`: https://www.youtube.com/watch?v=LUxWmH-JbgI
- `I Created a PERFECT SNAKE A.I.`: https://www.youtube.com/watch?v=xTgXnDZoYNA
- SethBling channel: https://www.youtube.com/@sethbling
- `MarI/O - Machine Learning for Video Games`: https://www.youtube.com/watch?v=qv6UVOQ0F44
- Jabrils channel: https://www.youtube.com/@Jabrils
- `WRITING MY FIRST MACHINE LEARNING GAME!`: https://www.youtube.com/watch?v=ZX2Hyu5WoFg
- Pezzza's Work channel: https://www.youtube.com/@PezzzasWork
- `AI Learns To Escape (using Reinforcement Learning)`: https://www.youtube.com/watch?v=v3UBlEJDXR0
- `Much bigger simulation, AIs learn Phalanx`: https://www.youtube.com/watch?v=Lu56xVlZ40M
- Sebastian Lague channel: https://www.youtube.com/@SebastianLague
- Primer channel: https://www.youtube.com/@PrimerBlobs
- sentdex channel: https://www.youtube.com/@sentdex
- Two Minute Papers channel: https://www.youtube.com/@TwoMinutePapers
- AI and Games channel: https://www.youtube.com/@aiandgames
