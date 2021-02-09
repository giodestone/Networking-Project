using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script for a tree.
/// </summary>
public class Tree : MonoBehaviour
{
    bool shouldShake = false;
    float curShakeTime = 0.0f;
    float maxShakeTimeSeconds = 1.0f;

    Vector3 originalPos;

    [SerializeField]
    SpriteRenderer spriteRenderer;
    
    /// <summary>
    /// Verfiy that the sprite rendere is set and get the original position.
    /// </summary>
    void Start()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("Sprite Renderer not found at Tree!");
            return;
        }

        //get original position
        originalPos = spriteRenderer.transform.position;
    }

    /// <summary>
    /// Shake the tree if it should be shaking.
    /// </summary>
    void Update()
    {
        //shake the tree along x
        if (shouldShake)
        {
            curShakeTime += Time.deltaTime;
            /* Multiply by pi to get half (180.f), divide to change the intensity of the shakes. */
            spriteRenderer.transform.position = new Vector3((Mathf.Sin(curShakeTime * Mathf.PI * 6.0f) / 20.0f)+ originalPos.x, spriteRenderer.transform.position.y);

            if (curShakeTime >= maxShakeTimeSeconds)
            {
                shouldShake = false;
                spriteRenderer.transform.position = originalPos;
            }
        }
    }

    /// <summary>
    /// If the local player is colliding and pressing shake tree the tree will be set to play the shake animation.
    /// </summary>
    /// <param name="col"></param>
    void OnTriggerStay2D(Collider2D col)
    {
        if (col.gameObject.CompareTag("LocalPlayer"))
        {
            if (Input.GetButtonDown("ShakeTree") && !shouldShake) //Shake the tree if its not shaking yet.
            {
                shouldShake = true;
                curShakeTime = 0.0f;
            }
        }
    }
}
