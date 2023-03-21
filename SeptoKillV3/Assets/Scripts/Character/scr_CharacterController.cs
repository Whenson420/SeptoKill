using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Models;
using EZCameraShake;

public class scr_CharacterController : MonoBehaviour
{
    #region Variables
    public PlayerSettingsModel playerSettings;
    [Header("Health")]
    [SerializeField] private float currentHealth = 100.0f;
    [SerializeField] private float maxHealth = 100.0f;
    [SerializeField] private int regenRate = 1;
    [SerializeField] private bool canRegen = false;
    [SerializeField] private float healCooldown = 3.0f;
    [SerializeField] private float maxHealCooldown = 3.0f;
    [SerializeField] private bool startCooldown = false;
    [SerializeField] public Image Splatter = null;
    [SerializeField] public Image hurtImage = null;
    [SerializeField] private float hurtTimer = 0.1f;
    [SerializeField] private AudioClip hurtAudio = null;
    private AudioSource healthAudioSource;
    [Header("Stamina")]
    public float Stamina = 100.0f;
    [SerializeField] private float maxStamina = 100.0f;
    [SerializeField] private float jumpCost = 20;
    public bool hasRegenerated = true;
    [Range(0,60)][SerializeField] private float staminaDrain = 0.5f;
    [Range(0,60)][SerializeField] private float staminaRegen = 0.5f;
    [SerializeField] private float slowedRunSpeed = 5;
    private float normalRunSpeed = 0;
    [SerializeField] private Image staminaBar = null;
    [SerializeField] private CanvasGroup slider = null;


    private CharacterController characterController;
    private DefaultInput defaultInput;
    [HideInInspector]
    public Vector2 input_Movement;
    [HideInInspector]
    public Vector2 input_View;
    private Vector3 newCameraRotation;
    private Vector3 newCharacterRotation;


    [Header("References")]
    public Transform cameraHolder;
    public Transform camera;
    public Transform feetTransform;
    public Image Crosshair = null;

    [Header("Settings")]
    public float viewClampYmin = -70;
    public float viewClampYmax = 80;
    public LayerMask playerMask;
    public LayerMask groundMask;

    [Header("Gravity")]
    public float gravityAmount;
    public float gravityMin;
    private float playerGravity;

    public Vector3 jumpingForce;
    private Vector3 jumpingForceVelocity;

    [Header("Stance")]
    public PlayerStance playerStance;
    public float playerStanceSmoothing;
    public CharacterStance playerStandStance;
    public CharacterStance playerCrouchStance;
    public CharacterStance playerProneStance;
    private float stanceCheckErrorMargin = 0.05f;

    private float cameraHeight;
    private float cameraHeightVelocity;

    private Vector3 stanceCapsuleCenterVelocity;
    private float stanceCapsuleHeightVelocity;

    [HideInInspector]
    public bool isSprinting;

    private Vector3 newMovementSpeed;
    private Vector3 newMovementVelocity;
    [Header("Weapon")]
    public WeaponController currentWeapon;

    public float weaponAnimationSpeed;

    [HideInInspector]
    public bool isGrounded;
    [HideInInspector]
    public bool isFalling;

    [Header("Leaning")]
    public Transform LeanPivot;
    private float currentLean;
    private float targetLean;
    public float leanAngle;
    public float leanSmoothing;
    private float leanVelocity;

    private bool isLeaningLeft;
    private bool isLeaningRight;

