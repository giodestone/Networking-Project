using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For a networked player on the local screen.
/// </summary>
public class NetworkPlayerInput : MonoBehaviour
{
    enum POSITION_UPDATE_MODE
    {
        NONE,
        PAKCETS,
        INTERPOLATING
    }

    /*For debug*/
    [SerializeField]
    private bool isPredictionEnabled = true;

    /*Connection Related*/
    PlayerID playerID;

    /*Components*/
    Animator animator;

    NetworkEventDispatcher networkEventDispatcher;

    /*Interpolation*/
    List<Packets.PositionPacket> positionPackets = new List<Packets.PositionPacket>(); //A priority queue would've been better
    const int maxPacketCache = 4;

    float lastTimeRecievedPositionPacket = 0.0f;
    const float maxTimeToWaitBeforeInterpolating = KeyboardPlayerInput.SendTimeDelay + 0.01f; //wait the time it takes to send a packet plus a bit more before starting to interpolate.

    POSITION_UPDATE_MODE positionUpdateMode = POSITION_UPDATE_MODE.PAKCETS;

    Vector2 interpolatedDirection = new Vector2();
    Vector2 averageVel = new Vector2();

    bool isPredictingPositions = true;
    bool onlyOnePacketMoving = false;
    const float maxTimeToLerpBetweenPos = 0.05f;
    float currentLerpTime = 0.0f; 
    Vector3 newPositionToLerpTo = new Vector3();
    float lerpingTimer = -1.0f;
    float averageVelDelta = -1.0f;
    /// <summary>
    /// Setup the events and find 
    /// </summary>
    void Start()
    {
        //Subscribe to position event.
        networkEventDispatcher = GameObject.FindWithTag("NetworkEventDispatcher").GetComponent<NetworkEventDispatcher>();
        networkEventDispatcher.PositionPacketRecieveEvent += HandlePositionPacketEvent;

        //find playerID
        playerID = gameObject.GetComponentInChildren<PlayerID>();
        if (playerID == null)
            throw new System.Exception("NetoworkPlayerInput is unable to find a PlayerID script in its parents' child objects.");

        //find animator
        animator = GetComponent<Animator>();
        if (animator == null)
            throw new System.Exception("Cannot find animator in KeyboardPlayerInput");
    }

    /// <summary>
    /// Store packets and change interpolation states.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void HandlePositionPacketEvent(object sender, NetworkEventArgs.PositionPacketEventArgs args)
    {
        if (args.PositionPacket.PlayerID == playerID.PlayerId) //If the player ID is to do with the current ID
        {
            /*Check if the packet sent already exists*/
            if (positionPackets.Contains(args.PositionPacket) || positionPackets.Exists(packet => Math.Abs(packet.timeSent - args.PositionPacket.timeSent) < 0.01f))
            {
                return; //if it does drop it
            }

            /*Add and sort that packet according to sent date*/
            positionPackets.Add(args.PositionPacket);
            positionPackets.Sort(); //See PacketHeader class for IComparable implemention - sorts time descending.

            if (positionPackets.Count > maxPacketCache)
                positionPackets.RemoveRange(maxPacketCache, positionPackets.Count - maxPacketCache); //purge old packets

            lastTimeRecievedPositionPacket = Time.time;

            /*Check if player has started moving or is still.*/
            if (positionPackets[0].isMoving && positionPackets[1].isMoving) //moving
            {
                isPredictingPositions = true;
                onlyOnePacketMoving = false;

                lerpingTimer = 0.0f;

                averageVel = (positionPackets[0].GetPositionVector2() - positionPackets[1].GetPositionVector2());
                averageVelDelta = positionPackets[0].timeSent - positionPackets[1].timeSent;
            }
            else if (positionPackets[0].isMoving) //beginning to move
            {
                lerpingTimer = 0.0f;

                onlyOnePacketMoving = true;
                isPredictingPositions = false;
            }
            else //stopped
            {
                isPredictingPositions = false;
                onlyOnePacketMoving = false;
            }
        }
    }

    /// <summary>
    /// Update animator variables to update current animation.
    /// </summary>
    /// <param name="direction"></param>
    void SetAnimator(Vector2 direction)
    {
        animator.SetFloat("HorizontalMovement", direction.x);
        animator.SetFloat("VerticalMovement", direction.y);
        animator.SetFloat("Magnitude", direction.magnitude);
    }

    /// <summary>
    /// Interpolate position based on positions.
    /// </summary>
    void Update()
    {
        if (positionPackets.Count > 1)
        {
            /*Determine what to do based on the above information*/

            if (isPredictingPositions && isPredictionEnabled) //If last two packets are moving then move and update animator.
            {
                var unitAverageVel = averageVel.normalized;
                SetAnimator(unitAverageVel);

                /*Get as percentage between two positions.*/
                float timeBetweenPackets = positionPackets[0].timeSent - positionPackets[1].timeSent;
                float deltaTimeAsPercentage = Time.deltaTime / timeBetweenPackets;
                lerpingTimer += deltaTimeAsPercentage;

                transform.position = Vector2.LerpUnclamped(positionPackets[1].GetPositionVector2(), positionPackets[0].GetPositionVector2(), lerpingTimer);
            }
            else
            {
                SetAnimator(Vector2.zero);
                transform.position = positionPackets[0].GetPositionVector2();
                return;
            }

        }
    }
}
