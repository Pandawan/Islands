using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        [SerializeField] private Dictionary<ChunkDataKey, ChunkDataValue> positionProperties =
            new Dictionary<ChunkDataKey, ChunkDataValue>();
        internal Dictionary<ChunkDataKey, ChunkDataValue> PositionProperties => positionProperties;
        
        #region Set

        public bool SetPositionProperty<T>(Vector3Int position, string name, T positionProperty)
        {
            throw new NotImplementedException("Storing this type is not accepted in ChunkData");
        }

        public bool SetPositionProperty(Vector3Int position, string name, int positionProperty)
        {
            return SetPositionProperty(position, name, ChunkDataType.Integer, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int position, string name, string positionProperty)
        {
            return SetPositionProperty(position, name, ChunkDataType.String, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int position, string name, float positionProperty)
        {
            return SetPositionProperty(position, name, ChunkDataType.Float, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int position, string name, double positionProperty)
        {
            return SetPositionProperty(position, name, ChunkDataType.Double, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int position, string name, UnityEngine.Object positionProperty)
        {
            return SetPositionProperty(position, name, ChunkDataType.UnityObject, positionProperty);
        }

        public bool SetPositionProperty(Vector3Int position, string name, Color positionProperty)
        {
            return SetPositionProperty(position, name, ChunkDataType.Color, positionProperty);
        }

        private bool SetPositionProperty(Vector3Int position, string name, ChunkDataType dataType,
            object positionProperty)
        {
            if (positionProperty != null)
            {
                ChunkDataKey positionKey;
                positionKey.position = position;
                positionKey.name = name;

                ChunkDataValue positionValue;
                positionValue.type = dataType;
                positionValue.data = positionProperty;

                positionProperties[positionKey] = positionValue;
                return true;
            }

            return false;
        }

        #endregion

        #region Get

        public T GetPositionProperty<T>(Vector3Int position, string name, T defaultValue) where T : UnityEngine.Object
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
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

        public int GetPositionProperty(Vector3Int position, string name, int defaultValue)
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
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

        public string GetPositionProperty(Vector3Int position, string name, string defaultValue)
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
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

        public float GetPositionProperty(Vector3Int position, string name, float defaultValue)
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
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

        public double GetPositionProperty(Vector3Int position, string name, double defaultValue)
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
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

        public Color GetPositionProperty(Vector3Int position, string name, Color defaultValue)
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
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

        #endregion
        
        #region Others

        public bool ErasePositionProperty(Vector3Int position, string name)
        {
            ChunkDataKey positionKey;
            positionKey.position = position;
            positionKey.name = name;
            return positionProperties.Remove(positionKey);
        }

        public virtual void Reset()
        {
            positionProperties.Clear();
        }

        public Vector3Int[] GetAllPositions(string propertyName)
        {
            return positionProperties.Keys.ToList().FindAll(x => x.name == propertyName).Select(x => x.position)
                .ToArray();
        }

        #endregion

        #region Structs

        [Serializable]
        public struct ChunkDataValue
        {
            public ChunkDataType type;
            public object data;
        }

        [Serializable]
        public struct ChunkDataKey
        {
            public Vector3Int position;
            public string name;
        }

        #endregion
    }
}