    [Header("Aiming In")]
    public bool isAimingIn;
#endregion
    private void Awake()
    {
        normalRunSpeed = playerSettings.RunningForwardSpeed;
        healthAudioSource = GetComponent<AudioSource>();
        currentWeapon.bulletsLeft = currentWeapon.magazineSize;
        currentWeapon.readyToShoot = true;
        Cursor.lockState = CursorLockMode.Locked;

        defaultInput = new DefaultInput();

        defaultInput.Character.Movement.performed += e => input_Movement = e.ReadValue<Vector2>();
        defaultInput.Character.View.performed += e => input_View = e.ReadValue<Vector2>();
        defaultInput.Character.Jump.performed += e => Jump();

        defaultInput.Character.Crouch.performed += e => Crouch();

        defaultInput.Character.Prone.performed += e => Prone();

        defaultInput.Weapon.Reload.performed += e => currentWeapon.Reload();

        defaultInput.Character.Sprint.performed += e => ToggleSprint();
        defaultInput.Character.SprintReleased.performed += e => StopSprint();

        defaultInput.Weapon.Fire2Pressed.performed += e => AimingInPressed();
        defaultInput.Weapon.Fire2Released.performed += e => AimingInReleased();
        defaultInput.Weapon.Fire1Pressed.performed += e => currentWeapon.shooting = true;
        defaultInput.Weapon.Fire1Released.performed += e => currentWeapon.shooting = false;

        defaultInput.Character.LeanLeftPressed.performed += e => isLeaningLeft = true;
        defaultInput.Character.LeanLeftReleased.performed += e => isLeaningLeft = false;

        defaultInput.Character.LeanRightPressed.performed += e => isLeaningRight = true;
        defaultInput.Character.LeanRightReleased.performed += e => isLeaningRight = false;


        defaultInput.Enable();

        newCameraRotation = cameraHolder.localRotation.eulerAngles;
        newCharacterRotation = transform.localRotation.eulerAngles;

        characterController = GetComponent<CharacterController>();

        cameraHeight = cameraHolder.localPosition.y;
        if (currentWeapon)
        {
            currentWeapon.Initialize(this);
        }
    }

    private void Update()
    {
        if (Stamina <= 0)
        {
            hasRegenerated = false;
            playerSettings.RunningForwardSpeed = slowedRunSpeed;
            slider.alpha = 0;
        }
        SetIsGrounded();
        SetIsFalling();

        CalculateMovement();
        CalculateView();
        CalculateJump();
        CalculateStance();
        CalculateLeaning();
        CalculateAimingIn();
        if (currentHealth <= 0)
        {
            Die();
        }
        if (startCooldown)
        {
            healCooldown -= Time.deltaTime;
            if (healCooldown <= 0)
            {
                canRegen = true;
                startCooldown = false;
            }
        }
        if (canRegen)
        {
            if (currentHealth <= maxHealth - 0.01)
            {
                currentHealth += Time.deltaTime * regenRate;
                UpdateHealth();
            }
            else
            {
                currentHealth = maxHealth;
                healCooldown = maxHealCooldown;
                canRegen = false;

            }
        }
        if (isAimingIn)
        {
            weaponAnimationSpeed = 0;
        }
    }
    #region Movement

    private void AimingInPressed()
    {
        isAimingIn = true;
        playerSettings.AimingSpeedEffector = 0.4f;
    }
    private void AimingInReleased()
    {
        isAimingIn = false;
        playerSettings.AimingSpeedEffector = 0.6f;
    }
    private void CalculateAimingIn()
    {
        if (!currentWeapon)
        {
            return;
        }

        currentWeapon.isAimingIn = isAimingIn;
        Crosshair.enabled = !isAimingIn;
        UpdateStamina(isAimingIn ? 0 : 1);
    }

    private void SetIsGrounded()
    {
        isGrounded = Physics.CheckSphere(feetTransform.position, playerSettings.isGroundedRadius, groundMask);
    }

    private void SetIsFalling()
    {
        isFalling = (!isGrounded && characterController.velocity.magnitude > playerSettings.isFallingSpeed);
    }

    private void CalculateView()
    {
        newCharacterRotation.y += (isAimingIn ? playerSettings.ViewXSensitivity * playerSettings.AimingSensitivityEffector : playerSettings.ViewXSensitivity) * (playerSettings.ViewXSensitivity * (playerSettings.ViewXInverted ? -input_View.x : input_View.x) * Time.deltaTime);
        transform.rotation = Quaternion.Euler(newCharacterRotation);

        newCameraRotation.x += (isAimingIn ? playerSettings.ViewYSensitivity * playerSettings.AimingSensitivityEffector : playerSettings.ViewYSensitivity) * (playerSettings.ViewYSensitivity * (playerSettings.ViewYInverted ? input_View.y : -input_View.y) * Time.deltaTime);
        newCameraRotation.x = Mathf.Clamp(newCameraRotation.x, viewClampYmin, viewClampYmax);

        cameraHolder.localRotation = Quaternion.Euler(newCameraRotation);
    }

