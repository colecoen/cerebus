using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2 : MonoBehaviour
{
    int facingDirection = 1;

    public float moveSpeed = 10.0f;
    public float jumpForce = 16.0f;
    public int maxJumps = 1;
    int jumpsRemaining;
    public float airDragMultiplier = 0.95f;
    public float variableJumpHeightMultiplier = 0.5f;
    bool checkJumpMultiplier;
    float jumpTimer;
    public float jumpTimerSet = 0.085f;
    bool isAttemptingToJump;
    public Vector2 currentVelocity;
    public bool canMove;

    [Header("Wall Jumping")]
    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;
    Vector2 wallJumpForce;
    bool wallJumpClimbing;
    bool wallJumpOffing;
    bool wallJumpLeaping;
    bool wallJumping;
    public float wallJumpTime;
    float wallJumpTimer;
    int jumpedFromDir;
    bool justWallJumped;

    [Header("Wall Sliding")]
    public float startWallSlideTime;
    float startWallSlideTimer;
    bool wallSliding;
    public float wallSlideDownSpeed = 4.0f;
    public float wallSlideUpSpeed = 1.0f;

    private bool facingRight = true;
    private float xInput;
    private float yInput;

    bool isWalking;
    bool onWall;
    int wallDirX;


    [Header("Raycasts")]
    public CollisionInfo collisions;
    public LayerMask collisionLayer;
    RaycastOrigins raycastOrigins;
    const float dstBetweenRays = 0.25f;
    float skinWidth = 0.15f;
    float verticalRaySpacing;
    float verticalRayCount;
    float verticalRayLength = 0.25f;
    float horizontalRayCount;
    float horizontalRaySpacing;
    float horizontalRayLength = 0.30f;

    [Header("Other References")]
    Rigidbody2D rb;
    PlayerInput input;
    PlayerStates playerStates;
    Collider2D col;
    Animator anim;

    private void Awake()
    {
        InitializeThings();
    }
    void Start()
    {
        CalculateRaySpacing();
        canMove = true;
    }

    void Update()
    {
        RaycastHandling();
        CheckInput();
        UpdateAnimations();
        CanWallSlide();
        CheckJump();
        HandleTimers();
        HandleAdditionalPhysics();

        currentVelocity = new Vector2(rb.velocity.x, rb.velocity.y);
    }

    private void FixedUpdate()
    {
        Move(xInput);

        //HandleWallJumping();

    }

    void CheckInput()
    {
        xInput = input.directionalInput.x;
        yInput = input.directionalInput.y;
        CheckMovementDirection();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (CanNormalJump())
            {
                NormalJump();
            }
            else if (CanWallJump())
            {
                CalculateWallJumpForce();
                wallJumping = true;
                wallJumpTimer = wallJumpTime;
            }
            else
            {
                jumpTimer = jumpTimerSet;
                isAttemptingToJump = true;
            }
        }
        if (checkJumpMultiplier && !Input.GetKey(KeyCode.Space))
        {
            checkJumpMultiplier = false;
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpHeightMultiplier);
        }
    }

    void Move(float _xinput)
    {

        if (!collisions.below && !wallSliding && xInput == 0 && !wallJumping)
        {
            rb.velocity = new Vector2(rb.velocity.x * airDragMultiplier, rb.velocity.y);
        }
        else if (canMove)
        {
            rb.velocity = new Vector2(_xinput * moveSpeed, rb.velocity.y);
        }

        if (CanWallSlide())
        {
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideDownSpeed, wallSlideUpSpeed));
        }
    }

    void HandleWallJumping()
    {
        // if (wallJumping)
        // {
        //     if (wallJumpClimbing)
        //     {
        //         if (xInput == 0)
        //         {
        //             wallJumpForce = new Vector2(-wallDirX * wallJumpOff.x, wallJumpOff.y);
        //             wallJumpTimer = -100f;
        //         }
        //         else
        //         {
        //             rb.velocity = wallJumpForce;
        //         }
        //     }
        //     else if (wallJumpOffing)
        //     {
        //         wallJumpTimer = -100f;
        //         rb.velocity = wallJumpForce;
        //     }
        // }
    }

    void CheckJump()
    {
        if (jumpTimer > 0)
        {
            if (!collisions.below && onWall && xInput != 0 && xInput != facingDirection) // may need to modify this
            {
                //WallJumpForce();
            }
            else if (collisions.below) // don't call CanJump here because this is coyote timing and is only applied once hitting the ground
            {
                NormalJump();
            }
        }
        if (isAttemptingToJump)
        {
            jumpTimer -= Time.deltaTime;
        }
    }

    void NormalJump()
    {
        jumpsRemaining -= 1;
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        jumpTimer = 0;
        isAttemptingToJump = false;
        checkJumpMultiplier = true;
    }

    void CalculateWallJumpForce()
    {
        wallSliding = false;
        justWallJumped = true;
        jumpsRemaining = maxJumps;
        jumpsRemaining--;

        jumpTimer = 0;
        isAttemptingToJump = false;

        jumpedFromDir = collisions.left ? -1 : 1;

        if (wallDirX == xInput)
        {
            wallJumpClimbing = true;
            wallJumpForce = new Vector2(-wallDirX * wallJumpClimb.x, wallJumpClimb.y);
            StartCoroutine(WallJumpClimbRoutine());
        }
        else if (xInput == 0)
        {
            wallJumpOffing = true;
            wallJumpForce = new Vector2(-wallDirX * wallJumpOff.x, wallJumpOff.y);
        }
        else
        {
            wallJumpLeaping = true;
            wallJumpForce = new Vector2(-wallDirX * wallLeap.x, wallLeap.y);
        }
    }

    IEnumerator WallJumpClimbRoutine()
    {
        while (wallJumpClimbing)
        {
            canMove = false;
            if (xInput != 0)
            {
                rb.velocity = wallJumpForce;
            }
            else
            {
                //rb.velocity = new Vector2(-wallDirX * wallJumpOff.x, wallJumpOff.y);
                rb.velocity = new Vector2(0, 0);
                //rb.velocity = new Vector2(0, wallJumpOff.y);
                wallJumpTimer = -10000f;
            }
            yield return new WaitForFixedUpdate();
        }
        canMove = true;
    }

    bool CanNormalJump()
    {
        if ((collisions.below || jumpsRemaining > 0) && !wallSliding)
        {
            if (jumpsRemaining == 0)
                jumpsRemaining = maxJumps;
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CanWallJump()
    {
        if (!collisions.below && wallSliding)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool CanWallSlide()
    {
        if (((onWall && !collisions.below && xInput == facingDirection) || (wallSliding && onWall && !collisions.below)) && rb.velocity.y < 0.0f)
        {
            wallSliding = true;
            return true;
        }
        else if (justWallJumped && onWall && !collisions.below && xInput == facingDirection)
        {
            wallSliding = true;
            return true;
        }
        else
        {
            wallSliding = false;
            return false;
        }
    }

    void CheckMovementDirection()
    {
        if (facingRight && xInput < 0 && !wallSliding && !wallJumping)
        {
            FlipSprite();
        }
        else if (!facingRight && xInput > 0 && !wallSliding && !wallJumping)
        {
            FlipSprite();
        }

        isWalking = (rb.velocity.x > 0.01f || rb.velocity.x < -0.01f) && collisions.below ? true : false;
        onWall = collisions.left || collisions.right ? true : false;

        if (onWall)
        {
            if (collisions.left)
            {
                wallDirX = -1;
            }
            else if (collisions.right)
            {
                wallDirX = 1;
            }
        }

        if (wallJumping)
        {
            if (jumpedFromDir == -1 && rb.velocity.x > 0.0f)
            {
                ForceFaceRight();
            }
            else if (jumpedFromDir == 1 && rb.velocity.y < 0.0f)
            {
                ForceFaceLeft();
            }
        }
    }

    void FlipSprite()
    {
        facingDirection *= -1;
        facingRight = !facingRight;
        Vector3 _localScale = transform.localScale;
        _localScale.x *= -1f;
        transform.localScale = _localScale;
    }

    void ForceFaceRight()
    {
        facingRight = true;
        facingDirection = 1;
        Vector3 _localScale = transform.localScale;
        _localScale.x = facingDirection;
        transform.localScale = _localScale;
    }

    void ForceFaceLeft()
    {
        facingRight = false;
        facingDirection = -1;
        Vector3 _localScale = transform.localScale;
        _localScale.x = facingDirection;
        transform.localScale = _localScale;
    }

    void HandleTimers()
    {
        if (wallJumping)
        {
            wallJumpTimer -= Time.deltaTime;
            if (wallJumpTimer <= 0)
            {
                jumpedFromDir = 0;
                wallJumpTimer = 0.0f;
                wallJumpForce = Vector2.zero;
                wallJumping = wallJumpClimbing = wallJumpOffing = wallJumpLeaping = false;
            }
        }
    }

    void HandleAdditionalPhysics()
    {
        if (collisions.below)
        {
            justWallJumped = false;
        }
    }

    void UpdateAnimations()
    {
        anim.SetBool("isWalking", isWalking);
        anim.SetBool("isGrounded", collisions.below);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("isWallSliding", wallSliding);
    }

    void InitializeThings()
    {
        if (playerStates == null)
            playerStates = new PlayerStates();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        input = GetComponent<PlayerInput>();
        anim = GetComponent<Animator>();

        jumpsRemaining = maxJumps;
    }

    public void CalculateRaySpacing()
    {
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;


        horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    public void UpdateRaycastOrigins()
    {
        Bounds bounds = col.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    void UpwardsCollisions()
    {
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOriginUp = raycastOrigins.topLeft;
            rayOriginUp += Vector2.right * (verticalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOriginUp, Vector2.up, verticalRayLength, collisionLayer);
            Debug.DrawRay(rayOriginUp, new Vector3(0, verticalRayLength, 0), Color.red);

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
            Vector2 rayOriginDown = raycastOrigins.bottomLeft;
            rayOriginDown += Vector2.right * (verticalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOriginDown, Vector2.down, verticalRayLength, collisionLayer);
            Debug.DrawRay(rayOriginDown, new Vector3(0, -verticalRayLength, 0), Color.red);

            if (hit)
            {
                collisions.below = true;
            }
            else
            {
                collisions.below = false;
            }
        }
    }

    void LeftwardsCollisions()
    {
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOriginLeft = raycastOrigins.bottomLeft;
            rayOriginLeft += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOriginLeft, Vector2.left, horizontalRayLength, collisionLayer);
            Debug.DrawRay(rayOriginLeft, Vector2.left * horizontalRayLength, Color.red);

            if (hit)
            {
                collisions.left = true;
            }
            else
            {
                collisions.left = false;
            }
        }
    }

    void RightwardsCollisions()
    {
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOriginRight = raycastOrigins.bottomRight;
            rayOriginRight += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOriginRight, Vector2.right, horizontalRayLength, collisionLayer);
            Debug.DrawRay(rayOriginRight, Vector2.right * horizontalRayLength, Color.red);

            if (hit)
            {
                collisions.right = true;
            }
            else
            {
                collisions.right = false;
            }
        }
    }

    void RaycastHandling()
    {
        UpdateRaycastOrigins();
        collisions.Reset();
        UpwardsCollisions();
        DownwardsCollisions();
        LeftwardsCollisions();
        RightwardsCollisions();
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
