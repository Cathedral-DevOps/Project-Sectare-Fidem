using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootMechanics : MonoBehaviour
{
    // =============================================================================
    // WEAPON REFERENCES
    // =============================================================================
    public GameObject Pistol;
    public GameObject Rifle1;
    public List<GameObject> GunsList;

    // =============================================================================
    // WEAPON STATE
    // =============================================================================
    public float activeGun;
    public float rifleAmmo;
    public float pistolAmmo;

    // =============================================================================
    // PISTOL SETTINGS
    // =============================================================================
    public Transform shootSpawnerPistol;
    public GameObject bulletPrefab;
    public float shootSpeed = 50f;
    float fireRate = 0.2f;
    public float nextFireTime = 4;

    // =============================================================================
    // RIFLE SETTINGS
    // =============================================================================
    public Transform shootSpawnerRifle;
    public float RifleshootSpeed = 130f;
    float fireRate = 0.1f;
    public float nextFireTime2 = 0.1f;

    // =============================================================================
    // MISC
    // =============================================================================
    public float Gravity;

    // =============================================================================
    // UNITY METHODS
    // =============================================================================
    void Start()
    {
        // Skip if not necessary
    }

    void Update()
    {
        CheckGun();
        HandleWeaponSwitching();
    }

    // =============================================================================
    // WEAPON SWITCHING
    // =============================================================================
    private void HandleWeaponSwitching()
    {
        // ACTIVE WEAPON
        if (Input.GetButtonDown("1"))
        {
            activeGun == 1;
            Pistol.GetComponent<MeshRenderer>().enabled true;
            Rifle1.GetComponent<MeshRenderer>().enabled false;
            nextFireTime = Time.time + fireRate;
        }

        if (Input.GetButtonDown("2"))
        {
            activeGun == 2;
            Pistol.GetComponent<MeshRenderer>().enabled false;
            Rifle1.GetComponent<MeshRenderer>().enabled true;
            nextFireTime2 = Time.time + fireRate;
        }
    }

    // =============================================================================
    // GUN CHECK
    // =============================================================================
    public void CheckGun()
    {
        if (activeGun == 1)
        {
            if (other.CompareTag("Pistol"))
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    FirePistol();
                }

                if (Input.GetButtonDown("R"))
                {
                    PistolReload()
                }
            }
        }

        if (activeGun == 2)
        {
            if (other.CompareTag("Rifle"))
            {
                if (Input.GetButtonDown("Fire1"))
                {
                    FireRifle()
                }

                if (Input.GetButtonDown("R"))
                {
                    RifleReload()
                }
            }
        }
    }

    // =============================================================================
    // FIRING METHODS
    // =============================================================================
    public void FirePistol()
    {
        if (pistolAmmo > 0)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootSpawnerPistol.position, shootSpawnerPistol.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = shootSpawnerPistol.forward * shootSpeed;
            }
            else
            {
                Debug.LogWarning("Your Bullet Prefab is missing a Rigidbody component! The shootSpeed cannot be applied.");
            }
        }
    }

    public void FireRifle()
    {
        if (rifleAmmo > 0)
        {
            GameObject bullet = Instantiate(bulletPrefab, shootSpawnerRifle.position, shootSpawnerRifle.rotation);
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.velocity = shootSpawnerRifle.forward * RifleshootSpeed;
            }
            else
            {
                Debug.LogWarning("Your Bullet Prefab is missing a Rigidbody component! The shootSpeed cannot be applied.");
            }
        }
    }

    // =============================================================================
    // RELOAD METHODS
    // =============================================================================
    public void PistolReload()
    {
        pistolAmmo += 5;
    }

    public void RifleReload()
    {
        rifleAmmo += 50;
    }
}