    private void CalculateMovement()
    {
        if (input_Movement.y <= 0.2f)
        {
            isSprinting = false;
        }

        var verticalSpeed = playerSettings.WalkingForwardSpeed;
        var horizontalSpeed = playerSettings.WalkingStrafeSpeed;
        if (!isSprinting)
        {
            if (Stamina <= maxStamina - 0.01)
            {
                Stamina += staminaRegen * Time.deltaTime;
                UpdateStamina(1);
                if (Stamina >= maxStamina)
                {
                    playerSettings.RunningForwardSpeed = normalRunSpeed;
                    slider.alpha = 0;
                    hasRegenerated = true;
                }
            }
        }
        if (isSprinting)
        {
            verticalSpeed = playerSettings.RunningForwardSpeed;
            horizontalSpeed = playerSettings.RunningForwardSpeed - 2;
            Stamina -= staminaDrain * Time.deltaTime;
            UpdateStamina(1);
        }
        if (!isGrounded)
        {
            playerSettings.SpeedEffector = playerSettings.FallingSpeedEffector;
        }
        else if (playerStance == PlayerStance.Crouch)
        {
            playerSettings.SpeedEffector = playerSettings.CrouchSpeedEffector;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            playerSettings.SpeedEffector = playerSettings.ProneSpeedEffector;
        }
        else if (isAimingIn)
        {
            playerSettings.SpeedEffector = playerSettings.AimingSpeedEffector;
        }
        else
        {
            playerSettings.SpeedEffector = 1;
        }
        weaponAnimationSpeed = characterController.velocity.magnitude / (playerSettings.WalkingForwardSpeed * playerSettings.SpeedEffector);
        if (weaponAnimationSpeed > 1)
        {
            weaponAnimationSpeed = 1;
        }

        verticalSpeed *= playerSettings.SpeedEffector;
        horizontalSpeed *= playerSettings.SpeedEffector;

        newMovementSpeed = Vector3.SmoothDamp(newMovementSpeed, new Vector3(horizontalSpeed * input_Movement.x * Time.deltaTime, 0, verticalSpeed * input_Movement.y * Time.deltaTime), ref newMovementVelocity, isGrounded ? playerSettings.MovementSmoothing : playerSettings.FallingSmoothing);
        var movementSpeed = transform.TransformDirection(newMovementSpeed);

        if (playerGravity > gravityMin)
        {
            playerGravity -= gravityAmount * Time.deltaTime;
        }

        playerGravity -= gravityAmount * Time.deltaTime;

        if (playerGravity < -0.1f && !isGrounded)
        {
            playerGravity = -0.1f;
        }



        movementSpeed.y += playerGravity;

        movementSpeed += jumpingForce * Time.deltaTime;

        characterController.Move(movementSpeed);

    }

    private void CalculateLeaning()
    {
        if (isLeaningLeft)
        {
            targetLean = leanAngle;
            isSprinting = false;
        }
        else if (isLeaningRight)
        {
            targetLean = -leanAngle;
            isSprinting = false;
        }
        else
        {
            targetLean = 0;
        }
        currentLean = Mathf.SmoothDamp(currentLean, targetLean, ref leanVelocity, leanSmoothing);

        LeanPivot.localRotation = Quaternion.Euler(new Vector3(0, 0, currentLean));
    }

    private void CalculateJump()
    {
        jumpingForce = Vector3.SmoothDamp(jumpingForce, Vector3.zero, ref jumpingForceVelocity, playerSettings.JumpingFalloff);
    }

