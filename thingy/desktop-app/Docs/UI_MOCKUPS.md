# OpenMacro Swift WPF UI Mockups

These mockups define the intended layout before polishing individual controls and connecting live runtime data.

## Shell

```text
+--------------------------------------------------------------------------------+
| OpenMacro Swift                00:00:00   Stopped   Up to date   Hotkey F6  Start |
+--------------------------------------------------------------------------------+
| OpenMacro         | General                                                        |
| Swift control     | Runtime status and core controls.                              |
|                  |                                                                |
| [icon] General   | + State -----+ + Runtime ---+ + Hotkey ----+ + Rod Slot --+    |
| [icon] Fishing   | | Stopped    | | 00:00:00  | | F6         | | 1          |    |
| [icon] Addons    | +------------+ +------------+ +------------+ +------------+    |
| [icon] Automation|                                                                |
| [icon] Hunt      | + Quick Controls ------------------------------------------+    |
| [icon] Account   | | Start  Stop  Tracking Mode  Casting Mode  Rod Slot       |    |
| [icon] Settings  | +----------------------------------------------------------+    |
+--------------------------------------------------------------------------------+
| 12:00:00 System ready     12:00:01 Awaiting macro start                         |
+--------------------------------------------------------------------------------+
```

## Fishing

```text
Fishing
Tracking, casting, rod management, and session statistics.

+ Tracking ----------------+   + Casting -----------------+
| Hybrid                   |   | Normal                   |
+--------------------------+   +---------------------------+

+ Rod Management ----------+   + Statistics --------------+
| Slot 1                   |   | Caught 0  Lost 0  SR 100% |
| Masterline text when set |   +---------------------------+
+--------------------------+
```

## Automation

```text
Automation
Select a module to configure.

+ module list ----------+   + selected module configuration ----------------------+
| Auto Angler           |   | Title, description, enable toggle, target fields     |
| Enchant               |   | mode picker, search/target inputs, tuning controls   |
| Appraise              |   +-----------------------------------------------------+
| Treasure Appraise     |
+-----------------------+
```

## Hunt Detect

```text
Hunt Detect
Search, select, and route hunt notifications.

[ search targets ]

[ Ancient Kraken ] [ Phantom Leviathan ] [ Storm Serpent ] [ Moonray ]
[ Aurelion Shark ] [ Abyssal Maw       ] [ Frost Warden  ]

+ Discord Webhook -----------------------------------------+
| webhook input                                            |
| Unselect All                                             |
+----------------------------------------------------------+
```
