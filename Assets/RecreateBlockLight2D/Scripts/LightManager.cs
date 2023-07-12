using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecreateBlockLight2D
{
    public class LightManager : MonoBehaviour
    {
        public static LightManager Instance { get; private set; }
        [SerializeField] private LightSource lightSourcePrefab;
        private Dictionary<Vector3Int, LightSource> ambientLightSources;


        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            ambientLightSources = new Dictionary<Vector3Int, LightSource>();
        }


        #region PRIVATE_METHODS
        private LightSource CreateLightSource(Vector3Int worldPosition)
        {
            if (HasAmbientLightSource(worldPosition) == false)
            {
                LightSource lightSource = Instantiate(lightSourcePrefab, worldPosition, Quaternion.identity, this.transform);
                ambientLightSources.Add(worldPosition, lightSource);
                return lightSource;
            }
            return null;
        }

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



        public void AddAmbientLight(Chunk chunk, Vector3Int worldPosition)
        {
            RemoveLightSource(worldPosition);
            List<Vector3Int> NB4Check = Get4Neightbours(worldPosition);

            for (int i = 0; i < NB4Check.Count; i++)
            {
                if (chunk.IsAIRBlock(NB4Check[i]) == true)
                {
                    CreateLightSource(NB4Check[i]);
                    //Debug.Log($"Add Ambient Light source. {NB4Check[i]} \t {chunk.GetBlockType(NB4Check[i])}");
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
    }
}