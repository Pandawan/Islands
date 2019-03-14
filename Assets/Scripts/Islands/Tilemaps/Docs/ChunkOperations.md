# ChunkOperation

The ChunkOperations system is a way for the World to handle asynchronous chunk modification and loading in series. The World runs through a queue of operations every time new operations are added.

## How operations work

The World keeps track of all `IChunkOperation`s that were requested using a Queue. The World's Update method calls `ProcessOperations`, which loops through currently queued operations and executes them.

The `ProcessOperations` system also takes care of unloading chunks; if a chunk that was used by an operation is not needed by any other chunk operation AND is not requested by a `ChunkLoader`, it is unloaded.

To load a chunk, use the `RequestChunkLoading` operation OR, if internally, use `GetOrCreateChunks` (this will just load it but not keep it loaded).
To unload a chunk use the `RequestChunkUnloading` operation; this cannot be done internally since the `ProcessOperations` takes care of chunk unloading (simply make sure that no operation or `ChunkLoader` is requesting that chunk).

## Using operations

Outside classes should simply call the operation like so `World.instance.RequestChunkLoading()`. In this case, `RequestChunkLoading` is an abstraction of the ChunkOperation `LoadChunkOperation`.

### Creating an abstraction method

When using operations, you want to create abstractions in the World so that outside classes do not have to use the `AddChunkOperation`.
These abstractions should be similar to this method, which uses `AddChunkOperation` with a new `IChunkOperation` class.

```cs
public async Task RequestChunkLoading(List<Vector3Int> chunkPositions, ChunkLoader requester)
{
    if (debugMode) Debug.Log("Requesting for chunks " + chunkPositions.ToStringFlattened() + " to load.");
    await AddChunkOperation(new LoadChunkOperation(chunkPositions, requester));
}
```

### Creating an internal method

```js
// TODO: Maybe I should move the internal chunk operations to each IChunkOperation class.
```

The `IChunkOperation` classes should call World internal methods, which are the bulk of the actual chunk operation, from their `Execute()` method.
This internal method should take care of all the Chunk modification, etc.

### Creating a IChunkOperation class

Creating a `IChunkOperation` class requires extending it, and creating an `Execute()` method to handle the actual operation. Because of the way `IChunkOperation`s are handled in the World, you also need to use the `ExecuteCompletionSource` provided so that the original operation caller can await it correctly. You should also use a `Result` variable to store any data that should be passed back to the original caller.

```cs
public override async Task Execute(World world)
{
    Result = await world._IsEmptyTileAt(TilePosition);
    ExecuteCompletionSource.SetResult(true);
}
```
