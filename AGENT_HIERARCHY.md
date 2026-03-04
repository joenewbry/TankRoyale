# TankRoyale Agent Hierarchy

## Command Layer
- **GM (arcade-gm)**
  - Owns roadmap, milestones, staffing, release decisions

## Planning Layer (Product Managers)
- **PM-Menu** (agent role: menu product manager)
  - Scope: menus, key bindings, settings, controller/touch UX
- **PM-Gameplay** (agent role: gameplay product manager)
  - Scope: movement, shooting, AI combat loop, win/loss flow
- **PM-Powerups/Progression** (agent role: progression product manager)
  - Scope: pickups, challenge mode (10 levels), balance tuning

## Architecture Layer
- **Tech Architect A/B per PM (shared pool)**
  - Defines subsystem architecture and optimization constraints
  - Reviews performance for WebGL + mobile + console path

## Implementation Layer (Developers)
Each PM can manage up to 4 developers.

- **Menu Squad Devs**
  - Dev M1, Dev M2, Dev M3, Dev M4
- **Gameplay Squad Devs**
  - Dev G1, Dev G2, Dev G3, Dev G4
- **Powerups Squad Devs**
  - Dev P1, Dev P2, Dev P3, Dev P4

Initial mapped agents:
- Dedicated coders: `arcade-dev-01`, `arcade-dev-02`, `arcade-dev-03`
- Overflow coder pool: `arcade-agent-01`..`arcade-agent-10` (assigned per sprint)

## QA + Support
- **QA Engineer**: PR gatekeeper, regression and perf validation
- **Playtester**: fun/difficulty feedback loop
- **Secretary**: milestone updates and executive summaries
- **PR Agent**: launch comms and promotion package

## Workflow Contract
1. GM assigns milestone to a PM.
2. PM decomposes into tickets and assigns devs.
3. Devs implement in branch, open PR.
4. QA validates and either approves or bounces.
5. PM closes ticket; GM assigns next milestone.
6. Secretary sends status update at major milestone completion.
