using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecreateBlockLight2D
{
    public enum LightingChannelMode
    {
        RED,
        GREEN,
        BLUE
    }


    public class LightManager : MonoBehaviour
    {
        public static LightManager Instance { get; private set; }
        [SerializeField] private LightSource lightSourcePrefab;
        private Dictionary<Vector3Int, LightSource> ambientLightSources;


        [Header("Light Properties")]
        public float lightPenetrationFront = 8.0f;
        public float lightPenetrationBack = 64.0f;
        public float backLayerShadowFactor = 0.25f;
        [Range(0, 1)]
        public float ambientLightStrength = 1.0f;
        private float lightFallOffFront;
        private float lightFallOffBack;
        private float lightPassThreshold;

        [Header("Block Light Properties")]
        public Color ambientLightColor = Color.white;


        [Header("Testing")]
        public Chunk targetChunk;
        public LightValueText lightValueTextPrefab;
        public Dictionary<Vector3Int, LightValueText> lightValueDict = new Dictionary<Vector3Int, LightValueText>();
        public Transform lightValueTextRoot;



        #region INITIALIZE
        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            ambientLightSources = new Dictionary<Vector3Int, LightSource>();
        }

        private void Start()
        {
            lightFallOffFront = 1.0f / lightPenetrationFront;
            lightFallOffBack = 1.0f / lightPenetrationBack;
            lightPassThreshold = lightFallOffBack * 0.999999f;
        }

        #endregion

        #region PRIVATE_METHODS

        private void RemoveLightSource(Vector3Int worldPosition)
        {
            if (HasAmbientLightSource(worldPosition))
            {
                var ambientLightsourceObject = ambientLightSources[worldPosition];
                ambientLightSources.Remove(worldPosition);
                Destroy(ambientLightsourceObject.gameObject);

            }
        }
        #endregion

        #region PUBLIC_METHODS

        #region - Methods for get
        public LightSource GetLightSource(Vector3Int worldPosition)
        {
            if (ambientLightSources.ContainsKey(worldPosition))
                return ambientLightSources[worldPosition];
            return null;
        }

        #endregion

        public LightSource CreateLightSource(Vector3Int worldPosition, Color color, float lightStrength)
        {
            if (HasAmbientLightSource(worldPosition) == false)
            {
                LightSource lightSource = Instantiate(lightSourcePrefab, worldPosition, Quaternion.identity, this.transform);
                ambientLightSources.Add(worldPosition, lightSource);

                lightSource.Initialized(color, lightStrength);
                UpdateLight(lightSource);


                return lightSource;
            }
            return null;
        }

        public void RemoveLightSource(LightSource lightSource)
        {
            if (lightSource == null) return;

            RemoveLight(lightSource);
            Destroy(lightSource.gameObject);
        }


        public void AddAmbientLight(Chunk chunk, Vector3Int worldPosition)
        {
            //RemoveLightSource(worldPosition);
            List<Vector3Int> NB4Check = Get4Neightbours(worldPosition);

            for (int i = 0; i < NB4Check.Count; i++)
            {
                if (chunk.IsAIRBlock(NB4Check[i]) == true)
                {
                    CreateLightSource(NB4Check[i], ambientLightColor, 1.0f);
                }
            }
        }

        public void RemoveAmbientLight(Chunk chunk, Vector3Int worldPosition)
        {
            List<Vector3Int> nb = Get4Neightbours(worldPosition);

            for (int i = 0; i < nb.Count; i++)
            {
                if (chunk.HasNBSolidBlock(nb[i]) == false)
                    RemoveLightSource(nb[i]);
            }

            List<Vector3Int> solidBlockNB = chunk.GetSolidBlockNB(worldPosition);
            for (int i = 0; i < solidBlockNB.Count; i++)
            {
                AddAmbientLight(chunk, solidBlockNB[i]);
            }

        }




        // Methods foor checking
        public bool HasAmbientLightSource(Vector3Int worldPosition)
        {
            return ambientLightSources.ContainsKey(worldPosition);
        }
        // =====================

        #endregion

        #region UTILITIES


        private List<Vector3Int> Get4Neightbours(Vector3Int worldPosition)
        {
            return new List<Vector3Int>()
            {
                new Vector3Int(worldPosition.x ,worldPosition.y + 1),   // UP
                new Vector3Int(worldPosition.x ,worldPosition.y - 1),   // DOWN
                new Vector3Int(worldPosition.x - 1 ,worldPosition.y),   // LEFT
                new Vector3Int(worldPosition.x + 1,worldPosition.y)     // RIGHT
            };
        }

        #endregion








        public Queue<LightNode> queueLightPasses = new Queue<LightNode>();
        private Queue<LightNode> queueLightRemovalPasses = new Queue<LightNode>();
        //private Queue<LightNode> queueRemoveInvalidPositions = new Queue<LightNode>();
        //private List<Vector3Int> removalPositions = new List<Vector3Int>();
        private List<LightSource> removalLightSources = new List<LightSource>();



        private float passLightColorChannelValue;

        private void ClearCachedData()
        {
            removalLightSources.Clear();
            //removalPositions.Clear();

            queueLightPasses.Clear();
            queueLightRemovalPasses.Clear();
            //queueRemoveInvalidPositions.Clear();
        }

        private void PerformLightPasses(Queue<LightNode> queue)
        {
            Debug.Log("PerformLightPasses");
            /* Generate a backup queue to refill the original queue after each channel
             * (since every channel execution empties the queue). */
            Queue<LightNode> queueBackup = new Queue<LightNode>(queue);

            // Spread light for each channel
            queue = new Queue<LightNode>(queueBackup);
            while (queue.Count > 0)
            {
                ExecuteLightingPassThroughColorChannel(queue);
            }
                
        }
        private void ExecuteLightingPassThroughColorChannel(Queue<LightNode> queue)
        {
            Debug.Log("ExecuteLightingPassThroughColorChannel");
            // Get the LightNode that's first in line
            LightNode light = queue.Dequeue();


            // Try and spread its light to surrounding blocks
            ExtendQueueLightPassThroughChannel(queue, light, Vector3Int.left);
            ExtendQueueLightPassThroughChannel(queue, light, Vector3Int.down);
            ExtendQueueLightPassThroughChannel(queue, light, Vector3Int.right);
            ExtendQueueLightPassThroughChannel(queue, light, Vector3Int.up);
        }

        private void ExtendQueueLightPassThroughChannel(Queue<LightNode> queue, LightNode lightNode, Vector3Int spreadDirection)
        {
            float currentLightNodeValueChannel = lightNode.color[(int)LightingChannelMode.GREEN];

            var nbLightNodeWorldPosition = lightNode.worldPosition + spreadDirection;

            /* Get the right chunk for this position.
             * We don't want to use GetChunk on every check, just the ones where we enter a new chunk. */
            var targetChunk = lightNode.chunk;
            

            // Calculate the light falloff for the position in the given direction
            Chunk.BlockType blockBackNB = targetChunk.GetBlockType(nbLightNodeWorldPosition, Chunk.TilemapType.BACK_MAP);
            Chunk.BlockType blockFontNB = targetChunk.GetBlockType(nbLightNodeWorldPosition, Chunk.TilemapType.FRONT_MAP);

            float blockFalloff;
            if (blockFontNB != Chunk.BlockType.AIR)
                blockFalloff = lightFallOffFront;
            else if (blockBackNB != Chunk.BlockType.AIR)
                blockFalloff = lightFallOffBack;
            else
                return;

            Color nbBlockColor = targetChunk.GetBlockBlendedColor(nbLightNodeWorldPosition);
            float nbLightNodeColorChannel = nbBlockColor[(int)LightingChannelMode.GREEN];

            /* Spread light if the tile's channel color in this direction is lower in lightValue even after compensating
             * its falloff. lightPassThreshold acts as an additional performance boost and should be < light falloff back. */           
            if (nbLightNodeColorChannel + blockFalloff + lightPassThreshold < currentLightNodeValueChannel)
            {
                float darkenedColorChannel = Mathf.Clamp(currentLightNodeValueChannel - blockFalloff, 0f, 1f);
                Color newColor = nbBlockColor;
                newColor[(int)LightingChannelMode.GREEN] = darkenedColorChannel;

                LightNode newLightNode = new LightNode()
                {
                    worldPosition = nbLightNodeWorldPosition,
                    color = newColor,
                    chunk = targetChunk
                };


                targetChunk.SetBlockColor(nbLightNodeWorldPosition, newColor);
                queue.Enqueue(newLightNode);


                // Test
                if(lightValueDict.ContainsKey(nbLightNodeWorldPosition))
                {
                    lightValueDict[nbLightNodeWorldPosition].SetLightValue(newColor[(int)LightingChannelMode.GREEN]);
                }
                else
                {
                    lightValueDict.Add(nbLightNodeWorldPosition, Instantiate(lightValueTextPrefab, nbLightNodeWorldPosition, Quaternion.identity, lightValueTextRoot.transform));
                    lightValueDict[nbLightNodeWorldPosition].SetLightValue(newColor[(int)LightingChannelMode.GREEN]);
                }
                // ====
            }
        }


        public void UpdateLight(LightSource lightSource)
        {
            ClearCachedData();

            //Create a struct as lightweight storage per visited block
            LightNode lightNode = new LightNode()
            {
                worldPosition = lightSource.worldPosition,
                color = lightSource.lightColor * lightSource.lightStrength,
                chunk = targetChunk,
            };
            if (lightNode.chunk == null) return;


            // Set the color of the light source's own tile.
            Color currentColor = lightNode.chunk.GetBlockBlendedColor(lightSource.worldPosition);
            Color resultColor = Utilities.GetMaxIntensity(currentColor, lightSource.lightColor * lightSource.lightStrength);
            lightNode.chunk.SetBlockColor(lightSource.worldPosition, resultColor);

            // Spread light
            queueLightPasses.Enqueue(lightNode);
            PerformLightPasses(queueLightPasses);
        }

  

        public void RemoveLight(LightSource lightSource)
        {
            ClearCachedData();

            // Create a struct as lightweight data storage per block
            LightNode lightNode = new LightNode()
            {
                worldPosition = lightSource.worldPosition,
                chunk = targetChunk,
            };
            if (lightNode.chunk == null)
                return;

            /* If there are many LightSources close together, the color of the LightSource is drowned out.
             * To correctly remove this enough, instead of using the light's color to remove, use the color 
             * of the LightSource's tile if that color is greater. */
            Color currentBlockColor = lightNode.chunk.GetBlockBlendedColor(lightSource.worldPosition);
            lightNode.color = Utilities.GetMaxIntensity(currentBlockColor, lightSource.lightColor * lightSource.lightStrength);
            lightNode.chunk.SetBlockColor(lightSource.worldPosition, Color.black);

            queueLightPasses.Clear();
            queueLightRemovalPasses.Enqueue(lightNode);
            PerformLightRemovalPasses(queueLightRemovalPasses);
            
            /* If we touched LightSources during removal spreading, we completely drowned out their color.
             * In order to correctly fill in the void, we need to update these lights. Create a new list
             * of them first to prevent problems from collection changes to removalLights somewhere else. */
            List<LightSource> voidLights = new List<LightSource>(removalLightSources);
            Debug.Log($"voidLights: {voidLights.Count}");
            foreach (LightSource voidLight in voidLights)
                if (voidLight != null && voidLight != lightSource)
                    UpdateLight(voidLight);
        }


        private void PerformLightRemovalPasses(Queue<LightNode> queue)
        {
            /* Generate a backup queue to refill the original queue after each channel
             * (since every channel execution empties the queue).*/
            Queue<LightNode> queueBackup = new Queue<LightNode>(queue);
            // Remove all light from the given channel
            while (queue.Count > 0)
            {
                ExecuteLightingRemovalPass(queue);
            }

            // Spread stronger surrounding channel light that should fill this void. For example from another light source nearby
            //RemoveInvalidSpreadPositions();
            Debug.Log($"queueLightPasses count: {queueLightPasses.Count}");
            PerformLightPasses(queueLightPasses);
        }

        //private void RemoveInvalidSpreadPositions()
        //{
        //    queueRemoveInvalidPositions = new Queue<LightNode>(queueLightPasses);
        //    queueLightPasses.Clear();
        //    foreach (LightNode lightNode in queueRemoveInvalidPositions)
        //        if (removalPositions.Contains(lightNode.worldPosition) == false)
        //            queueLightPasses.Enqueue(lightNode);
        //}




        private void ExecuteLightingRemovalPass(Queue<LightNode> queue)
        {
            // Get the LightNode that's first in line
            LightNode light = queue.Dequeue();

            /* Detect passing over LightSources, while removing, to update them later. When we touch
             * such a LightSource, it means we completely drowned out its color and we need to
             * update the light again to fill in the blanks correctly. */
            LightSource lightSource = GetLightSource(light.worldPosition);
            if (lightSource != null && removalLightSources.Contains(lightSource) == false)
                removalLightSources.Add(lightSource);

            // Track removed positions
            //removalPositions.Add(light.worldPosition);


            // Try and spread the light removal
            ExtendQueueLightRemovalPass(queue, light, light.color.g, Vector3Int.left);
            ExtendQueueLightRemovalPass(queue, light, light.color.g, Vector3Int.down);
            ExtendQueueLightRemovalPass(queue, light, light.color.g, Vector3Int.right);
            ExtendQueueLightRemovalPass(queue, light, light.color.g, Vector3Int.up);
        }

        private void ExtendQueueLightRemovalPass(Queue<LightNode> queue, LightNode light, float lightValue,
        Vector3Int direction)
        {
            Color currentColor = targetChunk.GetBlockBlendedColor(light.worldPosition + direction);
            float lightValueDirection = currentColor[(int)LightingChannelMode.GREEN];

            if (lightValueDirection > 0f)
            {
                // Continue removing and extending while the block I'm looking at has a lower lightValue for this channel
                if (lightValueDirection < lightValue)
                {
                    Color newColor = new Color(currentColor.r, 0, currentColor.b);

                    LightNode lightRemovalNode = new LightNode()
                    {
                        worldPosition = light.worldPosition + direction,
                        color = currentColor,
                        chunk = targetChunk
                    };
                    targetChunk.SetBlockColor(light.worldPosition + direction, newColor);
                    queue.Enqueue(lightRemovalNode);
                }
                /* I just found a tile with a higher lightValue for this channel which means another strong light source
                 * must be nearby. Add tile to the update queue and spread their light after all removal to fill in the blanks 
                 * this removal leaves behind.
                 *   
                 * Because we switch between two different falloff rates, this sometimes targets tiles within its own
                 * light. These are later filtered out before spreading the light (using removalPositions). */
                else
                {
                    LightNode lightNode = new LightNode()
                    {
                        worldPosition = light.worldPosition + direction,
                        color = currentColor,
                        chunk = targetChunk,
                    };

                    queueLightPasses.Enqueue(lightNode);
                }
            }
        }
    }
}