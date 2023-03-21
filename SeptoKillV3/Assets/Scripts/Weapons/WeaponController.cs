using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static Models;
using EZCameraShake;

public class WeaponController : MonoBehaviour
{
    private scr_CharacterController characterController;

    [Header("References")]
    public Animator weaponAnimator;

    [Header("Settings")]
    public WeaponSettingsModel settings;
    public bool isEnabled = false;

    bool isInitialized;

    Vector3 newWeaponRotation;
    Vector3 newWeaponRotationVelocity;
    Vector3 targetWeaponRotation;
    Vector3 targetWeaponRotationVelocity;

    Vector3 newWeaponMovementRotation;
    Vector3 newWeaponMovementRotationVelocity;

    Vector3 targetWeaponMovementRotation;
    Vector3 targetWeaponMovementRotationVelocity;

    private bool isGroundedTrigger;

    private float fallingDelay;

    [Header("Weapon Sway")]
    public Transform weaponSwayObject;

    public float swayAmountA = 1;
    public float swayAmountB = 2;
    public float swayScale = 600;
    public float swayLerpSpeed = 14;

    float swayTime;
    Vector3 swayPosition;

    [HideInInspector]
    public bool isAimingIn;

    [Header("Sights")]
    public Transform sightTarget;
    public float sightOffset;
    public float aimingInTime;
    private Vector3 weaponSwayPosition;
    private Vector3 weaponSwayPositionVelocity;

    [Header("Shooting")]

    public int damage;
    public float timeBetweenShooting, spread, range, reloadTime, timeBetweenShots;
    public float magazineSize, Magazines, bulletsPerTap, AmmoSum;
    public float bulletsLeft, bulletsShot;
    public bool shooting, readyToShoot, reloading, alreadyPlayed = false;
    public Transform attackPoint;
    public RaycastHit rayHit;
    public LayerMask Enemy;
    public GameObject muzzleFlash, bulletHoleGraphic;
    public float camShakeMagnitude, camShakeDuration;
    public TextMeshProUGUI text;
    public TextMeshProUGUI Magazinestext;
    public AudioSource ShootingSFX,MagazineSFX,EmptySFX;
    public Image MagazineIcon;

    private void Start()
    {
        MagazineIcon.fillAmount = bulletsLeft / magazineSize;
        newWeaponRotation = transform.localRotation.eulerAngles;
        AmmoSum = (Magazines + 1) * magazineSize;
    }

    public void Initialize(scr_CharacterController CharacterController)
    {
        characterController = CharacterController;
        isInitialized = true;
    }

    private void Update()
    {
        Magazinestext.SetText(Magazines.ToString());
        text.SetText(bulletsLeft + " | " + magazineSize);
        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            Shoot();
        }
        else if (shooting && bulletsLeft == 0)
        {
            if (!alreadyPlayed)
            {
                EmptySFX.PlayOneShot(EmptySFX.clip);
                alreadyPlayed = true;
            }
            else
            {
                Invoke("Reload", 0.5f);
            }
        }
        /*if (bulletsLeft == 0)
        {
            Reload();
        }*/
        if (!isInitialized)
        {
            return;
        }
        CalculateWeaponRotation();
        SetWeaponAnimation();
        CalculateWeaponSway();
        CalculateAimingIn();

