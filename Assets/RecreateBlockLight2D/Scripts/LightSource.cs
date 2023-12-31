﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecreateBlockLight2D
{
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

        public LightNode(LightSource lightSource, Chunk chunk)
        {
            this.worldPosition = lightSource.worldPosition;
            this.color = lightSource.lightColor;
            this.chunk = chunk;
        }
    }

    public class LightSource : MonoBehaviour
    {
        public Color lightColor;
        public Vector3Int worldPosition;


        [Header("Testing purpose")]
        [SerializeField] private SpriteRenderer sr;
       

        private void Awake()
        {
            worldPosition = new Vector3Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y), 0);        
        }

  
        public void Initialized(Color color, float strength)
        {
            lightColor = color;       
        }


    }
}

