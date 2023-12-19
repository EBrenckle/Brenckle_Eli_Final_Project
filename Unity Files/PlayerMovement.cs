using System;
using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private float horizontal;
    private float speed = 8f;
    private float jumpingPower = 16f;
    private bool isFacingRight = true;

    private bool dashCheck1 = false;
    private bool dashCheck2 = false;
    public bool canDash;
    private bool isDashing;
    private float dashingPower = 150f;
    private float dashingTime = 0.2f;
    private float dashingCooldown = 1f;

    private bool isWallSliding;
    private float wallSlidingSpeed = 2f;

    public bool canWallJump;
    private bool isWallJumping;
    private float wallJumpingDirection;
    private float wallJumpingTime = 0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDuration = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(45f, 16f);

    public bool canDoubleJump;

    private bool phaseCheck1 = false;
    private bool phaseCheck2 = false;
    public bool canPhase;
    private float phaseTimer = .5f;
    private float phaseCooldown = 2f;

    public bool canPogo = true;
    public float pogoPower = 12.5f;
    public float pogoCooldown = .3f;

    private Vector3 respawnPoint;   
        
    [SerializeField] private Transform player;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D bc;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask roomGroundLayer;
    [SerializeField] private LayerMask spikeLayer;
    [SerializeField] private LayerMask thornLayer;
    [SerializeField] private TrailRenderer tr;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private Transform pogoCheck;

    //runs everyframe
    private void Update()
    {
        if (isDashing)
        {
            return;
        }

        //checks to see if both checks have been fulfilled before reseting cooldowns
        if (dashCheck1 && dashCheck2)
        {
            canDash = true;
        }

        if (phaseCheck1 && phaseCheck2)
        {
            canPhase = true;
        }

        //checks to see if play is grounded or is against a wall.
        if (Checker())
        {
            dashCheck2 = true;
            phaseCheck2 = true;
        }
        
        horizontal = Input.GetAxisRaw("Horizontal");

        //checks to see if player's boxcollider touches a spike or thorn then respawns them
        if (Physics2D.OverlapCircle(bc.transform.position, .5f, thornLayer))
        {
            transform.position = respawnPoint;
        }

        if (Physics2D.OverlapCircle(bc.transform.position, .5f, spikeLayer))
        {
            transform.position = respawnPoint;
        }

        //C# automatically maps "Jump" to the spacebar key. Changes character vertical velocity to jumping power.
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
        }

        //Checks to make sure player can't use other options and is able to double jump before using double jump resource. 
        if (Input.GetButtonDown("Jump") && !IsGrounded() && !IsWalled() && canDoubleJump)
        {
            StartCoroutine(DoubleJump());
        }

        //If player releases "Jump" button then cut velocity by half.
        if (Input.GetButtonUp("Jump") && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * 0.5f);
        }

        //Checks before player can pogo.
        if (Input.GetKeyDown(KeyCode.K) && !IsGrounded() && !IsWalled() && Pogoable() && canPogo)
        {
            StartCoroutine(Pogo());
        }

        //Checks if player can use powerup and then sets checks to false.
        if (Input.GetKeyDown(KeyCode.J) && canPhase)
        {
            phaseCheck1 = false; 
            phaseCheck2 = false;
            StartCoroutine(PhaseTest());
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            dashCheck1 = false;
            dashCheck2 = false;
            StartCoroutine(Dash());
        }

        WallSlide();
        WallJump();

        if (!isWallJumping)
        {
            Flip();
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }

        if (!isWallJumping)
        {
            rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
        }
    }

    //checks to see if a pogoable surface is bellow the player.
    private bool Pogoable()
    {
        if (Physics2D.OverlapCircle(pogoCheck.position, 1f, spikeLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //checks to see if player is touching the ground.
    private bool IsGrounded()
    {
        if (Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer))
        {
            return true;
        }
        if (Physics2D.OverlapCircle(groundCheck.position, 0.2f, roomGroundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //checks to see if the player is touching a wall.
    private bool IsWalled()
    {
        if (Physics2D.OverlapCircle(wallCheck.position, 0.2f, groundLayer))
        {
            return true;
        }
        if (Physics2D.OverlapCircle(wallCheck.position, 0.2f, roomGroundLayer))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //Becomes true once the player touches the ground OR touches the wall.
    private bool Checker()
    {
        if (IsGrounded() || IsWalled() == true)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    //If the player is holding into the wall and is not touching the ground then slows down vertical velocity. 
    private void WallSlide()
    {
        if (IsWalled() && !IsGrounded() && horizontal != 0f && canWallJump)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlidingSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    //if the player jumps while wall sliding then jump in specified direction with velocity based on variable given. Also stops player movement until duration is up.
    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpingDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpingTime;

            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump") && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            rb.velocity = new Vector2(wallJumpingDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpingDirection)
            {
                isFacingRight = !isFacingRight;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1f;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpingDuration);
        }
    }

    //stops wall jump
    private void StopWallJumping()
    {
        isWallJumping = false;
    }

    //flips character sprite/model if the character's velocity is less than or greater than 1.
    private void Flip()
    {
        if (isFacingRight && horizontal < 0f || !isFacingRight && horizontal > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    //Checks to see if the player passes through a trigger body with the specified layers.
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 12)
        {
            //sets the respawn point to players current position when triggered.
            respawnPoint = transform.position;
        }
        if (collision.gameObject.layer == 13)
        {
            respawnPoint = new Vector3(-8,-7,0);
        }
    }

    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        //stores original gravity then sets gravity to 0 until dashing time is up.
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(transform.localScale.x * dashingPower, 0f);
        tr.emitting = true;
        yield return new WaitForSeconds(dashingTime);
        tr.emitting = false;
        //returns original gravity
        rb.gravityScale = originalGravity;
        isDashing = false;
        // Wait until dash cooldown is over then set first check to true.
        yield return new WaitForSeconds(dashingCooldown);
        dashCheck1 = true;
    }
    private IEnumerator DoubleJump()
    {
        canDoubleJump = false;
        rb.velocity = new Vector2(rb.velocity.x, jumpingPower);
        yield return new WaitUntil(Checker);
        canDoubleJump = true;
    }
    private IEnumerator PhaseTest()
    {
        canPhase = false;
        //sets main box collider to a trigger body until phase time is up
        bc.isTrigger = true;
        yield return new WaitForSeconds(phaseTimer);
        bc.isTrigger = false;
        //Wait until cooldown is over then set first check to true.
        yield return new WaitForSeconds(phaseCooldown);
        phaseCheck1 = true;
    }
    private IEnumerator Pogo()
    {
        canPogo = false;
        //sets the player's vertical velocity to variable assigned power
        rb.velocity = new Vector2(rb.velocity.x, pogoPower);
        //Sets all powerups to true for later use by player.
        canDash = true;
        canDoubleJump = true;
        canPhase = true;
        //wait for pogo's cooldown then allow player to pogo again.
        yield return new WaitForSeconds(pogoCooldown);
        canPogo = true;
    }
}