using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class LightValueText : MonoBehaviour
{
    [SerializeField] private TextMeshPro lightValueText;
    public List<string> history = new List<string>();   
    
    public void SetLightValue(float value)
    {
        lightValueText.text = value.ToString();
        history.Add(value.ToString());  
    }
}
