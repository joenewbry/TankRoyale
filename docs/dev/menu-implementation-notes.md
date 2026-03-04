# MENU-101 Implementation Notes (Kickoff)

This document captures initial Unity scene wiring for the menu shell controllers added in MENU-101.

## Added Scripts

- `Unity/TankRoyale/Assets/Scripts/Menu/MainMenuController.cs`
- `Unity/TankRoyale/Assets/Scripts/Menu/SettingsMenuController.cs`
- `Unity/TankRoyale/Assets/Scripts/Menu/InputRebindController.cs`

## Suggested Scene Hierarchy (Example)

Create a `MainMenu` scene with a root object similar to:

- `Canvas`
  - `MainMenuPanel`
    - Play Button
    - Settings Button
    - Quit Button
  - `SettingsPanel`
    - Back Button
    - Master Volume Slider
    - Fullscreen Toggle
    - Quality Dropdown
    - Rebind UI container (optional in kickoff)
- `MenuControllers`
  - `MainMenuController` component
  - `SettingsMenuController` component
  - `InputRebindController` component (optional placement)

## Wiring: MainMenuController

Attach `MainMenuController` to a scene object and set:

- `Gameplay Scene Name` = target gameplay scene (default is `Gameplay`)
- `Main Menu Panel` = `MainMenuPanel`
- `Settings Panel` = `SettingsPanel`

Button callback bindings:

- Play Button -> `MainMenuController.OnPlayPressed()`
- Settings Button -> `MainMenuController.OnSettingsPressed()`
- Quit Button -> `MainMenuController.OnQuitPressed()`
- Settings Back Button -> `MainMenuController.OnBackToMainMenuPressed()` **or** `SettingsMenuController.OnBackPressed()`

## Wiring: SettingsMenuController

Attach `SettingsMenuController` and set:

- `Settings Panel` = `SettingsPanel`
- `Main Menu Panel` = `MainMenuPanel`

UI callback bindings:

- Master Volume Slider (`OnValueChanged(float)`) -> `SettingsMenuController.OnMasterVolumeChanged(float)`
- Fullscreen Toggle (`OnValueChanged(bool)`) -> `SettingsMenuController.OnFullscreenToggled(bool)`
- Quality Dropdown (`OnValueChanged(int)`) -> `SettingsMenuController.OnQualityLevelChanged(int)`
- Back Button -> `SettingsMenuController.OnBackPressed()`

### Notes

- Settings currently persist via `PlayerPrefs`.
- Audio setting is a stub; TODO exists for AudioMixer integration.
- On scene start, saved fullscreen/quality are applied automatically.

## Wiring: InputRebindController

Attach `InputRebindController` and populate `Bindings` list in Inspector.

Example binding entries:

- actionId: `MoveUp`, defaultKey: `W`
- actionId: `MoveDown`, defaultKey: `S`
- actionId: `Fire`, defaultKey: `Space`

Typical UI flow:

1. Rebind button click calls `BeginRebind("Fire")`
2. Controller listens for next key press in `Update()`
3. Selected key is persisted to `PlayerPrefs`

Optional helper hooks:

- Show current binding text using `GetBindingDisplay(actionId)`
- Add a Reset button -> `ResetAllBindings()`

## TODO Follow-Ups (post-kickoff)

- Replace legacy key capture with Unity Input System rebinding operations.
- Add validation for duplicate/conflicting key binds.
- Add menu transition animations and async loading for gameplay scene.
- Connect master volume to AudioMixer and update UI at runtime.
- Add play mode tests for menu navigation + persistence behavior.
