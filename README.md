# MINDRIFT

Prototype Unity with a psychological/cyberpunk vibe.

## Environment

- Unity: `6000.3.10f1`
- Target: PC (keyboard/mouse + controller)

## Quick Start

1. Open the project in Unity.
2. Verify scenes in `Build Settings`:
   - `Assets/Scenes/MainMenu.unity`
   - `Assets/Scenes/Games.unity`
   - `Assets/Scenes/Break.unity`
3. Press Play from `MainMenu`.

## Scene Flow

- `MainMenu`: title screen (`Play / Options / Quit`).
- `Games`: main gameplay scene.
- `Break`: pause menu scene (loaded additively during gameplay).

## Core Rule

- Player lives are hard-capped at `3` (cannot exceed 3 in runtime or inspector).

## Controls

### MainMenu

- Keyboard: `Arrows`/`WASD` to navigate, `Enter` to confirm.
- Controller: `D-Pad`/stick to navigate, `A` to confirm.

### Gameplay / Pause

- `Esc` (keyboard) or `Start` (controller): pause / resume.

## Main UI Scripts

- `Assets/_MINDRIFT/Scripts/UI/MainMenuController.cs`
  - Main menu logic, button binding, and menu navigation.
- `Assets/_MINDRIFT/Scripts/UI/OptionsMenuController.cs`
  - Builds and manages the options panel.
- `Assets/_MINDRIFT/Scripts/UI/MenuSelectableFeedback.cs`
  - Visual feedback for selectable UI elements.
- `Assets/_MINDRIFT/Scripts/UI/GameplayPauseController.cs`
  - Pause scene loading and return-to-menu flow.

## Current MainMenu State

- Stronger visual hierarchy (title, subtitle area, framed center panel).
- Explicit `Play / Options / Quit` block with improved readability.
- Clearer `hover/selected/pressed` feedback.
- Better UI/background separation (overlay + vignette + subtle accents).
- Footer navigation hints integrated in scene.

## Project Note

Polish passes are done with strict scope (example: `MainMenu` only) to avoid gameplay regressions.
