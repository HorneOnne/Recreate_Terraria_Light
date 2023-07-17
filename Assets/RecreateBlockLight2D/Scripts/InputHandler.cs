using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace RecreateBlockLight2D
{
    public class InputHandler : MonoBehaviour
    {
        private Vector3Int worldPosition;
        private static Camera mainCam;
        [SerializeField] private LayerMask chunkLayer;

        [SerializeField] private SpriteRenderer ghost;



        // Cached
        private bool isPressShift;
        private LightManager lightManager;

        // Temp
        [SerializeField] private Chunk targetChunk;

        private void Awake()
        {
            mainCam = Camera.main;
        }

        private void Start()
        {
            lightManager = LightManager.Instance;
        }

        private void Update()
        {
            worldPosition = GetMouseWorldPosition();
            ghost.transform.position = worldPosition;
            isPressShift = Input.GetKey(KeyCode.LeftShift);


            if (!isPressShift)
            {
                if (Input.GetMouseButton(0))
                {
                    Vector2 mouseBtnPos = new Vector2(worldPosition.x, worldPosition.y);
                    if (Physics2D.Raycast(mouseBtnPos, Vector3.forward, 100, chunkLayer))
                    {
                        if (targetChunk.GetBlockType(worldPosition, Chunk.TilemapType.FRONT_MAP) == Chunk.BlockType.AIR)
                        {
                            targetChunk.SetBlock(worldPosition, Chunk.TilemapType.FRONT_MAP, Chunk.BlockType.DIRT);

                            // LIGHT
                            targetChunk.SetBlockColor(worldPosition, Color.black);
                            lightManager.AddAmbientLight(targetChunk, worldPosition);


                            /* We want to darken the surroundings now that we placed a block. Simulate this by placing a light and
                             * instead of updating it, simply remove the surrounding light using the current color. */
                            Color currentColor = targetChunk.GetBlockBlendedColor(worldPosition);
                            LightSource existingLight = lightManager.GetLightSource(worldPosition);
                            if (existingLight == null)
                            {
                                existingLight = lightManager.CreateLightSource(worldPosition, currentColor,
                                    lightManager.ambientLightStrength);
                            }
                            lightManager.RemoveLightSource(existingLight);


                        }
                    }

                }
                else if (Input.GetMouseButton(1))
                {
                    Vector2 mouseBtnPos = new Vector2(worldPosition.x, worldPosition.y);
                    if (Physics2D.Raycast(mouseBtnPos, Vector3.forward, 100, chunkLayer))
                    {
                        targetChunk.RemoveBlock(worldPosition, Chunk.TilemapType.FRONT_MAP);

                        // LIGHT
                        lightManager.RemoveAmbientLight(targetChunk, worldPosition);
                    }
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    Vector2 mouseBtnPos = new Vector2(worldPosition.x, worldPosition.y);
                    if (Physics2D.Raycast(mouseBtnPos, Vector3.forward, 100, chunkLayer))
                    {
                        if (targetChunk.GetBlockType(worldPosition, Chunk.TilemapType.BACK_MAP) == Chunk.BlockType.AIR)
                        {
                            targetChunk.SetBlock(worldPosition, Chunk.TilemapType.BACK_MAP, Chunk.BlockType.DIRT);

                            // LIGHT
                            lightManager.AddAmbientLight(targetChunk, worldPosition);
                        }
                    }
                }
                else if (Input.GetMouseButton(1))
                {
                    Vector2 mouseBtnPos = new Vector2(worldPosition.x, worldPosition.y);
                    if (Physics2D.Raycast(mouseBtnPos, Vector3.forward, 100, chunkLayer))
                    {
                        targetChunk.RemoveBlock(worldPosition, Chunk.TilemapType.BACK_MAP);

                        // LIGHT
                        lightManager.RemoveAmbientLight(targetChunk, worldPosition);
                    }
                }
            }

        }



        #region UTILITIES
        public static Vector3Int GetMouseWorldPosition()
        {
            Vector2 mousePosition = mainCam.ScreenToWorldPoint(Input.mousePosition);
            return new Vector3Int(Mathf.RoundToInt(mousePosition.x), Mathf.RoundToInt(mousePosition.y), 0);
        }

        private IEnumerator WaitAfter(float time, Action callback)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }

        #endregion
    }
}

