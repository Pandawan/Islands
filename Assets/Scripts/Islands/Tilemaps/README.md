# Tilemaps

*Don't use the Tilemap directly, always use the World (Abstraction layer) to manipulate the tilemap.*

- [Tile Positions](#Tile-Positions)
- [Abstraction Layer](#Abstraction-Layer)
- [Tilemap](#Tilemap)
- [GridInformation](#GridInformation)
- [Tile Pivot and Collider](#Tile-Pivot-and-Collider)
- [Tile Editor](#Tile-Editor)
- [World Generation](#World-Generation)

## Tile Positions

### Tile Positions
A position to identify tiles in the world where 1 = 1 tile.

### Chunk Position
A position to identify chunks where 1 = 1 chunk = [chunkSize] tiles.

### Local Chunk Position
A position to identify tiles inside a chunk. These are relative to the chunk's position. They go from 0 to [chunkSize].

## Abstraction Layer

The World Component has an abstraction layer using Chunks for easier Tile management and Saving/Loading system.
**From now on, never use the Tilemap system, just use the World Abstraction Layer or expand it.**

### Saving

To Save Chunks, you can either save each `Chunk` separately (they are currently 32x32x32), or use Regions (which are Key-Value-Pairs of Chunks within given bounds).

### Loading

**Loading isn't yet implemented!**
To make loading, add a system that deserializes the objects into `Chunk` objects, then call the Load method.
Note: The Load method might need some changes, for now it just copies its content into the Tilemap.

## Tilemap

Tilemap stores the actual Tile info. 

All Tiles are stored as ScriptableObject assets in the `GameAssets/Tiles/` folder.
They all need to be added to the TileDB's database/list so they can be used in-game.
They are referenced by their `name`, **there is no `id`**.

### Static Tiles

Static tiles inherit the regular `Tile` class; no need to create a new class.

### Dynamic Tiles

Dynamic tiles are created using custom classes that inherit `Tile` or `TileBase` in the `Scripts/Islands/Tilemaps/Tiles/` folder.
They can then add behavior by extending the methods of the `Tile`/`TileBase`.
If they need custom serializable variables (to be saved), use GridInformation. If they do not need to be saved, use local variable/properties.

### Serialization

Tilemap can be saved/serialized using `<Tilemap Object>.GetTilesBlock(<BoundsInt Object (similar to Rect)>)`, which returns an array of all Tiles in the specified region.
This array can then be serialized/saved into Chunks or however you see fit.

## GridInformation

GridInformation is used to store extra information about tiles.
For example, a chest tile could save its its content through this. 

It uses position to reference to a specific tile, and uses a name to reference to different "variables."

### Usage

#### Set a Property

Use `<GridInformation Object>.SetPositionProperty(<Tile's Position>, <Property Name>, <Property Value>)` to save a value.

Example:
```cs
// Set the value grassType of the Tile at (0,0,0) to 1
gridInfo.SetPositionProperty(Vector3Int.zero, "grassType", 1);
```

### Get a Property

Use `<GridInformation Object>.SetPositionProperty(<Tile's Position>, <Property Name>, <Default Value (If none found)>)` to get a value.

Example:
Example:
```cs
// If the Tile at (0,0,0) has a value grassType, get it; if not, use 5
gridInfo.GetPositionProperty(Vector3Int.zero, "grassType", 5);
```

### Serialization

GridInformation already implements a system to serialize it. You can serialize it however you want.

### Using Unsupported Data Types

If a data type you want to save is not supported by the GridInformation system, you have to implement it yourself.
Open the `GridInformation.cs` file, and add custom `GridInformationType` as well as custom Lists and Methods to save/get/serialize.


## Tile Pivot and Collider

Setting a custom pivot/offset and collider for a tile is simple. You can do it all from the Sprite itself (or by modifying the TileData.sprite).

### Pivot

To set a custom pivot or offset for the tile, open the Sprite Editor and set a custom Pivot.

This can also be done at runtime using the `TileData.sprite.pivot` (or from the BasicTile's `Sprite`).

### Collider

To use a custom collider for the tile, open the Sprite Editor. 
Set the top left dropdown to "Custom Physics Shape" and create a custom shape.
You can also use the "Generate" button to generate one based on pixels.

This can also be done at runtime using the `TileData.sprite.OverridePhysicsShape()` (or from the BasicTile's `Sprite`).

NOTE: Making the height too small might be an issue when sorting with the player. Watch out for this issue.

## Tile Editor

If you want to create a tilemap in the editor (out of runtime), so as to create custom/pre-made maps, use Unity's built-in editor.

Simply create a BasicTile Scriptable Object ([just like you would at runtime](#Tilemap)), add them to the "Editor Palette" (found in `GameAssets` folder), and start creating.
 
Then, add/enable a TilemapImporter component on the World game object so it can import all of these on GenerationEvent.

## World Generation

World Generation can be done in two ways. 

1. TilemapImporter: When using a pre-made tilemap during EditMode, the TilemapImporter script will import the Unity Tilemap into a World-compatible tilemap.
2. WorldGeneration: When procedurally generating new Worlds, the WorldGeneration script allows for runtime generation.

### How it works
World has an event called "GenerationEvent" which is called once the World is ready to be generated. Other components can subscribe to that event and generate once called.

TilemapImporter simply calls WorldManager.ImportTilemap, which loops through every tile in the Tilemap, and `SetTile`s them in the World.

WorldGeneration uses loops/perlin/other trickeries to generate using `SetTile`