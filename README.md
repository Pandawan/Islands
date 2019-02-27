# Islands
A 2D Survival Tile-based game in Unity

## TODO
- Stop using World.instance.GetChunkSize(), move all of it to another static config that both Chunk and World can reference.
- Add ChunkLoader buffer so they load far away chunks without rendering them? Maybe...
- Make Player's ChunkLoader use the camera's view bounds instead of preset? 
- Stop passing around references to ChunkLoader everywhere.
- Make WorldGen (and maybe TilemapImporter) import dynamically by calling an OnGenerate(-like) event, passing the new chunk to be generated (when creating a new one in GetOrCreateChunk)
- WorldGen using perlin noise (fix negative being reflected)