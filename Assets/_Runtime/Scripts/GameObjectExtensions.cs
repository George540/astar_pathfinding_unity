﻿using UnityEngine;

public static class GameObjectExtensions
{
    /// <summary>
    /// Destroy all children of this game object.
    /// <br /> <paramref name="delay"/> (optional): The amount of time to delay before destroying the objects.
    /// </summary>
    /// <param name="gameObject"></param>
    /// <param name="delay">The </param>
    public static void DestroyChildren(this GameObject gameObject, float delay = 0)
    {
        for (var i = 0; i < gameObject.transform.childCount; ++i)
        {
            if (Application.isPlaying)
            {
                if (delay <= 0)
                    Object.Destroy(gameObject.transform.GetChild(i).gameObject);
                else
                    Object.Destroy(gameObject.transform.GetChild(i).gameObject, delay);
            }
            else
            {
                Object.DestroyImmediate(gameObject.transform.GetChild(i).gameObject);
                --i;
            }
        }
    }
}
