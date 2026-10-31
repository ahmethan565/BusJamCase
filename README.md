# Bus Jam

A 3D mobile puzzle game in the style of Bus Jam, built with Unity. Tap a passenger to send them to a bus of
their color. Passengers whose bus is not at the stop wait in a few slots, and the level is lost when those
slots fill up.

The full design (rules, architecture, every decision and why) is in [`Docs/ARCHITECTURE.md`](Docs/ARCHITECTURE.md),
written in Turkish.

## Getting started

1. Install **Unity 2022.3.19f1** with Unity Hub. Use exactly this version; the project is not upgraded.
2. Clone the repository and add the folder in Unity Hub (**Add → Add project from disk**).
3. Open it. Nothing else needs to be installed:
   - Unity packages download on first open, from `Packages/manifest.json`.
   - DOTween, TextMesh Pro resources and all project settings are already in the repository.

The editor always opens `Assets/_Game/Scenes/Main.unity`. Press **Play** from any scene and the game starts
from the main scene.

To build for Android you also need the **Android Build Support** module. Add it in Unity Hub under
**Installs → 2022.3.19f1 → Add modules**. You do not need it to play in the editor or to run the tests.

## Project layout

| Path | Contents |
|---|---|
| `Assets/_Game/Core` | Game rules, plain C# with no Unity references (`Game.Core`) |
| `Assets/_Game/Runtime` | Views, animation, UI, input, audio and app flow (`Game.Runtime`) |
| `Assets/_Game/Editor` | Level editor, debug menu and the Android build (`Game.Editor`) |
| `Assets/_Game/Tests` | EditMode tests for Core and Editor |
| `Assets/_Game/Content/Levels` | Level assets and the `LevelCatalog` |
| `Assets/_Game/Content/Settings` | Tunable settings assets (see below) |
| `Docs/reference` | A Python reference simulator of the rules |

## Creating a level

### 1. Create the asset

In `Assets/_Game/Content/Levels`, right-click and choose **Create → Game → Level**. Name it in the existing
style, for example `Level_17_MyIdea`.

### 2. Paint it in the Inspector

Select the asset. The Inspector becomes a level editor:

- **Width / Height:** the grid size. Growing adds floor cells; shrinking crops.
- **Palette + grid:** pick a brush, then click or drag on the grid to paint.

  | Brush | Meaning |
  |---|---|
  | `.` Floor | An empty floor cell |
  | `#` Wall | A wall; passengers cannot pass |
  | `-` Void | No cell at all |
  | `R` `G` `B` `Y` `P` `O` | A floor cell with a passenger (Red, Green, Blue, Yellow, Purple, Orange) |

  Passengers leave the board from the **top**. A cell is an exit when every cell above it in its column is
  Void, so the top row is always an exit.
- **Waiting Slots:** how many passengers can wait at the stop. When the last slot fills, the level is lost.
- **Bus Capacity:** passengers per bus.
- **Time Limit (s):** a countdown that starts on the first tap. `0` means no time limit.
- **Buses:** the bus colors in arrival order, for example `RGBRGB`. Spaces are ignored.
  **Auto Fill** adds the right number of buses for each color; reorder them by hand to shape the puzzle.

### 3. Fix the live checks

Below the grid the editor shows **"Level is valid."** or a list of errors. A level is valid when:

- each color has exactly `buses of that color × Bus Capacity` passengers,
- there is at least one passenger, one bus and one exit,
- every passenger can reach an exit if the board were otherwise empty (no closed-off islands).

### 4. Solve and play it

- **Solve** checks that the level can be won. It also plays 1,000 random games and reports the win rate,
  which is useful for comparing how hard levels are.
- **Play This Level ▶** opens the main scene and starts this level directly. The level must be in the catalog.
  Progress from a level started this way is kept in memory only, so trying level 12 does not unlock levels
  1 to 12 in your real save.

### 5. Add it to the catalog

Select `Assets/_Game/Content/Levels/LevelCatalog.asset` and add the level **at the end** of the list. The
Inspector shows a ✅/❌ status for every level.

> Player progress is stored by level index. Once the game is released, only **append** levels; inserting or
> reordering shifts everyone's progress.

### 6. Run the tests

The tests parse, validate and solve every level in the catalog. They also check that each level fits the
pre-warmed object pools, so nothing is created during play:

| Object | Maximum per level |
|---|---|
| Passengers | 40 |
| Cells that are not Void (floor and walls) | 64 |
| Walls | 8 |
| Waiting slots | 8 |

A bigger level needs a larger pool size on the `BoardView`, `GameplayPresenter` or `WaitingAreaView` in
`Main.unity`.

## Editor tools

Everything is under **Tools → Game**:

| Menu item | What it does |
|---|---|
| **Unlock All Levels** | Marks every level except the last as completed, so every level on the map is open. |
| **Reset Progress** | Deletes the saved progress; the game starts again from level 1. |
| **Configure Development Build** | Turns on Development Build and Autoconnect Profiler for profiling on a phone. |
| **Build Android APK (Development)** | Applies the Android settings and builds `Builds/Android/BusJamCase-dev.apk`. |

**Unlock All Levels** and **Reset Progress** only work outside Play mode. During play, the game keeps
progress in memory and would overwrite the change with its next save.

Install the APK on a phone connected with USB debugging:

```bash
adb install -r Builds/Android/BusJamCase-dev.apk
```

Development builds show an FPS counter in the top-right corner. Release builds remove it.

## Tuning without code

Values that shape the game feel are in ScriptableObjects in `Assets/_Game/Content/Settings`, so you can tweak
them in the Inspector (even during Play mode):

| Asset | Controls |
|---|---|
| `AnimationSettings` | Every duration, speed and bounce of the animations |
| `LayoutSettings` | Board, stop and bus lane spacing |
| `ColorPalette` | Passenger and bus colors |
| `SoundLibrary` | Which clip and volume each sound uses |

## Tests

Open **Window → General → Test Runner**, choose **EditMode** and press **Run All**.

The suites:
- **`Game.Core.Tests`** covers the rules, parser, validator, solver and timer.
- **`Game.Editor.Tests`** covers scene wiring, prefabs, settings, the level catalog and the editor tools.

To run the tests from the command line, close the editor first:

```bash
"C:/Program Files/Unity/Hub/Editor/2022.3.19f1/Editor/Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults results.xml
```

## Reference simulator

`Docs/reference/bus_jam_sim.py` implements the same rules in Python, with no dependencies. It is used to
check expected results:

```bash
cd Docs/reference
python3 -c "from bus_jam_sim import *; from levels import LEVELS; print(solve(LEVELS['L4']))"
```
