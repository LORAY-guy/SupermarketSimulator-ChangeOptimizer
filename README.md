# Change Optimizer

A BepInEx mod for **Supermarket Simulator** that adds a live optimal-change display to the in-game cash register screen, helping you give the right coins and bills quickly.

---

## Overview

When a customer pays with cash, the bottom of the register screen shows the ideal denomination breakdown for the change you need to give back. As you add each coin or bill to the drawer, the display updates in real time to show your progress.

---

## Features

### Optimal change calculation
Uses a greedy algorithm to compute the minimum number of coins/bills needed. Denominations supported (in cents): $50, $20, $10, $5, $1, 50¢, 25¢, 10¢, 5¢, 1¢.

### Live colour feedback
Each denomination group is coloured based on what you've given so far:

| Colour | Meaning |
|--------|---------|
| White  | Not given yet |
| Yellow | Partially given |
| Green  | Exact amount given |
| Red    | Too many given, or denomination not needed at all |

---

## Configuration

The config file is generated at `BepInEx/config/ChangeOptimizer.cfg` on first run.

| Section | Key | Default | Description |
|---------|-----|---------|-------------|
| General | Enabled | `true` | Enable or disable the mod entirely |
| Miscellaneous | HappyMessage | `You're good! :)` | Text shown when no change is needed |
| Miscellaneous | ShowHappyOnExactChange | `true` | Replace the denomination display with the happy message once all change is given exactly |

---

## Requirements

- [BepInEx 6 IL2CPP](https://builds.bepinex.dev/projects/bepinex_be) (6.0.0-be.755 or later)
- Supermarket Simulator (Steam, v1.2.0+)

## Installation

1. Copy `ChangeOptimizer.dll` from `bin/Debug/net6.0/` into `BepInEx/plugins/`.
2. Launch the game.
