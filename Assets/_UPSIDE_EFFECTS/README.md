# UPSIDE EFFECTS - Prototype HDRP

Prototype first-person de climb vertical orienté malaise sensoriel (pas horror classique), construit pour Unity HDRP uniquement.

## Direction
- Jeu de plateforme vertical lisible au départ, de plus en plus agressif visuellement.
- Signature: surcharge perceptive, vertige, instabilité caméra, mensonges UI, faux chemins.
- Boucle de run: montée -> chute/respawn checkpoint -> reprise rapide -> goal.

## Architecture (hierarchy-first)
Le prototype est pensé pour être **édité dans la hiérarchie Unity**, pas en génération runtime.

Hiérarchie cible:
- `UPSIDE_EFFECTS_Prototype`
- `Systems`
- `PlayerRig`
- `World`
- `Effects`
- `UI`
- `Checkpoints`
- `Lighting`
- `Debug`

La génération automatique ne crée des objets **qu'en Editor** pour produire de vrais GameObjects dans la scène.

## Génération de la scène (Editor)
Menu:
- `Tools > UPSIDE EFFECTS > Build Or Refresh In Games Scene`

Le builder crée:
- met à jour `Assets/Scenes/Games.unity` (root `UPSIDE_EFFECTS_Prototype`)
- matériaux HDRP temporaires (`M_BaseNeutral`, `M_NeonToxic`, `M_DeepBlack`, `M_GlowPink`, `M_GlowCyan`)
- profil Volume HDRP global
- setup Custom Pass fullscreen HDRP
- parcours vertical jouable + moving/fake platforms + checkpoints + goal zone + UI run.

## Scripts clés
- Core:
  - `HeightProgressionManager` (progression 0..1, stages, distribution des intensités)
  - `RunSessionManager` (timer, chutes, checkpoints, fin de run)
  - `GameManager`, `AudioManager`
- Player:
  - `FirstPersonMotor`, `FirstPersonLook`, `PlayerFallRespawn`
- Checkpoints:
  - `Checkpoint`, `CheckpointManager`
- Effects:
  - `PsychedelicVolumeController` (overrides HDRP Volume)
  - `PsychedelicCustomPassController` (fullscreen custom pass HDRP)
  - `CameraSideEffects`, `SideEffectEventDirector`, `AudioIntensityDriver`
- World:
  - `MovingPlatform`, `FakePlatform`, `PlatformFlicker`, `GoalZone`
- UI:
  - `HUDAltitude`, `SideEffectUI`, `CheckpointUI`, `RunHUD`

## Gameplay loop actuelle
1. Départ zone basse, montée guidée.
2. Progression en hauteur augmente l'intensité (volume + custom pass + caméra + événements).
3. Chute sous kill height -> respawn dernier checkpoint (sans reload de scène).
4. Goal zone en haut -> fin de run et résumé (temps, chutes, checkpoints), restart `R`.

## Tuning rapide recommandé
- Sensation de mouvement:
  - `FirstPersonMotor`: `moveSpeed`, `jumpVelocity`, `gravity`, `airControl`
- Pente d'agression visuelle:
  - `HeightProgressionManager`: `minHeight`, `maxHeight`, `progressionCurve`, thresholds
  - `PsychedelicVolumeController`: ranges + curves
  - `PsychedelicCustomPassController`: warp/rgb/scan curves
- Fréquence des événements mentaux:
  - `SideEffectEventDirector`: cooldown + poids par stage

## Important: ancien système RGB Brainrot
L'auto-injection runtime legacy est désactivée par défaut pour garder un pipeline propre orienté scène/hierarchy.

Si vous voulez le réactiver explicitement:
- ajouter le define `RGB_BRAINROT_LEGACY_AUTOINJECT` dans les Scripting Define Symbols.

## Notes HDRP
- Pas d'API URP.
- Effets gérés via `Volume` HDRP + `CustomPassVolume` HDRP.
- Shader fullscreen custom pass: `Shaders/PsychedelicFullScreen.shader`.

## Input (New Input System)
- Contrôles gameplay pilotés via `InputSystem_Actions.inputactions` (map `Player`).
- Actions utilisées:
  - `Move`
  - `Look`
  - `Jump`
- Compatibilité:
  - clavier/souris
  - manette (left stick move, right stick look, south button jump)
- Composants:
  - `PlayerInput` sur `PlayerRoot`
  - `UpsidePlayerInputRouter` comme couche de lecture centralisée
