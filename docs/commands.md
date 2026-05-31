# Romestead Terminal Dot Commands

Generated: `2026-05-31 08:47:56Z`
Command count: `92`

## How Commands Work

- Registry: `Candide.CandideEngine.AddTerminalCommands()`
- Storage: `Candide.Terminal.GameTerminal.Commands: Dictionary<string, TerminalCommand>`
- Dispatch: `Candide.Terminal.GameTerminal.DoCommand(string) after stripping the leading dot`
- Autocomplete: `Candide.Terminal.GameTerminal.SuggestCommand(string), using Commands.Keys plus per-command suggestion delegates`

## Full Command List

| Command | Usage | Category | Summary | Autocomplete | Handler |
| --- | --- | --- | --- | --- | --- |
| `.all_pois` | `.all_pois [prefix]` | World / map | Spawns all POIs matching an optional prefix while local hosting, including flipped variants. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_107(System.String[])` |
| `.aura` | `.aura <aura-id>` | Player / cheats | Applies an aura to the player. | AuraDataBase.AuraDataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_86(System.String[])` |
| `.beat` | `.beat` | Debug / misc | Starts the beat engine. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_36(System.String[])` |
| `.boss` | `.boss <boss-id>` | Entities / spawning | Requests boss spawn at the player. | BossDataBase.DataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_80(System.String[])` |
| `.buildings_construct_all` | `.buildings_construct_all` | Constructions / placeables | Spawns every base building and upgrade chain in rows near the player. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_106(System.String[])` |
| `.challenge_dungeon` | `.challenge_dungeon <dungeon-id>` | World / map | Requests challenge dungeon spawn. | ChallengeDungeonDataBase.DataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_82(System.String[])` |
| `.chat` | `.chat <message...>` | Networking / chat | Sends chat text. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_44(System.String[])` |
| `.cheat_crop_growth_scale` | `.cheat_crop_growth_scale [int]` | Time / cheats | Sets crop growth time scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_48(System.String[])` |
| `.cheat_disable_construction_materials` | `.cheat_disable_construction_materials` | Constructions / placeables | Toggles construction material requirements. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_52(System.String[])` |
| `.cheat_job_progress_scale` | `.cheat_job_progress_scale [int]` | Time / cheats | Sets job progress scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_50(System.String[])` |
| `.cheat_job_speed_scale` | `.cheat_job_speed_scale [int]` | Time / cheats | Sets job tick speed scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_49(System.String[])` |
| `.cheat_spawn_wave` | `.cheat_spawn_wave` | Entities / spawning | Requests an enemy wave spawn. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_54(System.String[])` |
| `.cheat_spawnrate_scale` | `.cheat_spawnrate_scale [int]` | Entities / spawning | Sets dynamic entity spawn-rate scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_51(System.String[])` |
| `.cinematic` | `.cinematic` | Display / camera | Enters cinematic camera mode. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_94(System.String[])` |
| `.citizen_add_generic` | `.citizen_add_generic` | Entities / spawning | Adds one generic citizen. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_96(System.String[])` |
| `.citizen_kill_all` | `.citizen_kill_all` | Entities / spawning | Kills all citizens. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_97(System.String[])` |
| `.connection_address` | `.connection_address` | Networking / diagnostics | Copies and prints connection address or Steam relay ID. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_95(System.String[])` |
| `.construct` | `.construct <construction-id>` | Constructions / placeables | Requests construction placement at the player. | ConstructionDataBase.DataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_12(System.String[])` |
| `.debug3d` | `.debug3d` | Debug / overlays | Toggles deferred renderer debug mode. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_22(System.String[])` |
| `.debugchunks` | `.debugchunks` | Debug / overlays | Toggles exterior world chunk debug rendering. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_27(System.String[])` |
| `.debugcrops` | `.debugcrops` | Debug / overlays | Toggles crop debug rendering. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_24(System.String[])` |
| `.debuggeneral` | `.debuggeneral` | Debug / overlays | Toggles Globals.Debug, enabling broad debug UI features. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_25(System.String[])` |
| `.debugspeed` | `.debugspeed` | Debug / overlays | Toggles movement-speed debug rendering. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_26(System.String[])` |
| `.debugtiles` | `.debugtiles` | Debug / overlays | Toggles tile debug rendering. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_23(System.String[])` |
| `.detach` | `.detach` | Player / movement | Requests detach for the held entity. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_53(System.String[])` |
| `.die` | `.die` | Player / cheats | Requests player death mode 0; arguments are rejected with 'Use .health instead'. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_93(System.String[])` |
| `.dungeon` | `.dungeon <room-id>` | World / map | Requests handcrafted dungeon-room spawn. | DungeonRoomDataBase.DataMap handcrafted room keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_84(System.String[])` |
| `.dynamic-spawn` | `.dynamic-spawn <entity-or-doodad-name-or-guid> [instant-aggro]` | Entities / spawning | Requests dynamic entity spawning, optionally with instant aggro. | DoodadDatabaseManager.Names keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_9(System.String[])` |
| `.enter` | `.enter [building-guid]` | World / map | Enters a specified building or the exterior under the player. | ServerGameState.Buildings keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_77(System.String[])` |
| `.exit` | `.exit [building-guid]` | World / map | Exits a building or dungeon, or exits a specified building. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_79(System.String[])` |
| `.firstperson` | `.firstperson` | Display / camera | Toggles first-person/deferred-renderer debug mode. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_42(System.String[])` |
| `.float` | `.float` | Player / movement | Toggles player gravity. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_20(System.String[])` |
| `.fontscale` | `.fontscale <float>` | Display / camera | Sets terminal font scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_40(System.String[])` |
| `.framestepper` | `.framestepper` | Diagnostics | Toggles the frame stepper. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_66(System.String[])` |
| `.framestepper_stepcount` | `.framestepper_stepcount [int]` | Diagnostics | Sets custom frame-step skip count. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_68(System.String[])` |
| `.framestepper_stepframes` | `.framestepper_stepframes [int]` | Diagnostics | Runs N frames with the frame stepper. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_67(System.String[])` |
| `.freecam` | `.freecam` | Display / camera | Toggles free camera mode. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_62(System.String[])` |
| `.furniture_add_all` | `.furniture_add_all [count]` | Constructions / placeables | Adds every furniture entry by count when cheats are enabled. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_108(System.String[])` |
| `.gametimescale` | `.gametimescale [float]` | Time / cheats | Sets global game time scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_46(System.String[])` |
| `.grid` | `.grid` | Debug / overlays | Toggles tile grid rendering. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_35(System.String[])` |
| `.health` | `.health [float]` | Player / cheats | Sets player health; default is max health. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_59(System.String[])` |
| `.hitboxes` | `.hitboxes` | Debug / overlays | Toggles hitbox rendering. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_41(System.String[])` |
| `.hour` | `.hour [double]` | Time / cheats | Requests server time set to a day-hour value. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_56(System.String[])` |
| `.imgui` | `.imgui` | Debug / overlays | Toggles ImGui. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_34(System.String[])` |
| `.instantactions` | `.instantactions` | Time / cheats | Toggles instant actions when cheats are enabled. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_58(System.String[])` |
| `.item` | `.item <item-id> [count-or-aura]` | Items | Adds an item cheat when connected and cheats are enabled. | ItemDataBase.DataMap keys; ItemAuraDataBase.AuraDataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_1(System.String[])` |
| `.jump` | `.jump` | Player / movement | Applies upward player velocity. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_43(System.String[])` |
| `.lagsmoothmode` | `.lagsmoothmode [int]` | Networking / diagnostics | Sets lag smoothing mode; default is 3. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_88(System.String[])` |
| `.line_spawn` | `.line_spawn <entity-or-doodad-name-or-guid>` | Entities / spawning | Spawns or requests a 40-entity line. | DoodadDatabaseManager.Names keys | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_5(System.String[])` |
| `.load` | `.load <script-path>` | Lua / scripting | Imports a script file and runs it as Lua. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_0(System.String[])` |
| `.load_in_world_file` | `.load_in_world_file <map-name>` | World / map | Loads a world map file into the exterior world at the player while local hosting. | ServerWorldDataManager.WorldData keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_73(System.String[])` |
| `.marcopolo` | `.marcopolo` | World / map | Copies server world tiles/heights locally and discovers dungeons/bosses while local hosting. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_72(System.String[])` |
| `.mass_spawn` | `.mass_spawn <entity-or-doodad-name-or-guid>` | Entities / spawning | Spawns or requests a 25 by 40 entity grid. | DoodadDatabaseManager.Names keys | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_7(System.String[])` |
| `.mode` | `.mode <int>` | Debug / mode | Sets the engine mode manager by numeric value. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_32(System.String[])` |
| `.name` | `.name [name...]` | Player / profile | Sets player name; no argument reports the current name. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_45(System.String[])` |
| `.networkstats` | `.networkstats` | Networking / diagnostics | Enables network stats or prints current network stats. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_60(System.String[])` |
| `.nextday` | `.nextday` | Time / cheats | Requests a server time skip to the next day. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_55(System.String[])` |
| `.noclip` | `.noclip` | Player / movement | Toggles player collision. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_19(System.String[])` |
| `.playerstate` | `.playerstate` | Diagnostics | Prints the current player state machine state. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_37(System.String[])` |
| `.poi` | `.poi <poi-id>` | World / map | Spawns a registered POI near the player. | PoiDataBase.Data keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_75(System.String[])` |
| `.questcheat` | `.questcheat <quest-id>` | Progression | Requests world quest completion. | QuestsDataBase.DataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_63(System.String[])` |
| `.raid_arrive` | `.raid_arrive` | Raids | Forces the current town raid to arrive. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_103(System.String[])` |
| `.raid_beat` | `.raid_beat` | Raids | Marks the current town raid beaten. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_104(System.String[])` |
| `.raid_fail` | `.raid_fail` | Raids | Marks the current town raid failed. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_105(System.String[])` |
| `.raid_new` | `.raid_new <raid-id>` | Raids | Spawns a raid targeting the town under the player. | RaidDataBase.DataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_101(System.String[])` |
| `.reloaddata` | `.reloaddata` | Reload / content | Reloads exterior auto-tile rules. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_18(System.String[])` |
| `.reloadparticles` | `.reloadparticles` | Reload / content | Clears and reloads particle data. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_17(System.String[])` |
| `.reloadsprites` | `.reloadsprites` | Reload / content | Reloads sprite sheets. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_15(System.String[])` |
| `.renderscale` | `.renderscale <int>` | Display / camera | Sets render scale. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_38(System.String[])` |
| `.resettime` | `.resettime` | Time / cheats | Requests time reset to day 1 at 00:00. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_57(System.String[])` |
| `.rewards_unlock_all` | `.rewards_unlock_all` | Progression | Unlocks all rewards. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_98(System.String[])` |
| `.rewards_unlock_one` | `.rewards_unlock_one <reward-id>` | Progression | Unlocks one reward. | RewardDataBase.RewardsDataMap keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_99(System.String[])` |
| `.save_character` | `.save_character` | Saves | Saves the current character. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_90(System.String[])` |
| `.save_game` | `.save_game` | Saves | Queues server game save while local hosting. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_89(System.String[])` |
| `.save_worldmap` | `.save_worldmap` | Saves | Saves the world map. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_91(System.String[])` |
| `.seed` | `.seed` | World / map | Copies and prints the world seed. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_92(System.String[])` |
| `.spawn` | `.spawn <entity-or-doodad-name-or-guid>` | Entities / spawning | Spawns one entity or doodad at the player, or requests the server to spawn it. | DoodadDatabaseManager.Names keys | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_3(System.String[])` |
| `.spawn_all_creatures` | `.spawn_all_creatures` | Entities / spawning | Spawns every creature GUID near the player. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_11(System.String[])` |
| `.speed` | `.speed <int>` | Player / movement | Sets player acceleration scale. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_14(System.String[])` |
| `.taa` | `.taa <index>` | Diagnostics | Analyzes a time analyzer. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_30(System.String[])` |
| `.tac` | `.tac <index>` | Diagnostics | Clears logs for a time analyzer. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_31(System.String[])` |
| `.tad` | `.tad <index>` | Diagnostics | Disables a time analyzer. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_29(System.String[])` |
| `.tae` | `.tae <index>` | Diagnostics | Enables a time analyzer. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_28(System.String[])` |
| `.test` | `.test` | Diagnostics | Prints collision-offset entity diagnostics. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_65(System.String[])` |
| `.timescale` | `.timescale [int]` | Time / cheats | Sets day/night cycle time scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_47(System.String[])` |
| `.toggle_spawn_system` | `.toggle_spawn_system` | Entities / spawning | Toggles the spawn system disabled flag. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_69(System.String[])` |
| `.tp` | `.tp <tile-x> <tile-y>` | Player / movement | Teleports to tile coordinates; the handler multiplies by 16 and jumps the camera. | none | `System.Object Candide.CandideEngine::<AddTerminalCommands>b__61_21(System.String[])` |
| `.ui` | `.ui` | Debug / overlays | Toggles game UI visibility. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_33(System.String[])` |
| `.uiscale` | `.uiscale <int>` | Display / camera | Sets UI scale. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_39(System.String[])` |
| `.unlearn_all_favours` | `.unlearn_all_favours` | Progression | Removes all learned favours. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_16(System.String[])` |
| `.weather` | `.weather [weather-id]` | World / map | Requests a weather cheat at the player position. | ClientWeatherDatabase.Data keys | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_70(System.String[])` |
| `.zoom` | `.zoom [float]` | Display / camera | Sets camera and zoom scale; default is 1. | none | `System.Object Candide.CandideEngine/<>c::<AddTerminalCommands>b__61_61(System.String[])` |
