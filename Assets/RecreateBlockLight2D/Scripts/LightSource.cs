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

    public struct LightNode
    {
        public Vector3Int worldPosition;
        public Color color;
        public Chunk chunk;

        public LightNode(Vector3Int worldPosition, Color color, Chunk chunk)
        {
            this.worldPosition = worldPosition;
            this.color = color;
            this.chunk = chunk;
        }
    }

    public class LightSource : MonoBehaviour
    {
        public float lightStrength;
        public float lightPenetrationFront;
        public float lightPenetrationBack;
        private float lightFallOffFront;
        private float lightFallOffBack;
        private Vector3Int worldPosition;

        public enum LightSourceState
        {
            IDLE, UPDATE, REMOVE
        }


        public LightSourceState state;
        public Color lightColor;
        public Queue<LightNode> queueLightPasses = new Queue<LightNode>();

        private Queue<LightNode> queueLightRemovalPasses = new Queue<LightNode>();
        private Queue<LightNode> queueRemoveInvalidPositions = new Queue<LightNode>();
        private List<Vector3Int> removalPositions = new List<Vector3Int>();
        private List<LightSource> removalLights = new List<LightSource>();
        private float lightPassThreshold;


        private float passLightValue;

        #region INITIALIZE
        private void Awake()
        {
            worldPosition = new Vector3Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);        
        }



        #endregion



        #region PRIVATE_METHODS

        private void ClearCachedData()
        {
            removalLights.Clear();
            removalPositions.Clear();

            queueLightPasses.Clear();
            queueLightRemovalPasses.Clear();
            queueRemoveInvalidPositions.Clear();
        }

        private void PerformLightPasses(Queue<LightNode> queue, bool redChannel = true,
        bool greenChannel = true, bool blueChannel = true)
        {
            Debug.Log("PerformLightPasses");
            if (!redChannel && !greenChannel && !blueChannel)
                return;

            /* Generate a backup queue to refill the original queue after each channel
             * (since every channel execution empties the queue). */
            Queue<LightNode> queueBackup = new Queue<LightNode>(queue);

            // Spread light for each channel
            if (redChannel)
            {
                queue = new Queue<LightNode>(queueBackup);
                while (queue.Count > 0)
                    ExecuteLightingPass(queue, LightingChannelMode.RED);
            }
            if (greenChannel)
            {
                queue = new Queue<LightNode>(queueBackup);
                while (queue.Count > 0)
                    ExecuteLightingPass(queue, LightingChannelMode.GREEN);
            }
            if (blueChannel)
            {
                queue = new Queue<LightNode>(queueBackup);
                while (queue.Count > 0)
                    ExecuteLightingPass(queue, LightingChannelMode.BLUE);
            }
        }
        private void ExecuteLightingPass(Queue<LightNode> queue, LightingChannelMode mode)
        {
            //Debug.Log("ExecuteLightingPass");
            // Get the LightNode that's first in line
            LightNode light = queue.Dequeue();

            /* Obtain light values from the corresponding channel to lessen overhead
             * on extension passes. */
            switch (mode)
            {
                case LightingChannelMode.RED:
                    if (light.color.r <= 0f)
                        return;
                    passLightValue = light.color.r;
                    break;
                case LightingChannelMode.GREEN:
                    if (light.color.g <= 0f)
                        return;
                    passLightValue = light.color.g;
                    break;
                case LightingChannelMode.BLUE:
                    if (light.color.b <= 0f)
                        return;
                    passLightValue = light.color.b;
                    break;
                default:
                    return;
            }

            // Try and spread its light to surrounding blocks
            ExtendQueueLightPass(queue, light, passLightValue, Vector3Int.left, mode);
            ExtendQueueLightPass(queue, light, passLightValue, Vector3Int.down, mode);
            ExtendQueueLightPass(queue, light, passLightValue, Vector3Int.right, mode);
            ExtendQueueLightPass(queue, light, passLightValue, Vector3Int.up, mode);
        }

        private void ExtendQueueLightPass(Queue<LightNode> queue, LightNode lightNode, float lightValue,
        Vector3Int direction, LightingChannelMode mode)
        {
            //Debug.Log("ExtendQueueLightPass");
            var lightPositionDirection = lightNode.worldPosition + direction;

            /* Get the right chunk for this position.
             * We don't want to use GetChunk on every check, just the ones where we enter a new chunk. */
            var targetChunk = lightNode.chunk;
  

            // Calculate the light falloff for the position in the given direction
            Chunk.BlockType blockBack = targetChunk.GetBlockType(lightPositionDirection, Chunk.TilemapType.BACK_MAP);
            Chunk.BlockType blockFont = targetChunk.GetBlockType(lightPositionDirection, Chunk.TilemapType.FRONT_MAP);

            float blockFalloff;
            
            if (blockFont != Chunk.BlockType.AIR)
                blockFalloff = lightFallOffFront;
            else if (blockBack != Chunk.BlockType.AIR)
                blockFalloff = lightFallOffBack;
            else
                return;

            Color currentColor = targetChunk.GetBlockBlendedColor(lightPositionDirection);
            float lightValueDirection =
                (mode == LightingChannelMode.RED ?
                    currentColor.r :
                    (mode == LightingChannelMode.GREEN ?
                        currentColor.g :
                        currentColor.b));

            /* Spread light if the tile's channel color in this direction is lower in lightValue even after compensating
             * its falloff. lightPassThreshold acts as an additional performance boost and should be < light falloff back. */
            if (lightValueDirection + blockFalloff + lightPassThreshold < lightValue)
            {
                lightValue = Mathf.Clamp(lightValue - blockFalloff, 0f, 1f);
                Color newColor =
                    (mode == LightingChannelMode.RED ?
                        new Color(lightValue, currentColor.g, currentColor.b) :
                    (mode == LightingChannelMode.GREEN ?
                        new Color(currentColor.r, lightValue, currentColor.b) :
                        new Color(currentColor.r, currentColor.g, lightValue)));

                LightNode newLightNode;
                newLightNode.worldPosition = lightPositionDirection;
                newLightNode.color = newColor;
                newLightNode.chunk = targetChunk;
           
                targetChunk.SetBlockColor(lightPositionDirection, newColor);
                //Debug.Log($"{newColor}\t{direction}\t{queue.Count}");

                queue.Enqueue(newLightNode);
            }
        }
        #endregion


        #region PUBLIC_METHODS
        public void Initialized(Color color, float strength, float penetrationFront, float penetrationBack)
        {
            lightColor = color;
            lightStrength = strength;
            lightPenetrationFront = penetrationFront;
            lightPenetrationBack = penetrationBack;

            lightFallOffFront = 1.0f / lightPenetrationFront;
            lightFallOffBack = 1.0f / lightPenetrationBack;
            lightPassThreshold = lightFallOffBack * 0.999999f;
        }

        public void UpdateLight()
        {
            state = LightSourceState.UPDATE;

            ClearCachedData();


            //Create a struct as lightweight storage per visited block
            LightNode lightNode = new LightNode()
            {
                worldPosition = this.worldPosition,
                color = this.lightColor * lightStrength,
                chunk = LightManager.Instance.targetChunk,
            };
            if (lightNode.chunk == null) return;

            //LightNode lightNode = new LightNode(worldPosition, lightColor * lightStrength, LightManager.Instance.targetChunk);
            //if (lightNode.chunk == null) return;


            // Set the color of the light source's own tile.
            Color currentColor = lightNode.chunk.GetBlockBlendedColor(worldPosition);
            Color resultColor = new Color(
                Mathf.Max(currentColor.r, lightColor.r * lightStrength),
                Mathf.Max(currentColor.g, lightColor.g * lightStrength),
                Mathf.Max(currentColor.b, lightColor.b * lightStrength));
            lightNode.chunk.SetBlockColor(worldPosition, resultColor);

            // Spread light
            queueLightPasses.Enqueue(lightNode);
            PerformLightPasses(queueLightPasses);

            state = LightSourceState.IDLE;
        }


        #region - Remove Light
        public void RemoveLight()
        {
            state = LightSourceState.REMOVE;

            ClearCachedData();

            // Create a struct as lightweight data storage per block
            LightNode lightNode;
            lightNode.worldPosition = worldPosition;
            lightNode.chunk = LightManager.Instance.targetChunk;
            if (lightNode.chunk == null)
                return;

            /* If there are many LightSources close together, the color of the LightSource is drowned out.
             * To correctly remove this enough, instead of using the light's color to remove, use the color 
             * of the LightSource's tile if that color is greater. */
            Color currentColor = lightNode.chunk.GetBlockBlendedColor(worldPosition);
            lightNode.color = new Color(
                Mathf.Max(currentColor.r, lightColor.r * lightStrength),
                Mathf.Max(currentColor.g, lightColor.g * lightStrength),
                Mathf.Max(currentColor.b, lightColor.b * lightStrength));
            lightNode.chunk.SetBlockColor(worldPosition, Color.black);

            // Remove the actual light
            queueLightRemovalPasses.Enqueue(lightNode);
            PerformLightRemovalPasses(queueLightRemovalPasses);

            /* If we touched LightSources during removal spreading, we completely drowned out their color.
             * In order to correctly fill in the void, we need to update these lights. Create a new list
             * of them first to prevent problems from collection changes to removalLights somewhere else. */
            List<LightSource> voidLights = new List<LightSource>(removalLights);
            foreach (LightSource lightSource in voidLights)
                if (lightSource != null && lightSource != this && lightSource.state == LightSourceState.IDLE)
                    lightSource.UpdateLight();

            state = LightSourceState.IDLE;
        }

        private void PerformLightRemovalPasses(Queue<LightNode> queue)
        {
            /* Generate a backup queue to refill the original queue after each channel
             * (since every channel execution empties the queue).*/
            Queue<LightNode> queueBackup = new Queue<LightNode>(queue);

            // Remove all light from the given channel
            while (queue.Count > 0)
                ExecuteLightingRemovalPass(queue, LightingChannelMode.RED);
            // Spread stronger surrounding channel light that should fill this void. For example from another light source nearby
            RemoveInvalidSpreadPositions();
            PerformLightPasses(queueLightPasses, redChannel: true, greenChannel: false, blueChannel: false);

            // Repeat for other channels
            removalPositions.Clear();
            queueLightPasses.Clear();
            queue = new Queue<LightNode>(queueBackup);
            while (queue.Count > 0)
                ExecuteLightingRemovalPass(queue, LightingChannelMode.GREEN);
            RemoveInvalidSpreadPositions();
            PerformLightPasses(queueLightPasses, redChannel: false, greenChannel: true, blueChannel: false);

            removalPositions.Clear();
            queueLightPasses.Clear();
            queue = new Queue<LightNode>(queueBackup);
            while (queue.Count > 0)
                ExecuteLightingRemovalPass(queue, LightingChannelMode.BLUE);
            RemoveInvalidSpreadPositions();
            PerformLightPasses(queueLightPasses, redChannel: false, greenChannel: false, blueChannel: true);
        }

        private void RemoveInvalidSpreadPositions()
        {
            queueRemoveInvalidPositions = new Queue<LightNode>(queueLightPasses);
            queueLightPasses.Clear();
            foreach (LightNode lightNode in queueRemoveInvalidPositions)
                if (!removalPositions.Contains(lightNode.worldPosition))
                    queueLightPasses.Enqueue(lightNode);
        }
        #endregion


        #region Light removal passes
        private void ExecuteLightingRemovalPass(Queue<LightNode> queue, LightingChannelMode mode)
        {
            // Get the LightNode that's first in line
            LightNode light = queue.Dequeue();

            /* Detect passing over LightSources, while removing, to update them later. When we touch
             * such a LightSource, it means we completely drowned out its color and we need to
             * update the light again to fill in the blanks correctly. */
            LightSource lightSource = LightManager.Instance.GetLightSource(light.worldPosition);
            if (lightSource != null && !removalLights.Contains(lightSource))
                removalLights.Add(lightSource);

            // Track removed positions
            removalPositions.Add(light.worldPosition);

            /* Obtain light values from the corresponding channel to lessen overhead
             * on extension passes. */
            switch (mode)
            {
                case LightingChannelMode.RED:
                    if (light.color.r <= 0f)
                        return;
                    passLightValue = light.color.r;
                    break;
                case LightingChannelMode.GREEN:
                    if (light.color.g <= 0f)
                        return;
                    passLightValue = light.color.g;
                    break;
                case LightingChannelMode.BLUE:
                    if (light.color.b <= 0f)
                        return;
                    passLightValue = light.color.b;
                    break;
                default:
                    return;
            }

            // Try and spread the light removal
            ExtendQueueLightRemovalPass(queue, light, passLightValue, Vector3Int.left, mode);
            ExtendQueueLightRemovalPass(queue, light, passLightValue, Vector3Int.down, mode);
            ExtendQueueLightRemovalPass(queue, light, passLightValue, Vector3Int.right, mode);
            ExtendQueueLightRemovalPass(queue, light, passLightValue, Vector3Int.up, mode);
        }

        private void ExtendQueueLightRemovalPass(Queue<LightNode> queue, LightNode light, float lightValue,
        Vector3Int direction, LightingChannelMode mode)
        {
            Color currentColor = LightManager.Instance.targetChunk.GetBlockBlendedColor(light.worldPosition + direction);
            float lightValueDirection =
                (mode == LightingChannelMode.RED ?
                    currentColor.r :
                    (mode == LightingChannelMode.GREEN ?
                        currentColor.g :
                        currentColor.b));

            if (lightValueDirection > 0f)
            {
                // Continue removing and extending while the block I'm looking at has a lower lightValue for this channel
                if (lightValueDirection < lightValue)
                {
                    Color newColor =
                        (mode == LightingChannelMode.RED ?
                            new Color(0f, currentColor.g, currentColor.b) :
                            (mode == LightingChannelMode.GREEN ?
                                new Color(currentColor.r, 0f, currentColor.b) :
                                new Color(currentColor.r, currentColor.g, 0f)));

                    LightNode lightRemovalNode;
                    lightRemovalNode.worldPosition = light.worldPosition + direction;
                    lightRemovalNode.color = currentColor;
                    lightRemovalNode.chunk = LightManager.Instance.targetChunk;

                    LightManager.Instance.targetChunk.SetBlockColor(light.worldPosition + direction, newColor);

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
                    LightNode lightNode;
                    lightNode.worldPosition = light.worldPosition + direction;
                    lightNode.color = currentColor;
                    lightNode.chunk = LightManager.Instance.targetChunk;
                    queueLightPasses.Enqueue(lightNode);
                }
            }
        }
        #endregion

        #endregion





    }
}

