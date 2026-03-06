# Session Context Policy (Tank Royale)

## Problem
When multiple sessions use the same agent type, context can feel mixed if:
- sessions use generic labels,
- shared memory files are overwritten,
- tasks are broad and not file-scoped.

## Policy

### 1) Namespace every session
Use explicit session keys:
- `agent:royale-gm:feel-lab`
- `agent:royale-gm:combat-loop`
- `agent:royale-gm:ui-meta`

Never use one catch-all thread for unrelated workstreams.

### 2) One source of truth per workstream
For each track keep:
- `/.openclaw/workspace/royale-gm/contexts/<track>.md`
- `/.openclaw/workspace/royale-gm/handoffs/<track>.md`

Context file = current state.
Handoff file = latest branch/files/next steps.

### 3) Task prompts must include hard scope
Every delegation prompt should include:
- target branch name
- exact file paths
- expected output format
- done criteria

### 4) Keep cross-session memory explicit
If one session needs knowledge from another, copy it into the track context file.
Do not assume automatic cross-session transfer.

### 5) Use sub-agents for tightly coupled work
Sub-agents inherit parent task prompt and are less likely to drift than separate top-level sessions.

## Quick command examples
- Start a focused session:
  - `openclaw tui --session agent:royale-gm:feel-lab`
- Switch to progression:
  - `openclaw tui --session agent:royale-gm:progression`

## Practical rule
Treat sessions like git branches: small scope, explicit name, clear handoff.
