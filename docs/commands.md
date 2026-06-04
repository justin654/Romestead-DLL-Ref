# Romestead Terminal Dot Commands

Generated: `2026-06-04 05:42:43Z`
Command count: `92`

## How Commands Work

- Registry: `Candide.CandideEngine.AddTerminalCommands()`
- Storage: `Candide.Terminal.GameTerminal.Commands: Dictionary<string, TerminalCommand>`
- Dispatch: `Candide.Terminal.GameTerminal.DoCommand(string) after stripping the leading dot`
- Autocomplete: `Candide.Terminal.GameTerminal.SuggestCommand(string), using Commands.Keys plus per-command suggestion delegates`

## Latest Command Diff

- Added: `0`
- Removed: `0`
- Changed: `0`

## Full Command List

| Command | Usage | Category | Summary | Autocomplete |
| --- | --- | --- | --- | --- |
| `.all_pois` | `.all_pois [prefix]` | World / map | Spawns all POIs matching an optional prefix while local hosting, including flipped variants. | none |
| `.aura` | `.aura <aura-id>` | Player / cheats | Applies an aura to the player. | [AuraDataBase.AuraDataMap](types/Shared/Shared_Data_AuraDataBase.html#auradatamap-system-collections-generic-dictionary-2-system-string-shared-models-auras-entityaurainfo-) keys |
| `.beat` | `.beat` | Debug / misc | Starts the beat engine. | none |
| `.boss` | `.boss <boss-id>` | Entities / spawning | Requests boss spawn at the player. | [BossDataBase.DataMap](types/Shared/Shared_Data_BossDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-shared-models-boss-bossdata-) keys |
| `.buildings_construct_all` | `.buildings_construct_all` | Constructions / placeables | Spawns every base building and upgrade chain in rows near the player. | none |
| `.challenge_dungeon` | `.challenge_dungeon <dungeon-id>` | World / map | Requests challenge dungeon spawn. | [ChallengeDungeonDataBase.DataMap](types/CandideServer/CandideServer_Database_Dungeons_ChallengeDungeonDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-candideserver-data-dungeons-challengedungeondata-) keys |
| `.chat` | `.chat <message...>` | Networking / chat | Sends chat text. | none |
| `.cheat_crop_growth_scale` | `.cheat_crop_growth_scale [int]` | Time / cheats | Sets crop growth time scale. | none |
| `.cheat_disable_construction_materials` | `.cheat_disable_construction_materials` | Constructions / placeables | Toggles construction material requirements. | none |
| `.cheat_job_progress_scale` | `.cheat_job_progress_scale [int]` | Time / cheats | Sets job progress scale. | none |
| `.cheat_job_speed_scale` | `.cheat_job_speed_scale [int]` | Time / cheats | Sets job tick speed scale. | none |
| `.cheat_spawn_wave` | `.cheat_spawn_wave` | Entities / spawning | Requests an enemy wave spawn. | none |
| `.cheat_spawnrate_scale` | `.cheat_spawnrate_scale [int]` | Entities / spawning | Sets dynamic entity spawn-rate scale. | none |
| `.cinematic` | `.cinematic` | Display / camera | Enters cinematic camera mode. | none |
| `.citizen_add_generic` | `.citizen_add_generic` | Entities / spawning | Adds one generic citizen. | none |
| `.citizen_kill_all` | `.citizen_kill_all` | Entities / spawning | Kills all citizens. | none |
| `.connection_address` | `.connection_address` | Networking / diagnostics | Copies and prints connection address or Steam relay ID. | none |
| `.construct` | `.construct <construction-id>` | Constructions / placeables | Requests construction placement at the player. | [ConstructionDataBase.DataMap](types/Shared/Shared_Data_ConstructionDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-shared-models-construction-constructionmodel-) keys |
| `.debug3d` | `.debug3d` | Debug / overlays | Toggles deferred renderer debug mode. | none |
| `.debugchunks` | `.debugchunks` | Debug / overlays | Toggles exterior world chunk debug rendering. | none |
| `.debugcrops` | `.debugcrops` | Debug / overlays | Toggles crop debug rendering. | none |
| `.debuggeneral` | `.debuggeneral` | Debug / overlays | Toggles [Globals.Debug](types/Romestead/Candide_Globals.html#debug-system-boolean), enabling broad debug UI features. | none |
| `.debugspeed` | `.debugspeed` | Debug / overlays | Toggles movement-speed debug rendering. | none |
| `.debugtiles` | `.debugtiles` | Debug / overlays | Toggles tile debug rendering. | none |
| `.detach` | `.detach` | Player / movement | Requests detach for the held entity. | none |
| `.die` | `.die` | Player / cheats | Requests player death mode 0; arguments are rejected with 'Use .health instead'. | none |
| `.dungeon` | `.dungeon <room-id>` | World / map | Requests handcrafted dungeon-room spawn. | [DungeonRoomDataBase.DataMap](types/CandideServer/CandideServer_Database_DungeonRoomDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-candideserver-models-dungeons-dungeonroomdatamodel-) handcrafted room keys |
| `.dynamic-spawn` | `.dynamic-spawn <entity-or-doodad-name-or-guid> [instant-aggro]` | Entities / spawning | Requests dynamic entity spawning, optionally with instant aggro. | [DoodadDatabaseManager.Names](types/Romestead/Candide_Database_Doodad_DoodadDatabaseManager.html#names-system-collections-generic-dictionary-2-system-string-system-guid-) keys |
| `.enter` | `.enter [building-guid]` | World / map | Enters a specified building or the exterior under the player. | [ServerGameState.Buildings](types/CandideServer/CandideServer_ServerGameState.html#buildings-system-collections-generic-dictionary-2-system-guid-candideserver-simulationmodels-buildingsimulationmodel-) keys |
| `.exit` | `.exit [building-guid]` | World / map | Exits a building or dungeon, or exits a specified building. | none |
| `.firstperson` | `.firstperson` | Display / camera | Toggles first-person/deferred-renderer debug mode. | none |
| `.float` | `.float` | Player / movement | Toggles player gravity. | none |
| `.fontscale` | `.fontscale <float>` | Display / camera | Sets terminal font scale. | none |
| `.framestepper` | `.framestepper` | Diagnostics | Toggles the frame stepper. | none |
| `.framestepper_stepcount` | `.framestepper_stepcount [int]` | Diagnostics | Sets custom frame-step skip count. | none |
| `.framestepper_stepframes` | `.framestepper_stepframes [int]` | Diagnostics | Runs N frames with the frame stepper. | none |
| `.freecam` | `.freecam` | Display / camera | Toggles free camera mode. | none |
| `.furniture_add_all` | `.furniture_add_all [count]` | Constructions / placeables | Adds every furniture entry by count when cheats are enabled. | none |
| `.gametimescale` | `.gametimescale [float]` | Time / cheats | Sets global game time scale. | none |
| `.grid` | `.grid` | Debug / overlays | Toggles tile grid rendering. | none |
| `.health` | `.health [float]` | Player / cheats | Sets player health; default is max health. | none |
| `.hitboxes` | `.hitboxes` | Debug / overlays | Toggles hitbox rendering. | none |
| `.hour` | `.hour [double]` | Time / cheats | Requests server time set to a day-hour value. | none |
| `.imgui` | `.imgui` | Debug / overlays | Toggles ImGui. | none |
| `.instantactions` | `.instantactions` | Time / cheats | Toggles instant actions when cheats are enabled. | none |
| `.item` | `.item <item-id> [count-or-aura]` | Items | Adds an item cheat when connected and cheats are enabled. | [ItemDataBase.DataMap](types/Shared/Shared_Data_ItemDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-shared-models-items-itemdata-) keys; [ItemAuraDataBase.AuraDataMap](types/Shared/Shared_Data_ItemAuraDataBase.html#auradatamap-system-collections-generic-dictionary-2-system-string-shared-models-auras-itemaura-) keys |
| `.jump` | `.jump` | Player / movement | Applies upward player velocity. | none |
| `.lagsmoothmode` | `.lagsmoothmode [int]` | Networking / diagnostics | Sets lag smoothing mode; default is 3. | none |
| `.line_spawn` | `.line_spawn <entity-or-doodad-name-or-guid>` | Entities / spawning | Spawns or requests a 40-entity line. | [DoodadDatabaseManager.Names](types/Romestead/Candide_Database_Doodad_DoodadDatabaseManager.html#names-system-collections-generic-dictionary-2-system-string-system-guid-) keys |
| `.load` | `.load <script-path>` | Lua / scripting | Imports a script file and runs it as Lua. | none |
| `.load_in_world_file` | `.load_in_world_file <map-name>` | World / map | Loads a world map file into the exterior world at the player while local hosting. | [ServerWorldDataManager.WorldData](types/CandideServer/CandideServer_World_ServerWorldDataManager.html#worlddata-system-collections-generic-dictionary-2-system-string-candideserver-world-serverworlddata-) keys |
| `.marcopolo` | `.marcopolo` | World / map | Copies server world tiles/heights locally and discovers dungeons/bosses while local hosting. | none |
| `.mass_spawn` | `.mass_spawn <entity-or-doodad-name-or-guid>` | Entities / spawning | Spawns or requests a 25 by 40 entity grid. | [DoodadDatabaseManager.Names](types/Romestead/Candide_Database_Doodad_DoodadDatabaseManager.html#names-system-collections-generic-dictionary-2-system-string-system-guid-) keys |
| `.mode` | `.mode <int>` | Debug / mode | Sets the engine mode manager by numeric value. | none |
| `.name` | `.name [name...]` | Player / profile | Sets player name; no argument reports the current name. | none |
| `.networkstats` | `.networkstats` | Networking / diagnostics | Enables network stats or prints current network stats. | none |
| `.nextday` | `.nextday` | Time / cheats | Requests a server time skip to the next day. | none |
| `.noclip` | `.noclip` | Player / movement | Toggles player collision. | none |
| `.playerstate` | `.playerstate` | Diagnostics | Prints the current player state machine state. | none |
| `.poi` | `.poi <poi-id>` | World / map | Spawns a registered POI near the player. | [PoiDataBase.Data](types/CandideServer/CandideServer_Database_PoiDataBase.html#data-system-collections-generic-dictionary-2-system-string-candideserver-models-poidata-) keys |
| `.questcheat` | `.questcheat <quest-id>` | Progression | Requests world quest completion. | [QuestsDataBase.DataMap](types/Shared/Shared_Data_QuestsDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-shared-models-quests-questdata-) keys |
| `.raid_arrive` | `.raid_arrive` | Raids | Forces the current town raid to arrive. | none |
| `.raid_beat` | `.raid_beat` | Raids | Marks the current town raid beaten. | none |
| `.raid_fail` | `.raid_fail` | Raids | Marks the current town raid failed. | none |
| `.raid_new` | `.raid_new <raid-id>` | Raids | Spawns a raid targeting the town under the player. | [RaidDataBase.DataMap](types/Shared/Shared_Data_RaidDataBase.html#datamap-system-collections-generic-dictionary-2-system-string-shared-data-datamodels-raiddata-) keys |
| `.reloaddata` | `.reloaddata` | Reload / content | Reloads exterior auto-tile rules. | none |
| `.reloadparticles` | `.reloadparticles` | Reload / content | Clears and reloads particle data. | none |
| `.reloadsprites` | `.reloadsprites` | Reload / content | Reloads sprite sheets. | none |
| `.renderscale` | `.renderscale <int>` | Display / camera | Sets render scale. | none |
| `.resettime` | `.resettime` | Time / cheats | Requests time reset to day 1 at 00:00. | none |
| `.rewards_unlock_all` | `.rewards_unlock_all` | Progression | Unlocks all rewards. | none |
| `.rewards_unlock_one` | `.rewards_unlock_one <reward-id>` | Progression | Unlocks one reward. | [RewardDataBase.RewardsDataMap](types/CandideServer/CandideServer_Database_RewardDataBase.html#rewardsdatamap-system-collections-generic-dictionary-2-system-string-candideserver-data-datamodels-rewarddata-) keys |
| `.save_character` | `.save_character` | Saves | Saves the current character. | none |
| `.save_game` | `.save_game` | Saves | Queues server game save while local hosting. | none |
| `.save_worldmap` | `.save_worldmap` | Saves | Saves the world map. | none |
| `.seed` | `.seed` | World / map | Copies and prints the world seed. | none |
| `.spawn` | `.spawn <entity-or-doodad-name-or-guid>` | Entities / spawning | Spawns one entity or doodad at the player, or requests the server to spawn it. | [DoodadDatabaseManager.Names](types/Romestead/Candide_Database_Doodad_DoodadDatabaseManager.html#names-system-collections-generic-dictionary-2-system-string-system-guid-) keys |
| `.spawn_all_creatures` | `.spawn_all_creatures` | Entities / spawning | Spawns every creature GUID near the player. | none |
| `.speed` | `.speed <int>` | Player / movement | Sets player acceleration scale. | none |
| `.taa` | `.taa <index>` | Diagnostics | Analyzes a time analyzer. | none |
| `.tac` | `.tac <index>` | Diagnostics | Clears logs for a time analyzer. | none |
| `.tad` | `.tad <index>` | Diagnostics | Disables a time analyzer. | none |
| `.tae` | `.tae <index>` | Diagnostics | Enables a time analyzer. | none |
| `.test` | `.test` | Diagnostics | Prints collision-offset entity diagnostics. | none |
| `.timescale` | `.timescale [int]` | Time / cheats | Sets day/night cycle time scale. | none |
| `.toggle_spawn_system` | `.toggle_spawn_system` | Entities / spawning | Toggles the spawn system disabled flag. | none |
| `.tp` | `.tp <tile-x> <tile-y>` | Player / movement | Teleports to tile coordinates; the handler multiplies by 16 and jumps the camera. | none |
| `.ui` | `.ui` | Debug / overlays | Toggles game UI visibility. | none |
| `.uiscale` | `.uiscale <int>` | Display / camera | Sets UI scale. | none |
| `.unlearn_all_favours` | `.unlearn_all_favours` | Progression | Removes all learned favours. | none |
| `.weather` | `.weather [weather-id]` | World / map | Requests a weather cheat at the player position. | [ClientWeatherDatabase.Data](types/Romestead/Candide_Database_ClientWeatherDatabase.html#data-system-collections-generic-dictionary-2-system-string-candide-gamemodels-models-weather-clientweatherdata-) keys |
| `.zoom` | `.zoom [float]` | Display / camera | Sets camera and zoom scale; default is 1. | none |
