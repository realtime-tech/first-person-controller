using UnityEngine;

/// <summary>
/// A realistic first-person player controller with smooth camera movement,
/// head bobbing, momentum-based movement, and extensive customization.
/// Attach to a capsule with a Camera as a child object.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("=== CAMERA SETTINGS ===")]
    [Tooltip("The camera attached to the player (should be a child object)")]
    public Camera playerCamera;
    
    [Tooltip("Mouse sensitivity for looking around")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Separate vertical sensitivity multiplier")]
    [Range(0.5f, 1.5f)]
    public float verticalSensitivityMultiplier = 1f;
    
    [Tooltip("Maximum angle the player can look up (degrees)")]
    [Range(60f, 89.9f)]
    public float maxLookUpAngle = 89f;
    
    [Tooltip("Maximum angle the player can look down (degrees)")]
    [Range(60f, 89.9f)]
    public float maxLookDownAngle = 89f;
    
    [Tooltip("Smoothing applied to camera rotation (0 = instant, higher = smoother)")]
    [Range(0f, 20f)]
    public float cameraSmoothTime = 10f;
    
    [Tooltip("Invert Y-axis look")]
    public bool invertY = false;
    
    [Tooltip("Invert X-axis look")]
    public bool invertX = false;

    [Header("=== MOVEMENT SETTINGS ===")]
    [Tooltip("Normal walking speed")]
    public float walkSpeed = 4f;
    
    [Tooltip("Sprinting speed")]
    public float sprintSpeed = 7f;
    
    [Tooltip("Crouching speed")]
    public float crouchSpeed = 2f;
    
    [Tooltip("How quickly the player accelerates")]
    [Range(1f, 30f)]
    public float acceleration = 12f;
    
    [Tooltip("How quickly the player decelerates when grounded")]
    [Range(1f, 30f)]
    public float groundDeceleration = 10f;
    
    [Tooltip("How much control the player has in the air (0-1)")]
    [Range(0f, 1f)]
    public float airControl = 0.3f;
    
    [Tooltip("Deceleration while in the air")]
    [Range(0f, 10f)]
    public float airDeceleration = 2f;

    [Header("=== JUMPING & GRAVITY ===")]
    [Tooltip("Jump height in units")]
    public float jumpHeight = 1.2f;
    
    [Tooltip("Gravity multiplier")]
    public float gravityMultiplier = 2.5f;
    
    [Tooltip("Extra gravity when falling (makes jumps feel snappier)")]
    public float fallMultiplier = 1.5f;
    
    [Tooltip("Time after leaving ground where jump is still allowed")]
    [Range(0f, 0.3f)]
    public float coyoteTime = 0.15f;
    
    [Tooltip("Buffer window for jump input before landing")]
    [Range(0f, 0.3f)]
    public float jumpBufferTime = 0.1f;

    [Header("=== CROUCHING ===")]
    [Tooltip("Enable crouching")]
    public bool canCrouch = true;
    
    [Tooltip("Height when crouched")]
    public float crouchHeight = 1f;
    
    [Tooltip("Normal standing height")]
    public float standingHeight = 2f;
    
    [Tooltip("How fast crouch transition happens")]
    [Range(1f, 20f)]
    public float crouchTransitionSpeed = 10f;

    [Header("=== HEAD BOB ===")]
    [Tooltip("Enable head bobbing effect")]
    public bool enableHeadBob = true;
    
    [Tooltip("Head bob frequency while walking")]
    public float walkBobFrequency = 8f;
    
    [Tooltip("Head bob frequency while sprinting")]
    public float sprintBobFrequency = 12f;
    
    [Tooltip("Vertical bob amount")]
    [Range(0f, 0.1f)]
    public float bobVerticalAmount = 0.03f;
    
    [Tooltip("Horizontal bob amount")]
    [Range(0f, 0.1f)]
    public float bobHorizontalAmount = 0.02f;

    [Header("=== CAMERA EFFECTS ===")]
    [Tooltip("Enable landing impact effect")]
    public bool enableLandingImpact = true;
    
    [Tooltip("Maximum landing impact (camera dip)")]
    [Range(0f, 1f)]
    public float maxLandingImpact = 0.3f;
    
    [Tooltip("Speed of landing impact recovery")]
    [Range(1f, 20f)]
    public float landingRecoverySpeed = 8f;
    
    [Tooltip("Enable FOV change when sprinting")]
    public bool enableSprintFOV = true;
    
    [Tooltip("Normal field of view")]
    public float normalFOV = 60f;
    
    [Tooltip("Field of view while sprinting")]
    public float sprintFOV = 70f;
    
    [Tooltip("FOV transition speed")]
    [Range(1f, 20f)]
    public float fovTransitionSpeed = 8f;

    [Header("=== FOOTSTEPS (Optional) ===")]
    [Tooltip("Audio source for footsteps")]
    public AudioSource footstepAudioSource;
    
    [Tooltip("Footstep sounds (randomly selected)")]
    public AudioClip[] footstepSounds;
    
    [Tooltip("Time between footsteps while walking")]
    public float walkStepInterval = 0.5f;
    
    [Tooltip("Time between footsteps while sprinting")]
    public float sprintStepInterval = 0.35f;
    
    [Tooltip("Footstep volume")]
    [Range(0f, 1f)]
    public float footstepVolume = 0.5f;

    [Header("=== INPUT SETTINGS ===")]
    [Tooltip("Sprint key")]
    public KeyCode sprintKey = KeyCode.LeftShift;
    
    [Tooltip("Crouch key")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    
    [Tooltip("Toggle crouch instead of hold")]
    public bool toggleCrouch = false;

    // Private variables
    private CharacterController characterController;
    private Vector3 velocity;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    
    // Camera rotation
    private float targetPitch;
    private float targetYaw;
    private float currentPitch;
    private float currentYaw;
    
    // Ground detection
    private bool isGrounded;
    private bool wasGrounded;
    private float timeSinceGrounded;
    private float timeSinceJumpPressed;
    private bool jumpConsumed;
    
    // States
    private bool isSprinting;
    private bool isCrouching;
    private bool wantsToCrouch;
    private float currentHeight;
    
    // Head bob
    private float bobTimer;
    private Vector3 originalCameraLocalPos;
    private float landingImpactOffset;
    
    // Footsteps
    private float footstepTimer;
    
    // Cached
    private Transform cameraTransform;
    private float baseStepOffset;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
        
        if (playerCamera != null)
        {
            cameraTransform = playerCamera.transform;
            originalCameraLocalPos = cameraTransform.localPosition;
        }
        
        baseStepOffset = characterController.stepOffset;
        currentHeight = standingHeight;
    }

    private void Start()
    {
        // Lock and hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize rotation to current transform rotation
        Vector3 eulerAngles = transform.eulerAngles;
        targetYaw = eulerAngles.y;
        currentYaw = targetYaw;
        
        if (cameraTransform != null)
        {
            targetPitch = cameraTransform.localEulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;
            currentPitch = targetPitch;
        }
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleCameraLook();
        HandleMovementInput();
        HandleJumping();
        HandleCrouching();
        ApplyGravity();
        ApplyMovement();
        HandleHeadBob();
        HandleCameraEffects();
        HandleFootsteps();
        
        wasGrounded = isGrounded;
    }

    private void HandleGroundCheck()
    {
        isGrounded = characterController.isGrounded;
        
        if (isGrounded)
        {
            timeSinceGrounded = 0f;
            jumpConsumed = false;
            
            // Slight downward velocity to keep grounded
            if (verticalVelocity < 0)
            {
                verticalVelocity = -2f;
            }
        }
        else
        {
            timeSinceGrounded += Time.deltaTime;
        }
    }

    private void HandleCameraLook()
    {
        // Get raw mouse input
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity * verticalSensitivityMultiplier;
        
        // Apply inversion
        if (invertX) mouseX = -mouseX;
        if (invertY) mouseY = -mouseY;
        
        // Update target rotation
        targetYaw += mouseX;
        targetPitch -= mouseY;
        
        // Clamp pitch to prevent gimbal lock and over-rotation
        targetPitch = Mathf.Clamp(targetPitch, -maxLookUpAngle, maxLookDownAngle);
        
        // Smooth the rotation
        if (cameraSmoothTime > 0)
        {
            float smoothFactor = cameraSmoothTime * Time.deltaTime;
            currentYaw = Mathf.Lerp(currentYaw, targetYaw, smoothFactor);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, smoothFactor);
        }
        else
        {
            currentYaw = targetYaw;
            currentPitch = targetPitch;
        }
        
        // Apply rotation - body rotates on Y axis, camera rotates on X axis
        transform.rotation = Quaternion.Euler(0f, currentYaw, 0f);
        
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
        }
    }

    private void HandleMovementInput()
    {
        // Get input
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");
        
        // Determine if sprinting
        bool wantsToSprint = Input.GetKey(sprintKey) && inputZ > 0 && !isCrouching;
        isSprinting = wantsToSprint && isGrounded;
        
        // Calculate move direction relative to player facing
        Vector3 inputDirection = new Vector3(inputX, 0f, inputZ).normalized;
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        
        // Determine target speed
        float targetSpeed = walkSpeed;
        if (isCrouching)
        {
            targetSpeed = crouchSpeed;
        }
        else if (isSprinting)
        {
            targetSpeed = sprintSpeed;
        }
        
        Vector3 targetVelocity = worldDirection * targetSpeed;
        
        // Calculate acceleration based on grounded state
        float currentAcceleration;
        float currentDeceleration;
        
        if (isGrounded)
        {
            currentAcceleration = acceleration;
            currentDeceleration = groundDeceleration;
        }
        else
        {
            currentAcceleration = acceleration * airControl;
            currentDeceleration = airDeceleration;
        }
        
        // Apply acceleration/deceleration
        if (inputDirection.magnitude > 0.1f)
        {
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity, 
                targetVelocity, 
                currentAcceleration * Time.deltaTime
            );
        }
        else
        {
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity, 
                Vector3.zero, 
                currentDeceleration * Time.deltaTime
            );
        }
    }

    private void HandleJumping()
    {
        // Track jump buffer
        if (Input.GetButtonDown("Jump"))
        {
            timeSinceJumpPressed = 0f;
        }
        else
        {
            timeSinceJumpPressed += Time.deltaTime;
        }
        
        // Check if we can jump (coyote time + jump buffer)
        bool canJump = (isGrounded || timeSinceGrounded <= coyoteTime) && !jumpConsumed;
        bool wantsToJump = timeSinceJumpPressed <= jumpBufferTime;
        
        // Check for ceiling when crouched
        if (isCrouching && wantsToJump && canJump)
        {
            if (CheckCeilingAbove())
            {
                return; // Can't jump while crouched under something
            }
        }
        
        if (wantsToJump && canJump)
        {
            // Calculate jump velocity from desired height
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics.gravity.y) * gravityMultiplier);
            jumpConsumed = true;
            timeSinceJumpPressed = jumpBufferTime + 1f; // Consume the buffer
            
            // Uncrouch when jumping
            if (isCrouching)
            {
                wantsToCrouch = false;
            }
        }
    }

    private void HandleCrouching()
    {
        if (!canCrouch) return;
        
        // Handle input
        if (toggleCrouch)
        {
            if (Input.GetKeyDown(crouchKey))
            {
                wantsToCrouch = !wantsToCrouch;
            }
        }
        else
        {
            wantsToCrouch = Input.GetKey(crouchKey);
        }
        
        // Determine target height
        float targetHeight;
        
        if (wantsToCrouch)
        {
            targetHeight = crouchHeight;
            isCrouching = true;
        }
        else
        {
            // Check if we can stand up
            if (isCrouching && CheckCeilingAbove())
            {
                targetHeight = crouchHeight;
                // Stay crouched
            }
            else
            {
                targetHeight = standingHeight;
                isCrouching = false;
            }
        }
        
        // Smoothly transition height
        if (!Mathf.Approximately(currentHeight, targetHeight))
        {
            float previousHeight = currentHeight;
            currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            
            characterController.height = currentHeight;
            
            // Adjust center
            characterController.center = new Vector3(0f, currentHeight / 2f, 0f);
            
            // Adjust camera position
            if (cameraTransform != null)
            {
                Vector3 camPos = originalCameraLocalPos;
                camPos.y = currentHeight - 0.2f; // Camera slightly below top
                originalCameraLocalPos = camPos;
            }
            
            // Adjust position to prevent going through floor when standing
            if (targetHeight > previousHeight && isGrounded)
            {
                float heightDiff = currentHeight - previousHeight;
                characterController.Move(new Vector3(0f, heightDiff / 2f, 0f));
            }
        }
        
        // Adjust step offset when crouched
        characterController.stepOffset = isCrouching ? baseStepOffset * 0.5f : baseStepOffset;
    }

    private bool CheckCeilingAbove()
    {
        float checkDistance = standingHeight - currentHeight + 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * currentHeight;
        
        return Physics.Raycast(rayOrigin, Vector3.up, checkDistance);
    }

    private void ApplyGravity()
    {
        float gravity = Physics.gravity.y * gravityMultiplier;
        
        // Apply extra gravity when falling for snappier feel
        if (verticalVelocity < 0)
        {
            gravity *= fallMultiplier;
        }
        // Reduce gravity at peak of jump if holding jump (variable jump height)
        else if (verticalVelocity > 0 && !Input.GetButton("Jump"))
        {
            gravity *= fallMultiplier;
        }
        
        verticalVelocity += gravity * Time.deltaTime;
        
        // Terminal velocity
        verticalVelocity = Mathf.Max(verticalVelocity, -50f);
    }

    private void ApplyMovement()
    {
        velocity = horizontalVelocity + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;
        
        // Only bob when grounded and moving
        float speed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;
        
        if (isGrounded && speed > 0.5f)
        {
            float bobFrequency = isSprinting ? sprintBobFrequency : walkBobFrequency;
            float speedMultiplier = speed / walkSpeed;
            
            bobTimer += Time.deltaTime * bobFrequency * speedMultiplier;
            
            float verticalBob = Mathf.Sin(bobTimer) * bobVerticalAmount * speedMultiplier;
            float horizontalBob = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmount * speedMultiplier;
            
            Vector3 bobOffset = new Vector3(horizontalBob, verticalBob, 0f);
            cameraTransform.localPosition = originalCameraLocalPos + bobOffset + Vector3.up * landingImpactOffset;
        }
        else
        {
            // Smoothly return to original position
            bobTimer = 0f;
            cameraTransform.localPosition = Vector3.Lerp(
                cameraTransform.localPosition, 
                originalCameraLocalPos + Vector3.up * landingImpactOffset,
                10f * Time.deltaTime
            );
        }
    }

    private void HandleCameraEffects()
    {
        if (cameraTransform == null) return;
        
        // Landing impact
        if (enableLandingImpact)
        {
            // Detect landing
            if (isGrounded && !wasGrounded && verticalVelocity < -5f)
            {
                float impactStrength = Mathf.Clamp01(Mathf.Abs(verticalVelocity) / 20f);
                landingImpactOffset = -maxLandingImpact * impactStrength;
            }
            
            // Recover from impact
            landingImpactOffset = Mathf.Lerp(landingImpactOffset, 0f, landingRecoverySpeed * Time.deltaTime);
        }
        
        // Sprint FOV
        if (enableSprintFOV)
        {
            float targetFOV = isSprinting ? sprintFOV : normalFOV;
            playerCamera.fieldOfView = Mathf.Lerp(
                playerCamera.fieldOfView, 
                targetFOV, 
                fovTransitionSpeed * Time.deltaTime
            );
        }
    }

    private void HandleFootsteps()
    {
        if (footstepAudioSource == null || footstepSounds == null || footstepSounds.Length == 0)
            return;
        
        float speed = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z).magnitude;
        
        if (isGrounded && speed > 0.5f)
        {
            float stepInterval = isSprinting ? sprintStepInterval : walkStepInterval;
            footstepTimer += Time.deltaTime;
            
            if (footstepTimer >= stepInterval)
            {
                footstepTimer = 0f;
                PlayFootstep();
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    private void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;
        
        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
        footstepAudioSource.pitch = Random.Range(0.9f, 1.1f);
        footstepAudioSource.PlayOneShot(clip, footstepVolume);
    }

    // Public methods for external control
    
    /// <summary>
    /// Teleport the player to a position
    /// </summary>
    public void Teleport(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
    }
    
    /// <summary>
    /// Set the player's look direction
    /// </summary>
    public void SetLookDirection(float yaw, float pitch)
    {
        targetYaw = yaw;
        targetPitch = Mathf.Clamp(pitch, -maxLookUpAngle, maxLookDownAngle);
        currentYaw = targetYaw;
        currentPitch = targetPitch;
    }
    
    /// <summary>
    /// Add external force to the player (e.g., explosion knockback)
    /// </summary>
    public void AddForce(Vector3 force)
    {
        horizontalVelocity += new Vector3(force.x, 0f, force.z);
        verticalVelocity += force.y;
    }
    
    /// <summary>
    /// Lock or unlock cursor
    /// </summary>
    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
    
    /// <summary>
    /// Check if player is currently grounded
    /// </summary>
    public bool IsGrounded => isGrounded;
    
    /// <summary>
    /// Check if player is currently sprinting
    /// </summary>
    public bool IsSprinting => isSprinting;
    
    /// <summary>
    /// Check if player is currently crouching
    /// </summary>
    public bool IsCrouching => isCrouching;
    
    /// <summary>
    /// Get current movement speed
    /// </summary>
    public float CurrentSpeed => horizontalVelocity.magnitude;
}
