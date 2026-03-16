# Voice-Friendly Copy

Write spoken lines that sound clean in neutral TTS.
Assume the user may use ElevenLabs or another specialized voice tool, but do not depend on vendor-specific features.

## Sentence Design

- Keep most lines short.
- Prefer one idea per sentence.
- Use concrete nouns and active verbs.
- Split long thoughts into two sentences instead of stacking clauses.

## Pronunciation Safety

- Write out numbers in words when rhythm matters.
- Expand abbreviations on first use if they may read awkwardly.
- Avoid slashes, symbols, camelCase, file names, and config keys in spoken copy.
- Replace punctuation-heavy text with natural language.

## Rhythm

- Use periods for hard beat breaks.
- Use commas for small pauses.
- Use ellipses rarely.
- Do not rely on all caps for emphasis.
- Put the important word near the end of the sentence when emphasis matters.

## Humor Under TTS

- Favor contrast, understatement, and clean reversals over rapid-fire wordplay.
- Give the punchline its own sentence when possible.
- Do not write tongue twisters for a joke.

## Separation Of Concerns

- Keep spoken lines separate from text-card copy.
- Keep edit notes separate from spoken lines.
- If a pronunciation note is needed, label it outside the script line.

## Default Cleanup Pass

Before finalizing, remove or rewrite:
- stacked adjectives
- three-part clauses in one breath
- RL jargon that the footage already explains
- any sentence that feels hard to say once out loud
