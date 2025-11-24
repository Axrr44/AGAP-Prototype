# Card Match Prototype

Small card-matching prototype built for a hiring test using Unity 2021 LTS.

## Tech

- Unity 2021 LTS
- UI Toolkit: UGUI + TextMeshPro
- Animations: DOTween
- Platforms: Desktop + Mobile-ready layout

## Gameplay

- Tap cards to flip them.
- Match pairs with the same ID.
- Matched cards stay revealed and disabled.
- Game ends when all pairs are matched.

## Features vs Requirements

- Card flip animation with smooth tweening.
- Continuous card flipping (you can interact with other cards while a mismatch pair is resolving).
- Dynamic board layouts (rows/columns configurable: works with 2x2, 3x3, 4x4, 5x6, etc.).
- Cards scale automatically to fit the board area.
- Score system that increments on successful matches.
- Basic combo-friendly architecture (score handling separated from card logic).
- Sound effects for:
  - Flip
  - Match
  - Mismatch
  - Game over
- Save/Load:
  - Board layout
  - Matched state per card
  - Current score
- Simple Start screen and Game Over screen with retry.

## How to Run

1. Open the project in Unity 2021 LTS.
2. Open the scene: `Assets/AGAP/Scenes/MainScene.unity`.
3. Press Play in the editor.

## Notes

- No warnings or errors in the Console.
- All logic is built from scratch without prebuilt gameplay frameworks or purchased card systems.
- Focus is on clean, testable code and simple visuals rather than art.