    private void CalculateStance()
    {
        var currentStance = playerStandStance;

        if (playerStance == PlayerStance.Crouch)
        {
            currentStance = playerCrouchStance;
        }
        else if (playerStance == PlayerStance.Prone)
        {
            currentStance = playerProneStance;
        }
        cameraHeight = Mathf.SmoothDamp(cameraHolder.localPosition.y, currentStance.CameraHeight, ref cameraHeightVelocity, playerStanceSmoothing);
        cameraHolder.localPosition = new Vector3(cameraHolder.localPosition.x, cameraHeight, cameraHolder.localPosition.z);

        characterController.height = Mathf.SmoothDamp(characterController.height, currentStance.StanceCollider.height, ref stanceCapsuleHeightVelocity, playerStanceSmoothing);
        characterController.center = Vector3.SmoothDamp(characterController.center, currentStance.StanceCollider.center, ref stanceCapsuleCenterVelocity, playerStanceSmoothing);


    }

    private void Jump()
    {
        if (!isGrounded || playerStance == PlayerStance.Prone)
        {
            return;
        }
        if (playerStance == PlayerStance.Crouch)
        {
            if (StandCheck(playerStandStance.StanceCollider.height))
            {
                return;
            }
            playerStance = PlayerStance.Stand;
            return;
        }

        if (Stamina >= (maxStamina * jumpCost / maxStamina))
        {
            Stamina -= jumpCost;
            jumpingForce = Vector3.up * playerSettings.JumpingHeight;
            playerGravity = 0;
            currentWeapon.TriggerJump();
            UpdateStamina(1);
        }

    }

    private void Crouch()
    {
        if (playerStance == PlayerStance.Crouch)
        {
            if (StandCheck(playerStandStance.StanceCollider.height))
            {
                return;
            }
            playerStance = PlayerStance.Stand;
            return;
        }
        if (StandCheck(playerCrouchStance.StanceCollider.height))
        {
            return;
        }

        playerStance = PlayerStance.Crouch;
    }

    private void Prone()
    {
        playerStance = PlayerStance.Prone;
    }

    private bool StandCheck(float stanceCheckheight)
    {
        var start = new Vector3(feetTransform.position.x, feetTransform.position.y + characterController.radius + stanceCheckErrorMargin + stanceCheckheight, feetTransform.position.z);
        var end = new Vector3(feetTransform.position.x, feetTransform.position.y - characterController.radius - stanceCheckErrorMargin + stanceCheckheight, feetTransform.position.z);

        return Physics.CheckCapsule(start, end, characterController.radius, playerMask);
    }

    private void ToggleSprint()
    {
        if (!isLeaningLeft || !isLeaningRight)
        {
            if (input_Movement.y <= 0.2f)
            {
                isSprinting = false;
                return;
            }
            if (hasRegenerated)
            {
                isSprinting = true;                
            }
        }
    }
    private void StopSprint()
    {
        if (playerSettings.SprintingHold)
        {
            isSprinting = false;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(feetTransform.position, playerSettings.isGroundedRadius);
    }
    void UpdateStamina(int value)
    {
        staminaBar.fillAmount = Stamina / maxStamina;

        if (value == 0)
        {
            slider.alpha = 0;
        }
        else
        {
            slider.alpha = 1;
        }
    }
    #endregion
    #region Health
    void UpdateHealth()
    {
        Color splatterAlpha = Splatter.color;
        splatterAlpha.a = 1 - (currentHealth / maxHealth);
    }

    IEnumerator HurtFlash()
    {
        hurtImage.enabled = true;
        healthAudioSource.PlayOneShot(hurtAudio);
        yield return new WaitForSeconds(hurtTimer);
        hurtImage.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth >= 0)
        {
            canRegen = false;
            StartCoroutine(HurtFlash());
            UpdateHealth();
            healCooldown = maxHealCooldown;
            //startCooldown = true;
        }
        currentHealth -= damage;
        CameraShaker.Instance.ShakeOnce(2, 1f, 1,  1);
    }
    private void Die()
    {

    }
    #endregion

}
