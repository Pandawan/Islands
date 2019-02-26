# Islands
A 2D Survival Tile-based game in Unity

## TODO
- Make WorldGen (and maybe TilemapImporter) import dynamically by calling an OnGenerate(-like) event, passing the new chunk to be generated (when creating a new one in GetOrCreateChunk)
- WorldGen using perlin noise (fix negative being reflected)
- Standardize positions names, there are too many different names for same thing (tile/world/global, chunk, local/relative, etc.) 