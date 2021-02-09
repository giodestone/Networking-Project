using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For attaching to a camera and making it follow an object smoothly.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    GameObject objectToFollow;

    Camera cameraComponent; //Camera component which should be in the same object as the script.

    Vector3 velocity = Vector3.zero;
    float originalZ;

    [SerializeField]
    float dampTime = 0.05f;

    bool localPlayerExists = false;

    // Start is called before the first frame update
    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
            throw new System.Exception("Camera not found at Camera Follow!");

        GameObject.FindGameObjectWithTag("ClientConnectionManager").GetComponent<ClientConnectionManager>().NetworkStatusUpdateEvent += HandleNetworkStatusUpdateEvent;

        originalZ = cameraComponent.transform.position.z;
    }

    void HandleNetworkStatusUpdateEvent(object sender, NetworkEventArgs.NetworkStatusUpdateEventArgs args)
    {
        switch (args.NetworkStatusEventType)
        {
            case NetworkEventArgs.NetworkStatusUpdateEventArgs.NETWORK_STATUS_EVENT_TYPE.CONNECTED:
                if (!localPlayerExists) //This is mostly for debugging!
                    Invoke("FindPlayer", 0.5f); //find the player in just a moment - may not have been instanced quite just yet.
                break;
        }
    }

    void FindPlayer()
    {
        objectToFollow = GameObject.FindWithTag("LocalPlayer");
        if (objectToFollow == null)
            throw new System.Exception("Client: Camera follow not found LocalPlayer.");

        localPlayerExists = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (localPlayerExists)
        {
            var objPos = objectToFollow.transform.position;
            var cameraPos = cameraComponent.transform.position;
            var deltaPos = objPos - cameraPos;

            cameraComponent.transform.position = Vector3.SmoothDamp(transform.position, objPos, ref velocity, dampTime);
            cameraComponent.transform.position = new Vector3(cameraComponent.transform.position.x, cameraComponent.transform.position.y, originalZ); //freeze z
        }
    }
}
