# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Project Arc is a bullet-hell / turret-defense Unity game. The player operates a turret via a fan-shaped touch control area in the bottom-right corner of the screen. The core loop: aim turret → fire projectiles → destroy enemy waves → survive/win.

**Engine:** Unity 2022.3 (Tuanjie edition, `m_TuanjieEditorVersion: 1.6.8`)
**Render Pipeline:** URP 14.1.0
**Language:** C# (all scripts under `Assets/_Game/Scripts/`, namespace `ProjectArc.*`)
**Serialization:** Force Text mode (YAML)

## Build & Run

There is no CI/CD or CLI build system. All building and testing happens inside the Unity Editor.

- **Open project:** Open the root folder in Unity Editor (Tuanjie 1.6.8+)
- **Run:** Open `Assets/_Game/Scenes/Boot.scene` and press Play. BootLoader loads `L_Test_01.scene` after a delay.
- **Build:** File → Build Settings (scenes are pre-configured in EditorBuildSettings)
- **Tests:** Unity Test Framework is installed but no project tests exist yet. Run via Window → General → Test Runner.

## Architecture

All game code lives in `Assets/_Game/Scripts/` organized by layer:

### Core (`ProjectArc.Core`)
- **GameManager** — Global singleton (DontDestroyOnLoad). State machine: Boot → Menu → Gameplay → Paused → GameOver.
- **ObjectPoolManager** — Singleton. All runtime spawning goes through this (projectiles, enemies, VFX). No Instantiate/Destroy at runtime. Supports auto-expand and theme-based initialization via `LevelTheme` ScriptableObjects.
- **EventManager** — Static string-keyed event bus for decoupled pub/sub communication.
- **IDamageable** — Interface (`TakeDamage()`, `CurrentHealth`). Used uniformly by enemies, projectiles, and bullet-vs-bullet clash.

### Gameplay (`ProjectArc.Gameplay`)
- **TurretController** — Rotates turret model toward aim direction. Has sector angle clamping (MinAngle/MaxAngle) with a Smart Clamp fix for 0°/360° wrapping.
- **WorldSpaceController** — Raycasts a 3D control pad mesh, computes aim direction, drives TurretController.
- **WeaponSystem** — Multi-slot weapon with auto/manual fire. Spawns via `ObjectPoolManager.Spawn()`.
- **Projectile** — Movement, collision, IDamageable, ricochet/bounce, bullet-vs-bullet clash, VFX spawning on hit.
- **EnemyController** — Linear movement, auto-fire, IDamageable, death VFX, returns to pool on death.
- **LevelManager** — Primary game loop controller. State machine: Loading → Intro → Playing → Victory/Defeat. Drives wave spawning and win condition checks.
- **WaveManager** — Coroutine-based wave spawner using `WaveDefinition` ScriptableObjects.

### VFX (`ProjectArc.VFX`)
- **AutoReturnToPool** — Auto-returns VFX prefab to ObjectPoolManager after lifetime expires.

### Data (ScriptableObjects in `Assets/_Game/Data/`)
- **LevelConfig** — Level metadata, win conditions, wave list.
- **LevelTheme** — Skybox, fog, prefab lists (enemies, projectiles, VFX). Drives ObjectPoolManager initialization.
- **WaveDefinition** — Enemy prefab, count, interval, positioning, wait time per wave.

## Key Design Patterns

1. **Singleton MonoBehaviours** — GameManager, ObjectPoolManager, LevelManager are singletons with DontDestroyOnLoad.
2. **Object Pooling** — Everything is pooled. Never use `Instantiate()` or `Destroy()` for gameplay objects; use `ObjectPoolManager.Spawn()` and return to pool.
3. **ScriptableObject Data-Driven** — Levels, themes, and waves are configured via ScriptableObjects, not hardcoded.
4. **Interface-Based Damage** — `IDamageable` is the single contract for all damageable entities.
5. **Event Bus** — `EventManager` for cross-system communication. String keys, lightweight.
6. **Coroutine Game Loop** — LevelManager uses nested coroutines for wave sequencing.

## Physics Layers

| Layer | ID | Purpose |
|-------|----|---------|
| Controls | 10 | Touch control pad raycasts |
| Player | 12 | Player turret |
| Enemy | 13 | Enemy entities |
| PlayerProjectile | 14 | Player bullets |
| EnemyProjectile | 15 | Enemy bullets |
| Environment | 16 | Static environment |
| PowerUp | 17 | Pickups |
| Graze | 18 | Near-miss detection |

## Scene Flow

```
Boot.scene (BootLoader.cs)
  └─→ L_Test_01.scene (main gameplay test level)
        ├── GameManager (singleton)
        ├── ObjectPoolManager (singleton)
        ├── LevelConfigurator (applies LevelTheme to pool)
        ├── LevelManager (game loop)
        ├── WaveManager (enemy waves)
        └── PlayerTurret (TurretController + WeaponSystem + WorldSpaceController)
```

## Conventions

- All scripts use `ProjectArc.*` namespaces matching their layer (Core, Gameplay, VFX).
- Prefabs are organized under `Assets/_Game/Prefabs/` by type, then by theme (e.g., `ThemeDefault/`).
- VFX prefabs live alongside gameplay prefabs in `Prefabs/VFX/ThemeDefault/`.
- Commit messages are in Chinese, short, and descriptive.
