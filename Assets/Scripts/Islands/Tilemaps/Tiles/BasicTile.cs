using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Pandawan.Islands.Tilemaps.Tiles
{
    [CreateAssetMenu(fileName = "New Tile", menuName = "Tiles/Basic Tile")]
    [Serializable]
    public class BasicTile : TileBase
    {
        [SerializeField]
        private string m_Id => this.name;

        public string Id => m_Id;
        
        [SerializeField]
        private string m_TileName;

        public string TileName
        {
            get { return m_TileName; }
            set { m_TileName = value; }
        }

        [SerializeField]
        private Sprite m_Sprite;

        public Sprite Sprite
        {
            get { return m_Sprite; }
            set { m_Sprite = value; }
        }

        [SerializeField]
        private Color m_Color = Color.white;

        public Color Color
        {
            get { return m_Color; }
            set { m_Color = value; }
        }

        [SerializeField]
        private Tile.ColliderType m_ColliderType = Tile.ColliderType.None;
        
        public Tile.ColliderType ColliderType
        {
            get { return m_ColliderType; }
            set { m_ColliderType = value; }
        }
        
        public override void GetTileData(Vector3Int location, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = this.Sprite;
            tileData.color = this.Color;
            tileData.colliderType = this.ColliderType;
        }
    }
}