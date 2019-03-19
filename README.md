# Islands

A test project where I try to create an advanced Tile Engine with async Chunk loading, runtime tilemap modification and dynamic tile data, using Unity's Built-in Tilemaps. I also tried to maximize performances as much as I could while making sure it remains easy to use.

You can find the actual code in `/Assets/Scripts/Islands/Tilemaps/`, with documentation in the `Docs` directory. There are also custom tools and inspectors available in the `/Assets/Editor/Tilemap/` directory.

The rest of the code is simply here to test the engine itself.

## How it works

Basically, there is one Tilemap, with which a `World` component interacts to load/unload chunks, and modify the tilemap dynamically.
Every task, whether load/unload chunk, set/get/remove/exists tile, etc, is converted to a `ChunkOperation` and added to a Queue in the `World` component.
This `World` component goes through each `ChunkOperation` in series asynchronously, but in an efficient way so as to maximize performance.
You can learn more about the `ChunkOperation` system in the [Chunk Operations docs](Assets/Scripts/Islands/Tilemaps/Docs/ChunkOperations).

To learn more about how this entire system is incorporated with other parts of Unity (such as creating new Tiles, converting between positions, saving dynamic tile data, World Generation, WorldInfo, etc.) check out the [Tilemaps docs](Assets/Scripts/Islands/Tilemaps/Docs/README).

By default, worls are saved in [Application.persistentDataPath](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html) at `/saves/WORLD_NAME/`.

## TODO

- Make it so a World ALWAYS saves with a WorldInfo file.
- Optimize World/Chunk loading more to limit spikes in Unity Profiler (although they are not noticeable in-game).
- Add ChunkLoader buffer so they load far away chunks without rendering them? Maybe...
- Make Player's ChunkLoader use the camera's view bounds instead of preset? 
- Make WorldGen (and maybe TilemapImporter) import dynamically by calling an OnGenerate(-like) event, passing the new chunk to be generated (when creating a new one in GetOrCreateChunk)
- WorldGen using perlin noise (fix negative being reflected)

## TODO Later (nice to have)

- Stop passing around references to ChunkLoader everywhere.
- Stop using World.instance.GetChunkSize(), move all of it to another static config that both Chunk and World can reference.
