using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pandawan.Islands.Tilemaps {

    [RequireComponent(typeof(World))]
	public class WorldManager : MonoBehaviour
	{
	    private World world;

	    private void Awake()
	    {
	        world = GetComponent<World>();
	    }

	    public void Save()
	    {
	        List<Chunk> chunks = world.GetDirtyChunks();
            // TODO: Add Saving Chunks System
	    }

	}
}