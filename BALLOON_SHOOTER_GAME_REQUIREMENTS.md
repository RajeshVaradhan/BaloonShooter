# Balloon Shooter (Unity 3D) — Game Requirements

## Core Loop
- Aim (mouse) and shoot (LMB) balloons across a “shooting gallery” lane.
- Balloons spawn at varying **distances** and **sizes**, drift upward with **wind**, and despawn if missed.
- Pops award points; misses reduce combo/multiplier; game ends when time runs out.

## Balloon Variety (Size / Distance / Material)
Each balloon instance is generated from a `BalloonType` definition:
- **Size tiers**: small (hard), medium, large (easy). Smaller = more points.
- **Distance tiers**: near, mid, far. Farther = more points.
- **Material archetypes** (affects toughness + scoring + VFX):
  - **Latex (Normal)**: 1 hit to pop; baseline points.
  - **Metallic**: 2 hits to pop; higher points; subtle “ping” feedback.
  - **Glass**: 1 hit; high points; “shatter” particles.
  - **Bomb**: 1 hit; pops nearby balloons (chain scoring reduced).
  - **Splitter**: 1 hit; splits into 2 smaller balloons (lower-value offspring).
  - **Golden (Rare)**: 1 hit; big bonus and extends timer slightly.

## Scoring & Combos
- **Points** = base (type) × size factor × distance factor × combo multiplier.
- **Combo window**: consecutive hits within a short time increases multiplier (caps).
- **Miss penalty**: resets or reduces combo; accuracy shown on HUD.

## Difficulty & Progression
- Spawn rate ramps up during the round.
- Rare/special balloons become more common later.
- Wind changes direction/strength over time to keep aiming dynamic.

## Player Feedback
- Crosshair and hit indicator.
- Pop VFX (particles) and optional SFX hooks.
- End-of-round summary: score, accuracy, best combo.

## Controls
- `LMB`: shoot
- `R`: restart round
- `Esc`: pause/unpause

## Tuning Workflow
- All gameplay tuning lives in ScriptableObjects:
  - `GameConfig` (round, difficulty, wind, combo)
  - `BalloonType` (materials, hit points, rarity, points, size/speed ranges)

