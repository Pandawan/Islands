# Tilemaps

- [Tilemap](#Tilemap)
- [GridInformation](#GridInformation)

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