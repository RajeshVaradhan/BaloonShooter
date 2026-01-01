# BaloonShooter (Unity 2022.3)

This workspace contains a simple but extensible 2D balloon shooting game scaffold (spawner + balloon variants + scoring/combos + HUD).

## Quick Start
1. Open the project in **Unity 2022.3.x** (LTS).
2. Let scripts compile (TextMeshPro may prompt to import essentials — accept).
3. Auto-setup runs once on first open; or manually run `Balloon Shooter/Setup or Update Game`.
4. Open `Assets/BalloonShooter/Scenes/BalloonShooter.unity`.
5. Press Play.

## Controls
- `LMB`: shoot (raycast from screen center)
- `R`: restart round
- `Esc`: pause/unpause

## Tuning
- `Assets/BalloonShooter/ScriptableObjects/GameConfig_Default.asset`: round length, spawn rate ramp, wind, combo rules, max balloons.
- `Assets/BalloonShooter/ScriptableObjects/BalloonType_*.asset`: points, rarity, size range, speed range, material behavior.

## Design Notes
- Requirements doc: `BALLOON_SHOOTER_GAME_REQUIREMENTS.md`
- Setup automation: `Assets/BalloonShooter/Editor/BalloonShooterSetupWizard.cs`
