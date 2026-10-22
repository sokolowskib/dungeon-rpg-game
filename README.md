# dungeon-rpg-game

A terminal roguelike RPG in C# / .NET. You can play it alone, or several players
can share one dungeon over **TCP**. The game logic, rendering and input are
separate layers, so the same game model runs locally or behind a server.

Written for the Object-Oriented Design course (ProjOb) at the Faculty of
Mathematics and Information Science, Warsaw University of Technology. The main
goal was to apply classic design patterns to a real codebase. In-game text is
in Polish.

## Features

- **Procedurally built dungeons**: rooms, corridors, a central hall, items,
  weapons, artifacts and enemies, set up by a fluent `DungeonBuilder`.
- **Themes** (`minecraft`, `christmas`), set in `config.json`. Each one picks its
  own layout, loot and enemy roster.
- **Turn-based combat** with three attack styles (normal / sneaky / magic). The
  outcome depends on the weapon type and the attack style.
- **Inventory and two hands**: one- and two-handed weapons, pick up, drop,
  equip, swap hands.
- **Socketed items**: some weapons have slots. Gems and handles go into them,
  and handles have slots of their own, so items nest like a tree. A slot editor
  lets you browse and change that tree.
- **Enemy AI**: each species reacts to sight and sound (aggressive, neutral or
  cowardly). It can change behaviour after being hit and reacts differently to
  dying.
- **Event journal**: game events are written to a timestamped log file.
- **Multiplayer over TCP**: up to 9 players share one world. A background tick
  moves the enemies.

## Build and run

Needs the .NET 10 SDK.

```sh
dotnet build

# single player
dotnet run --project Game

# multiplayer
dotnet run --project Game -- --server 5555          # host
dotnet run --project Game -- --client 127.0.0.1:5555  # each player
```

`config.json` in the repository root sets the player name, the theme and the
journal directory:

```json
{
    "PlayerName": "Bohater",
    "DungeonTheme": "minecraft",
    "JournalPath": "./"
}
```

## Controls

| Key | Action |
|---|---|
| `W` `A` `S` `D` | Move |
| `E` / `Q` | Pick up / drop |
| `1`–`5` | Select inventory slot |
| `Tab` | Equip selected item |
| `G` | Switch active hand |
| `F` | Fight the enemy on your tile |
| `1` / `2` / `3` (in combat) | Normal / sneaky / magic attack |
| `Q` (in combat) | Flee |
| `T` | Toggle slot editor (`↑` `↓` move, `Enter`/`←` go in/out, `1`–`5` insert, `Backspace` remove) |
| `P` | Dismiss message |

## Networking

```
 ┌──────────┐  JSON lines   ┌───────────────────────────────┐
 │ client 1 │ ◄───────────► │ server                        │
 └──────────┘   over TCP    │  ├ accept loop (TcpListener)  │
 ┌──────────┐               │  ├ thread per client          │
 │ client 2 │ ◄───────────► │  ├ tick thread (enemies)      │
 └──────────┘               │  └ one GameModel (locked)     │
                            └───────────────────────────────┘
```

- **Thread per client.** Each connection gets its own thread. It reads
  newline-delimited JSON, turns each line into an `ICommand` and runs it on the
  shared `GameModel` while holding a lock.
- **Commands go up, state comes down.** Clients send only player intents
  (`move`, `attack`, `pickup`, …). After every change the server takes a
  snapshot of the world (the `*Dto` classes made by `StateMapper`) and sends it
  to all clients. Clients only draw what they receive.
- **Typed messages.** `ICommand` and `IServerMessage` are polymorphic JSON types
  with a `type` field (`welcome`, `state_update`, `game_over`, `victory`).
  `MessageCodec` encodes and decodes them with `System.Text.Json`.
- **Concurrency.** The model and the list of client connections each have
  their own lock. A tick thread moves the enemies once per second.
  Disconnects remove the player and update the remaining clients.

## Design patterns

| Pattern | Where |
|---|---|
| **MVC** | `GameModel` / `IGameView` + `ConsoleView` / `LocalController`, `NetworkController` |
| **Command** | Every player action is an `ICommand`, so the same code runs locally and arrives over the network |
| **Chain of Responsibility** | `InputHandler`: slot editor → combat → movement → inventory → message handlers turn a key into a command |
| **Visitor** | `IAttackVisitor` / `IDefenseVisitor`: damage depends on both the attack style and the weapon type |
| **Decorator** | `ItemDecorator`: weapon classes, stat bonuses, socket slots wrap base items |
| **Composite** | `Socket` / `SlottedDecorator`: items hold other items in a tree |
| **Builder** | `DungeonBuilder`: fluent dungeon generation |
| **Abstract Factory** | `DungeonTheme` + `DungeonThemeFactory`: each theme supplies its own layout, items and enemies |
| **Strategy** | Enemy sight/sound/death reactions; the journal backend (`IEventJournal`) |
| **Singleton** | `EventJournalManager` |
| **Observer** | `SoundChannel`: enemies are notified of noise around them |
| **DTO** | `Logic/Network/Dto`: serializable snapshots of the model |

## Project layout

```
Game/             entry point: picks local / server / client mode
Logic/
  Controller/     input handling, commands, local & network controllers
  Engine/         config loading
  Model/          game state: combat, entities, items, world, journal
  Network/        TCP server & client, DTOs, message codec
  View/           ANSI console renderer
```
