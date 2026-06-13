# Gravity Defied — .NET 10 / raylib-cs port

A faithful port of the J2ME game *Gravity Defied – Trial Racing* (originally a
MIDP-1.0 MIDlet, `354_trial_racing_gr.jar`) to .NET 10 using
[Raylib-cs](https://github.com/raylib-cs/raylib-cs).

The original was decompiled, and each class was transcribed into C# keeping the
game logic as close to the original as possible. In particular the **physics**
is a line-for-line port of the original fixed-point (16.16) simulation, so the
bike handling matches the original. Only the platform layer was adapted: the
J2ME `Canvas`/`Graphics`/threading model was mapped onto raylib's single-threaded
frame loop, and the MIDP `RecordStore` highscore storage was replaced with a
local file.

## Build & run

```sh
cd GravityDefied
dotnet run -c Release
```

Requires the .NET 10 SDK. Assets (`*.png`, `levels.mrg`) are copied to the
output `Assets/` folder automatically.

Extra modes:

```sh
dotnet run -- selftest   # headless: steps physics across all 30 levels, reports stability
dotnet run -- shot       # writes shot_menu.png / shot_game.png and exits
```

## Controls

| Key | Action |
|-----|--------|
| Arrow Up | accelerate |
| Arrow Down | brake |
| Arrow Left / Right | lean back / forward |
| `1 3 7 9` (and keysets 2/3) | combined accel/brake + lean (see Options → Input) |
| Enter / Space | menu select |
| Arrows | menu navigate |
| Esc / Backspace | menu back; in game: open pause menu |

## Class mapping (original obfuscated name → C# type)

| Original | C# | Role |
|----------|----|------|
| `d` | `FpMath` | 16.16 fixed-point sin/cos/atan/atan2, mul, div |
| `n` (physics) | `Node` | point-mass state; reused as a spring |
| `k` | `Body` | rigid body holding 6 integration states |
| `b` | `Bike` | multi-body bike simulation (midpoint integration + collision) |
| `l` | `Level` | terrain polyline + rendering |
| `f` | `Levels` | `levels.mrg` loading, view window, terrain collision |
| `i` | `Renderer` | Canvas → raylib: primitives, sprites, HUD, input |
| `e` | `MenuList` | scrollable selectable list |
| `g` | `Selector` | value-cycling / submenu menu item |
| `h` | `Label` | (wrappable) text item |
| `n` (UI) | `MenuLink` | navigate-to-submenu item |
| `c`, `j` | `IMenuCallback`, `IMenuItem` | menu interfaces |
| `m` | `Menu` | menu state machine + game flow |
| `a` | `Highscores` | best times (file-backed instead of RecordStore) |
| `Micro` | `Game` | main loop / app lifecycle |
| — | `Program` | entry point |
| — | `BeReader` | big-endian reader for `levels.mrg` |

## Notes on faithfulness

- **Physics**: transcribed exactly, including the per-league engine parameters,
  spring layout, gravity, friction, the adaptive substep/collision-resolution
  loop and wheel-rotation integration. The simulation is bit-comparable in
  structure to the original.
- **Levels**: `levels.mrg` is parsed with the original binary format (3 groups ×
  10 tracks, relative/absolute polyline deltas) and the same coordinate scaling.
- **Rendering**: both the sprite path (engine/fender/helmet/driver sheets) and
  the vector path are ported; sprite-frame selection from angle matches.
- **Simplified vs. original**: the progression *unlock* gating is relaxed (any
  track/league is selectable immediately); highscore name-entry uses a default
  name; help/about screens are short functional summaries. None of this affects
  the driving model.
