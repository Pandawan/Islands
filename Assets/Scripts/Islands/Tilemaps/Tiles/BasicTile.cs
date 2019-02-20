using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps.Tiles
{
    [CreateAssetMenu(fileName = "New Tile", menuName = "Tiles/Basic Tile")]
    [Serializable]
    public class BasicTile : TileBase
    {
        [SerializeField] protected string m_TileName;

        [SerializeField] protected Sprite m_Sprite;

        [SerializeField] protected Color m_Color = Color.white;

        [SerializeField] protected Tile.ColliderType m_ColliderType = Tile.ColliderType.None;
        
        [SerializeField] protected string m_Id => name;

        public string Id => m_Id;

        public string TileName
        {
            get { return m_TileName; }
            set { m_TileName = value; }
        }

        public Sprite Sprite
        {
            get { return m_Sprite; }
            set { m_Sprite = value; }
        }

        public Color Color
        {
            get { return m_Color; }
            set { m_Color = value; }
        }

        public Tile.ColliderType ColliderType
        {
            get { return m_ColliderType; }
            set { m_ColliderType = value; }
        }
        
        public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = Sprite;
            tileData.color = Color;
            tileData.colliderType = ColliderType;
        }
    }
}