        if (Input.GetButtonDown("Fire2"))
        {
            isEnabled = !isEnabled;
        }
        if (isEnabled == true)
        {
            isEnabled = true;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            isEnabled = false;
        }
    }
    private void CalculateAimingIn()
    {
        var targetPosition = transform.position;
        if (isAimingIn)
        {
            weaponAnimator.SetBool("isSprinting", false);
            targetPosition = characterController.camera.transform.position + (weaponSwayObject.transform.position - sightTarget.position) + (characterController.camera.transform.forward * sightOffset);
        }
        weaponSwayPosition = weaponSwayObject.transform.position;
        weaponSwayPosition = Vector3.SmoothDamp(weaponSwayPosition, targetPosition, ref weaponSwayPositionVelocity, aimingInTime);
        weaponSwayObject.transform.position = weaponSwayPosition + swayPosition;

    }
    public void TriggerJump()
    {
        isGroundedTrigger = false;
        if(!isAimingIn)
            weaponAnimator.SetTrigger("Jump");
    }
    private void CalculateWeaponRotation()
    {

        targetWeaponRotation.y += (isAimingIn ? settings.SwayAmount / 3 : settings.SwayAmount) * (settings.SwayXInverted ? -characterController.input_View.x : characterController.input_View.x) * Time.deltaTime;
        targetWeaponRotation.x += (isAimingIn ? settings.SwayAmount / 3 : settings.SwayAmount) * (settings.SwayYInverted? characterController.input_View.y : -characterController.input_View.y) * Time.deltaTime;

        targetWeaponRotation.x = Mathf.Clamp(targetWeaponRotation.x, -settings.SwayClampX, settings.SwayClampX);
        targetWeaponRotation.y = Mathf.Clamp(targetWeaponRotation.y, -settings.SwayClampY, settings.SwayClampY);
        targetWeaponRotation.z = isAimingIn ? 0 : targetWeaponRotation.y;

        targetWeaponRotation = Vector3.SmoothDamp(targetWeaponRotation, Vector3.zero, ref targetWeaponRotationVelocity, settings.SwayResetSmoothing);
        newWeaponRotation = Vector3.SmoothDamp(newWeaponRotation, targetWeaponRotation, ref newWeaponRotationVelocity, settings.SwaySmoothing);

        targetWeaponRotation.z = (isAimingIn ? settings.MovementSwayX / 3 : settings.MovementSwayX) * (settings.MovementSwayXInverted ? -characterController.input_Movement.x : characterController.input_Movement.x);
        targetWeaponRotation.x = (isAimingIn ? settings.MovementSwayY / 3 : settings.MovementSwayY) * (settings.MovementSwayYInverted ? -characterController.input_Movement.y : characterController.input_Movement.y);
        targetWeaponMovementRotation = Vector3.SmoothDamp(targetWeaponMovementRotation, Vector3.zero, ref targetWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);
        newWeaponMovementRotation = Vector3.SmoothDamp(newWeaponMovementRotation, targetWeaponMovementRotation, ref newWeaponMovementRotationVelocity, settings.MovementSwaySmoothing);

        transform.localRotation = Quaternion.Euler(newWeaponRotation + newWeaponMovementRotation);
 
    }
    private void SetWeaponAnimation()
    {
        if (!characterController.isAimingIn)
        {
            if (isGroundedTrigger)
            {
                fallingDelay = 0;
            }
            else
            {
                fallingDelay += Time.deltaTime;
            }

            if (characterController.isGrounded && !isGroundedTrigger && fallingDelay > 0.1f)
            {
                weaponAnimator.SetTrigger("Land");
                isGroundedTrigger = true;
            }
            if (!characterController.isGrounded && isGroundedTrigger)
            {
                weaponAnimator.SetTrigger("Falling");
                isGroundedTrigger = false;
            }
            weaponAnimator.SetBool("isSprinting", characterController.isSprinting);
            weaponAnimator.SetFloat("WeaponAnimationSpeed", characterController.weaponAnimationSpeed);
        }
        

    }
    private void CalculateWeaponSway()
    {
        if (!characterController.isAimingIn)
        {
            var targetPosition = LissajousCurve(swayTime, swayAmountA, swayAmountB) / (isAimingIn ? swayScale * 3 : swayScale);

            swayPosition = Vector3.Lerp(swayPosition, targetPosition, Time.smoothDeltaTime * swayLerpSpeed);
            swayTime += Time.deltaTime;
            if(swayTime>6.3f)
            {
                swayTime = 0;
            }
        }
    }
    private Vector3 LissajousCurve(float Time, float A, float B)
    {
        return new Vector3(Mathf.Sin(Time), A * Mathf.Sin(B * Time + Mathf.PI));
    }
    public void Shoot()
    {
        GameObject hole = new GameObject();
        GameObject flash = new GameObject();
        readyToShoot = false;
        if (!(bulletsLeft == 0))
        {
            float x =isAimingIn ? Random.Range(-spread/2, spread/2) : Random.Range(-spread, spread);
            float y =isAimingIn ? Random.Range(-spread / 2, spread / 2) : Random.Range(-spread, spread);

            Vector3 direction = characterController.cameraHolder.transform.forward + new Vector3(x, y, 0);

            if (Physics.Raycast(characterController.cameraHolder.position, direction, out rayHit, range, Enemy))
            {
                if (rayHit.collider.CompareTag("Enemy"))
                    rayHit.collider.GetComponent<Target>().TakeDamage(damage);
            }

            CameraShaker.Instance.ShakeOnce(camShakeMagnitude,1f,camShakeDuration/2,camShakeDuration/2);
            hole = Instantiate(bulletHoleGraphic, rayHit.point, Quaternion.Euler(0, 180, 0));
            flash = Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

            bulletsLeft--;
            bulletsShot--;
            ShootingSFX.PlayOneShot(ShootingSFX.clip);

            if (!IsInvoking("ResetShot") && !readyToShoot)
            {
                Invoke("ResetShot", timeBetweenShooting);
            }

            if (bulletsShot > 0 && bulletsLeft > 0)
                Invoke("Shoot", timeBetweenShots);
        }        
        MagazineIcon.fillAmount = bulletsLeft / magazineSize;
        Destroy(flash, 1.5f);
        Destroy(hole, 6.0f);
    }
    private void ResetShot()
    {
        readyToShoot = true;
    }
    public void Reload()
    {
        if (bulletsLeft < magazineSize && !reloading)
        {
            reloading = true;
            weaponAnimator.SetBool("Reloading", true);
            Invoke("ReloadFinished", reloadTime);
        }
    }
    private void ReloadFinished()
    {
        AmmoSum += bulletsLeft;
        if (AmmoSum - magazineSize < 0)
        {
            Magazines--;
            bulletsLeft = AmmoSum;
            Debug.Log("Success");
            AmmoSum = 0;

        }
        else
        {
            bulletsLeft = magazineSize;
            AmmoSum -=magazineSize;
            Magazines--;
        }
        if (AmmoSum > 0 && Magazines == 0)
        {
            Magazines = 1;
        }
        MagazineSFX.PlayOneShot(MagazineSFX.clip);
        reloading = false;
        weaponAnimator.SetBool("Reloading", false);
        MagazineIcon.fillAmount = bulletsLeft / magazineSize;
    }

}
