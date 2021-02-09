using System;
using UnityEngine;

/// <summary>
/// For sending poisiton to server and moving the local player. 
/// </summary>
public class KeyboardPlayerInput : MonoBehaviour
{
    [SerializeField]
    float movementSpeed = 5.0f;

    Animator animator = null;

    NetworkEventDispatcher networkEventDispatcher;

    PlayerID playerID;

    float nextTimeToSendPacketUpdate;
    [SerializeField]
    public const float SendTimeDelay = 0.03f;

    Vector3 movement = new Vector3();

    /// <summary>
    /// Get components and subscribe to events.
    /// </summary>
    void Start()
    {
        /*Get animator*/
        animator = GetComponent<Animator>();
        if (animator == null)
            throw new Exception("Cannot find animator in KeyboardPlayerInput");

        /*Get PlayerID component*/
        playerID = gameObject.GetComponentInChildren<PlayerID>();

        if (playerID == null)
            throw new Exception("PlayerID not found! at Keyboard Player Input.");

        /*Get network event dispatcher*/
        networkEventDispatcher = GameObject.FindWithTag("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>();
        if (networkEventDispatcher == null)
            throw new Exception("NetworkEventDispatcher script not found! At Keyboard Player Input.");

        nextTimeToSendPacketUpdate = Time.time + 0.25f;
    }

    /// <summary>
    /// Move the player and send out packets every Time.time + SendTimeDelay.
    /// </summary>
    void Update()
    {
        MovePlayerKeyboard();

        if (Time.time >= nextTimeToSendPacketUpdate)
        {
            nextTimeToSendPacketUpdate = SendTimeDelay + Time.time;
            Packets.PositionPacket playerPosPacket = new Packets.PositionPacket();

            playerPosPacket.timeSent = networkEventDispatcher.CurrentTimeRelativeToServer;

            playerPosPacket.PlayerID = playerID.PlayerId; //This player has sent a new position update.
            playerPosPacket.x = transform.position.x;
            playerPosPacket.y = transform.position.y;

            playerPosPacket.speed = movement.magnitude * movementSpeed;

            if (playerPosPacket.speed > 0.0001f)
                playerPosPacket.isMoving = true;
            else
                playerPosPacket.isMoving = false;

            networkEventDispatcher.SendPacket(playerPosPacket);
        }
    }

    /// <summary>
    /// Move the player based on the keyboard and update animation
    /// </summary>
    void MovePlayerKeyboard()
    {
        /*Update animation*/
        movement = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0.0f);
        movement.Normalize();

        animator.SetFloat("HorizontalMovement", movement.x);
        animator.SetFloat("VerticalMovement", movement.y);
        animator.SetFloat("Magnitude", movement.magnitude);

        /*Get new position and move*/
        var newPos = transform.position + movement * Time.deltaTime * movementSpeed;

        GetComponent<Rigidbody2D>().MovePosition(newPos); //move the players rigid body so it doesn't jerk when colliding with something
    }
}
