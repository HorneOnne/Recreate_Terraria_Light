using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecreateBlockLight2D
{
    public class LightManager : MonoBehaviour
    {   
        public static LightManager Instance { get; private set; }
        [SerializeField] private LightSource lightSourcePrefab;
        private HashSet<Vector3Int> ambientLightSet = new HashSet<Vector3Int>();

        private void Awake()
        {
            Instance = this;
        }


        #region PUBLIC_METHODS

        public LightSource CreateLightSource(Vector3Int worldPosition)
        {
            if(ambientLightSet.Contains(worldPosition) == false)
            {
                ambientLightSet.Add(worldPosition);
                LightSource lightSource = Instantiate(lightSourcePrefab, worldPosition, Quaternion.identity, this.transform);
                return lightSource;
            }
            return null;
        }    
        #endregion
    }
}