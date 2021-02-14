using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Run / Walk")]
    public float runSpeed;
    public float walkSpeed = 25f;

    [Header("Jump")]
    public float jumpSpeed = 45f;
    public float fallSpeed = 45f;
    public int jumpSteps = 20;
    public int jumpThreshold = 7;
    int stepsJumped = 0;

    public Vector2 currentVelocity;
    public float normalGravity;
    private float previousGravityScale;

    private float xInput;
    private float yInput;

    int roofRaycount;
    const float dstBetweenRays = 0.25f;
    float skinWidth = 0.15f;
    float verticalRaySpacing;
    float verticalRayCount;
    float verticalRayLength = 0.17f;
    RaycastOrigins raycastOrigins;
    public CollisionInfo collisions;


    private static PlayerController _instance;
    public static PlayerController instance
    {
        get
        {
            if (PlayerController._instance == null)
            {
                PlayerController._instance = FindObjectOfType<PlayerController>();
                if (PlayerController._instance == null)
                {
                    Debug.LogError("No player in the scene");
                }
                else
                {
                    DontDestroyOnLoad(PlayerController._instance.gameObject);
                }
            }
            return PlayerController._instance;
        }
    }

    public PlayerInput input;
    public PlayerStates playerStates;
    public LayerMask collisionLayer;
    private Rigidbody2D rb;
    private Collider2D col;

    void Awake()
    {
        if (PlayerController._instance == null)
        {
            PlayerController._instance = this;
            DontDestroyOnLoad(this);
        }
        else if (this != PlayerController._instance)
        {
            Destroy(this.gameObject);
            return;
        }

        InitializeThings();
    }
    void Start()
    {
        playerStates.facingRight = true;
        CalculateRaySpacing();
    }


    void Update()
    {
        RaycastHandling();


        currentVelocity = rb.velocity;
        CheckInput();

    }

    void FixedUpdate()
    {
        Move(xInput);

        Jump();
    }

    void CheckInput()
    {
        xInput = input.directionalInput.x;
        yInput = input.directionalInput.y;

        if ((double)xInput > 0.0 && !playerStates.facingRight)
        {
            FlipCharacterSprite();
            playerStates.facingRight = true;
        }
        else if ((double)xInput < 0.0 && playerStates.facingRight)
        {
            FlipCharacterSprite();
            playerStates.facingRight = false;
        }

        if (input.jumpPressed && playerStates.grounded)
            playerStates.jumping = true;

        if (!input.jumpPressed && playerStates.jumping)
            JumpReleased();
    }

    private void Move(float _xinput)
    {
        rb.velocity = new Vector2(_xinput * runSpeed, rb.velocity.y);

        playerStates.running = Mathf.Abs(rb.velocity.x) > 0 ? true : false;

    }

    private void Jump()
    {
        if (playerStates.jumping)
        {
            if (stepsJumped < jumpSteps && !collisions.above)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);
                stepsJumped++;
            }
            else
            {
                StopJumpSlow();
            }
        }

        if (rb.velocity.y < -Mathf.Abs(fallSpeed))
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -Mathf.Abs(fallSpeed), Mathf.Infinity));
        }
    }

    private void JumpReleased()
    {
        if (stepsJumped < jumpSteps && stepsJumped > jumpThreshold)
        {
            StopJumpQuick();
        }
        else if (stepsJumped < jumpThreshold)
        {
            StopJumpSlow();
        }
    }

    void StopJumpSlow()
    {
        stepsJumped = 0;
        playerStates.jumping = false;
    }

    void StopJumpQuick()
    {
        stepsJumped = 0;
        playerStates.jumping = false;
        rb.velocity = new Vector2(rb.velocity.x, 0);
    }

    public void FlipCharacterSprite()
    {
        playerStates.facingRight = !playerStates.facingRight;
        Vector3 _localScale = transform.localScale;
        _localScale.x *= -1f;
        transform.localScale = _localScale;
    }

    public void GravityToggle(bool gravityToggle)
    {
        float gravityScale = rb.gravityScale;
        if ((double)rb.gravityScale > (double)Mathf.Epsilon && !gravityToggle)
        {
            previousGravityScale = rb.gravityScale;
            rb.gravityScale = 0.0f;
        }
        else
        {
            if ((double)rb.gravityScale > (double)Mathf.Epsilon || !gravityToggle)
            {
                return;
            }
            rb.gravityScale = previousGravityScale;
            previousGravityScale = 0.0f;
        }
    }

    void InitializeThings()
    {
        if (playerStates == null)
            playerStates = new PlayerStates();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        input = GetComponent<PlayerInput>();

        currentVelocity = Vector2.zero;
        previousGravityScale = normalGravity;
        GravityToggle(true);
    }

    public void CalculateRaySpacing()
    {
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;


        //horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

        // calculate the spacing between each ray
        //horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public void UpdateRaycastOrigins()
    {
        Bounds bounds = col.bounds; // bounds of our collider
        bounds.Expand(skinWidth * -2); // "shrinks" the collider by the skinWidth on all sides

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void UpwardsCollisions()
    {
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, verticalRayLength, collisionLayer);
            Debug.DrawRay(rayOrigin, new Vector3(0, verticalRayLength, 0), Color.red);

            if (hit)
            {
                collisions.above = true;
            }
            else
            {
                collisions.above = false;
            }
        }
    }

    void DownwardsCollisions()
    {
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = raycastOrigins.bottomLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, verticalRayLength, collisionLayer);
            Debug.DrawRay(rayOrigin, new Vector3(0, -verticalRayLength, 0), Color.red);

            if (hit)
            {
                collisions.below = true;
                playerStates.grounded = true;
            }
            else
            {
                collisions.below = false;
                playerStates.grounded = false;
            }
        }
    }

    void RaycastHandling()
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        UpwardsCollisions();
        DownwardsCollisions();
    }

    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }

    [System.Serializable]
    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public void Reset()
        {
            above = below = false;
            left = right = false;
        }
    }
}
