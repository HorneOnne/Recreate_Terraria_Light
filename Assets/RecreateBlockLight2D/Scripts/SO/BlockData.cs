using UnityEngine;
using UnityEngine.Tilemaps;

namespace RecreateBlockLight2D
{
    [CreateAssetMenu(fileName = "BlockData", menuName = "BlockLight/BlockData")]
    public class BlockData : ScriptableObject
    {
        public Chunk.BlockType blockType;
        public Tile tile;
    }

}
