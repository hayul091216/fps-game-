using UnityEngine;
using Photon.Pun;

public class ShootingController : MonoBehaviour
{
    Animator animator;
    InputManager inputManager;
    PlayerMovement playerMovement;

    [Header("Shooting Var")]
    public Transform firePoint;
    public float fireRate = 0f;
    public float fireRange = 100f;
    public float fireDamage = 15f;
    private float nextFireTime = 0f;

    [Header("Reloading")]
    public int maxAmmo = 30;
    private int currentAmmo;
    public float reloadTime = 1.5f;

    [Header("Shooting Flags")]
    public bool isShooting;
    public bool isWalking;
    public bool isShootingInput;
    public bool isReloading = false;

    [Header("Sound Effects")]
    public AudioSource soundAudoiSource;
    public AudioClip shootingSoundClip;
    public AudioClip reloadingSoundClip;

    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem bloodEffect;


    PhotonView view;





    void Start()
    {
        view = GetComponent<PhotonView>();
        animator= GetComponent<Animator>();
        inputManager = GetComponent<InputManager>();
        playerMovement = GetComponent<PlayerMovement>();
        currentAmmo = maxAmmo;

    }

    void Update()
    {
        if (!view.IsMine)
            return;
        
        if(isReloading || playerMovement.isSprinting)
        {
            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", false);
            animator.SetBool("ShootWalk", false);
            return;
        }
        isWalking = playerMovement.isMoving;
        isShootingInput = inputManager.fireInput;

        if (isShootingInput && isWalking)
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
                animator.SetBool("ShootWalk", true);
            }

            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", true);
            isShooting = true;
        }
        else if(isShootingInput)
        {
            if (Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + 1f / fireRate;
                Shoot();
                
            }
            
            animator.SetBool("Shoot", true);
            animator.SetBool("ShootingMovement", false);
            animator.SetBool("ShootWalk", false);
            isShooting = true;
        }
        else
        {
            animator.SetBool("Shoot", false);
            animator.SetBool("ShootingMovement", false);
            animator.SetBool("ShootWalk", false);
            isShooting = false;
        }

        if(inputManager.reloadInput && currentAmmo < maxAmmo)
        {
            Reload();
        }
    }
        

    private void Shoot()
    {

        if (currentAmmo > 0)
        {
            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, fireRange))
            {
                Debug.Log(hit.transform.name);
                //사격추출, 플레이어 대미지
                Vector3 hitPoint = hit.point;
                Vector3 hitNormal = hit.normal;

                PlayerMovement playermovementDamage = hit.collider.GetComponent<PlayerMovement>();
                if(playermovementDamage != null)
                {
                    playermovementDamage.ApplyDamage(fireDamage);
                    view.RPC("RPC_Shoot",RpcTarget.All,hitPoint,hitNormal);
                }
            }

            muzzleFlash.Play();
            soundAudoiSource.PlayOneShot(shootingSoundClip);
            currentAmmo--;
        }
        else
        {
            Reload();
        }
    }
    [PunRPC]
    void RPC_Shoot(Vector3 hitpoint, Vector3 hitNormal)
    {
        ParticleSystem blood = Instantiate(bloodEffect, hitpoint, Quaternion.LookRotation(hitNormal));
        Destroy(blood.gameObject, blood.main.duration);
    }
    private void Reload()
    {
        if(!isReloading && currentAmmo < maxAmmo)
        {
            if(isShootingInput && isWalking)
            {
                animator.SetTrigger("ShootReload");
            }
            else
            {
                animator.SetTrigger("Reload");
            }
            isReloading= true;
            //장전소리 재생
            soundAudoiSource.PlayOneShot(reloadingSoundClip);
           
            Invoke("FinishReloading", reloadTime);
        }
    }

    private void FinishReloading()
    {
        currentAmmo = maxAmmo;
        isReloading = false;
        inputManager.reloadInput = false;
        if (isShootingInput && isWalking)
        {
            animator.ResetTrigger("ShootReload");
        }
        else
        {
            animator.ResetTrigger("Reload");
        }
    }

}
