using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RecreateBlockLight2D
{
    public class Chunk : MonoBehaviour
    {
        


        // Chunk properties
        public int chunkSize = 32;
        public Vector2Int chunkIndex;

        public Tilemap tilemapFront;
        public Tilemap tilemapBack;
        public Color[] tileColorsAmbient;
        public BlockType[] mapFront, mapBack;
       

        [SerializeField] private List<BlockData> blocksData;
        [SerializeField] private List<LightSource> ambientLight;

        public enum BlockType
        {
            AIR,
            DIRT,
            STONE,
        }
        public enum TilemapType
        {
            FRONT_MAP,
            BACK_MAP,
            ENUM_END
        }

        #region INITIAL
        private void Awake()
        {
            mapFront = new BlockType[chunkSize * chunkSize];
            mapBack = new BlockType[chunkSize * chunkSize];
            tileColorsAmbient = new Color[chunkSize * chunkSize];
        }



        #endregion

        #region UTILITIES
        public Rect GetRect(Vector2Int chunkIndex)
        {
            int left = chunkIndex.x * chunkSize;
            int bottom = chunkIndex.y * chunkSize;
            int right = left + chunkSize - 1;
            int top = bottom + chunkSize - 1;

            return new Rect(left, bottom, right - left, top - bottom);
        }

        public Vector3Int WP2CB(Vector3Int worldPosition)
        {
            int localChunkPosX = worldPosition.x % chunkSize;
            int localChunkPosY = worldPosition.y % chunkSize;

            if (localChunkPosX < 0)
                localChunkPosX += chunkSize;
            if (localChunkPosY < 0)
                localChunkPosY += chunkSize;

            return new Vector3Int(localChunkPosX, localChunkPosY, 0);
        }

        public Vector3Int GetWorldPosition(Vector3 position)
        {
            return new Vector3Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), 0);
        }
        #endregion

        #region PRIVATE METHODS
        private BlockType[] GetMap(TilemapType tilemapType)
        {
            switch (tilemapType)
            {
                default: return null;
                case TilemapType.FRONT_MAP:
                    return mapFront;
                case TilemapType.BACK_MAP:
                    return mapBack;
            }
        }

        private BlockType GetBlock(Vector3Int worldPosition, TilemapType mapType)
        {
            Vector3Int chunkPosition = WP2CB(worldPosition);

            switch (mapType)
            {
                default: return BlockType.AIR;
                case TilemapType.FRONT_MAP:
                    return mapFront[chunkPosition.x + chunkPosition.y * chunkSize];
                case TilemapType.BACK_MAP:
                    return mapBack[chunkPosition.x + chunkPosition.y * chunkSize];
            }
        }

        private BlockData GetBlockData(BlockType blockType)
        {
            foreach (var blockData in blocksData)
            {
                if (blockData.blockType == blockType)
                    return blockData;
            }

            return null;
        }


        private void SetBlockVisual(Vector3Int worldPosition, TilemapType mapType, BlockType blockType)
        {
            BlockData blockData = GetBlockData(blockType);

            switch (mapType)
            {
                default: break;
                case TilemapType.FRONT_MAP:
                    tilemapFront.SetTile(WP2CB(worldPosition), blockData.tile);
                    break;
                case TilemapType.BACK_MAP:
                    tilemapBack.SetTile(WP2CB(worldPosition), blockData.tile);
                    break;

            }
        }

        private void SetBlockData(Vector3Int worldPosition, TilemapType mapType, BlockType blockType)
        {
            BlockData blockData = GetBlockData(blockType);
            Vector3Int chunkPosition = WP2CB(worldPosition);

            switch (mapType)
            {
                default: break;
                case TilemapType.FRONT_MAP:
                    mapFront[chunkPosition.x + chunkPosition.y * chunkSize] = blockType;
                    break;
                case TilemapType.BACK_MAP:
                    mapBack[chunkPosition.x + chunkPosition.y * chunkSize] = blockType;
                    break;

            }
        }


        #endregion




        #region PUBLIC METHODS

        // Methods for checking
        public bool IsAIRBlock(Vector3Int worldPosition)
        {
            Vector3Int chunkPosition = WP2CB(worldPosition);

            if (mapFront[chunkPosition.x + chunkPosition.y * chunkSize] == BlockType.AIR)
            {
                return true;
            }

            if (mapBack[chunkPosition.x + chunkPosition.y * chunkSize] == BlockType.AIR)
            {
                return true;
            }
            return false;
        }


        // ====================

        public void SetBlock(Vector3Int worldPosition, TilemapType mapType, BlockType blockType)
        {
            SetBlockVisual(worldPosition, mapType, blockType);
            SetBlockData(worldPosition, mapType, blockType);
        }


        public void RemoveBlock(Vector3Int worldPosition, TilemapType mapType)
        {
            SetBlockVisual(worldPosition, mapType, BlockType.AIR);
            SetBlockData(worldPosition, mapType, BlockType.AIR);
        }

        #endregion

    }
}

