using System;
using System.Collections.Generic;
using System.Linq;
using Pandawan.Islands.Other;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pandawan.Islands.Tilemaps
{
    [Serializable]
    public enum ChunkDataType
    {
        Integer,
        String,
        Float,
        Double,
        UnityObject,
        Color
    }

    [Serializable]
    public class ChunkData
    {
        [SerializeField] private readonly Dictionary<ChunkDataKey, ChunkDataValue> positionProperties =
            new Dictionary<ChunkDataKey, ChunkDataValue>();

        [NonSerialized] private Chunk chunk;

        // Size of a chunk
        [NonSerialized] private BoundsInt chunkBounds;

        internal Dictionary<ChunkDataKey, ChunkDataValue> PositionProperties => positionProperties;

        #region Constructor

        public ChunkData(Chunk chunk, BoundsInt chunkBounds)
        {
            Setup(chunk, chunkBounds);
        }

        public void Setup(Chunk _chunk, BoundsInt _chunkBounds)
        {
            chunk = _chunk;
            chunkBounds = _chunkBounds;
        }

        #endregion

        #region Set

        public bool SetPositionProperty<T>(Vector3Int globalPosition, string name, T positionProperty)
        {
            throw new NotImplementedException("Storing this type is not accepted in ChunkData");
        }

        public bool SetPositionProperty(Vector3Int globalPosition, string name, int positionProperty)
        {
            return SetPositionProperty(globalPosition, name, ChunkDataType.Integer, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int globalPosition, string name, string positionProperty)
        {
            return SetPositionProperty(globalPosition, name, ChunkDataType.String, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int globalPosition, string name, float positionProperty)
        {
            return SetPositionProperty(globalPosition, name, ChunkDataType.Float, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int globalPosition, string name, double positionProperty)
        {
            return SetPositionProperty(globalPosition, name, ChunkDataType.Double, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int globalPosition, string name, Object positionProperty)
        {
            return SetPositionProperty(globalPosition, name, ChunkDataType.UnityObject, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int globalPosition, string name, Color positionProperty)
        {
            return SetPositionProperty(globalPosition, name, ChunkDataType.Color, positionProperty);
        }

        private bool SetPositionProperty(Vector3Int globalPosition, string name, ChunkDataType dataType,
            object positionProperty)
        {
            if (positionProperty != null)
            {
                Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

                ChunkDataKey positionKey;
                positionKey.position = localPosition;
                positionKey.name = name;

                ChunkDataValue positionValue;
                positionValue.type = dataType;
                positionValue.data = positionProperty;

                positionProperties[positionKey] = positionValue;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Remove the property at the given position and with the given name.
        /// </summary>
        /// <param name="globalPosition">The position of the property to erase.</param>
        /// <param name="name">The name of the property to erase.</param>
        public bool ErasePositionProperty(Vector3Int globalPosition, string name)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;
            return positionProperties.Remove(positionKey);
        }

        /// <summary>
        ///     Remove all of the properties at the given position (regardless of their name).
        /// </summary>
        /// <param name="globalPosition">The position of the property to erase.</param>
        public bool ErasePositionProperty(Vector3Int globalPosition)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);
            return positionProperties.RemoveAll((key, value) => key.position == localPosition);
        }

        #endregion

        #region Get

        public T GetPositionProperty<T>(Vector3Int globalPosition, string name, T defaultValue) where T : Object
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;

            ChunkDataValue positionValue;
            if (positionProperties.TryGetValue(positionKey, out positionValue))
            {
                if (positionValue.type != ChunkDataType.UnityObject)
                    throw new InvalidCastException("Value stored in ChunkData is not of the right type");
                return positionValue.data as T;
            }

            return defaultValue;
        }

        public int GetPositionProperty(Vector3Int globalPosition, string name, int defaultValue)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;

            ChunkDataValue positionValue;
            if (positionProperties.TryGetValue(positionKey, out positionValue))
            {
                if (positionValue.type != ChunkDataType.Integer)
                    throw new InvalidCastException("Value stored in ChunkData is not of the right type");
                return (int) positionValue.data;
            }

            return defaultValue;
        }

        public string GetPositionProperty(Vector3Int globalPosition, string name, string defaultValue)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;

            ChunkDataValue positionValue;
            if (positionProperties.TryGetValue(positionKey, out positionValue))
            {
                if (positionValue.type != ChunkDataType.String)
                    throw new InvalidCastException("Value stored in ChunkData is not of the right type");
                return (string) positionValue.data;
            }

            return defaultValue;
        }

        public float GetPositionProperty(Vector3Int globalPosition, string name, float defaultValue)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;

            ChunkDataValue positionValue;
            if (positionProperties.TryGetValue(positionKey, out positionValue))
            {
                if (positionValue.type != ChunkDataType.Float)
                    throw new InvalidCastException("Value stored in ChunkData is not of the right type");
                return (float) positionValue.data;
            }

            return defaultValue;
        }

        public double GetPositionProperty(Vector3Int globalPosition, string name, double defaultValue)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;

            ChunkDataValue positionValue;
            if (positionProperties.TryGetValue(positionKey, out positionValue))
            {
                if (positionValue.type != ChunkDataType.Double)
                    throw new InvalidCastException("Value stored in ChunkData is not of the right type");
                return (double) positionValue.data;
            }

            return defaultValue;
        }

        public Color GetPositionProperty(Vector3Int globalPosition, string name, Color defaultValue)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            ChunkDataKey positionKey;
            positionKey.position = localPosition;
            positionKey.name = name;

            ChunkDataValue positionValue;
            if (positionProperties.TryGetValue(positionKey, out positionValue))
            {
                if (positionValue.type != ChunkDataType.Color)
                    throw new InvalidCastException("Value stored in ChunkData is not of the right type");
                return (Color) positionValue.data;
            }

            return defaultValue;
        }

        /// <summary>
        ///     Get a dictionary with all the properties at that position.
        /// </summary>
        /// <param name="globalPosition">The position at which to get the properties.</param>
        public Dictionary<ChunkDataKey, ChunkDataValue> GetAllPropertiesAt(Vector3Int globalPosition)
        {
            Vector3Int localPosition = TileToLocalChunkPosition(globalPosition);

            return positionProperties.Keys.ToList().FindAll(x => x.position == localPosition)
                .ToDictionary(key => key, key => positionProperties[key]);
        }

        /// <summary>
        ///     Get a list of all positions that contain this property.
        /// </summary>
        /// <param name="propertyName">The property to search for.</param>
        public Vector3Int[] GetAllPositionsWithProperty(string propertyName)
        {
            return positionProperties.Keys.ToList().FindAll(x => x.name == propertyName).Select(x => x.position)
                .ToArray();
        }

        #endregion

        #region Others

        /// <summary>
        ///     Clear every value in the ChunkData
        /// </summary>
        public virtual void Reset()
        {
            positionProperties.Clear();
        }

        /// <summary>
        ///     Convert the given tile position to a chunk's local position (from 0 to chunkSize - 1).
        /// </summary>
        /// <param name="tilePosition">The tile position to convert from.</param>
        /// <returns>The resulting local chunk position.</returns>
        public Vector3Int TileToLocalChunkPosition(Vector3Int tilePosition)
        {
            // Check that this tilePosition is valid for this ChunkData
            if (!chunk.IsValidPosition(tilePosition))
                Debug.LogError($"Position {tilePosition} is not valid for ChunkData at {chunkBounds.position}");

            return PositionUtilities.TileToChunkPosition(tilePosition, chunkBounds.size);
        }

        #endregion

        #region Structs

        [Serializable]
        public struct ChunkDataValue
        {
            public ChunkDataType type;
            public object data;

            public override string ToString()
            {
                return $"{{ type: {type}, data: {data} }}";
            }
        }

        [Serializable]
        public struct ChunkDataKey
        {
            public Vector3Int position;
            public string name;

            public override string ToString()
            {
                return $"{{ position: {position}, name: {name} }}";
            }
        }

        #endregion
    }
}