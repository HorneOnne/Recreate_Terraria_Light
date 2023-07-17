using System.Collections;
using UnityEngine;

namespace RecreateBlockLight2D
{
    public static class Utilities
    {
        /// <summary>
        /// Gets the color with the maximum intensity for each channel.
        /// </summary>
        /// <param name="color">The original color.</param>
        /// <param name="otherColor">The other color to compare with.</param>
        /// <returns>The color with the maximum intensity for each channel.</returns>
        public static Color GetMaxIntensity(Color color, Color otherColor)
        {
            return new Color(
                Mathf.Max(color.r, otherColor.r),
                Mathf.Max(color.g, otherColor.g),
                Mathf.Max(color.b, otherColor.b),
                Mathf.Max(color.a, otherColor.a)
            );
        }


        public static IEnumerator WaitAfter(float time, System.Action callback)
        {
            yield return new WaitForSeconds(time);
            callback?.Invoke();
        }
    }
}

