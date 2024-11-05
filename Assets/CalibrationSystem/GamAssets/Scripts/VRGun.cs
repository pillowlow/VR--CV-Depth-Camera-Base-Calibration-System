using UnityEngine;
using TMPro;

public class VRGun : MonoBehaviour
{
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public float bulletSpeed = 20f;
    public AudioClip shootSound;
    public ParticleSystem muzzleFlash;
    public int maxBullets = 10;  // Max bullets in the gun
    public int currentBullets;   // Current bullets left in the gun
    public TextMeshProUGUI bulletCountText;  // Text component to display bullet count

    private AudioSource audioSource;
    private bool isGrabbed = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        currentBullets = maxBullets;  // Start with a full magazine
        UpdateBulletCountText();
    }

    private void Update()
    {
        if (isGrabbed && Input.GetButtonDown("Fire1"))
        {
            Shoot();
        }
    }

    public void GrabGun()
    {
        isGrabbed = true;
    }

    public void ReleaseGun()
    {
        isGrabbed = false;
    }

    private void Shoot()
    {
        if (currentBullets > 0)  // Only shoot if there are bullets left
        {
            // Instantiate the bullet
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            bullet.GetComponent<Rigidbody>().velocity = bulletSpawnPoint.forward * bulletSpeed;
            bullet.tag = "Bullet";

            currentBullets--;  // Decrease the bullet count
            UpdateBulletCountText();  // Update the UI with the new bullet count
            OnShoot();         // Trigger shoot effects
        }
        else
        {
            Debug.Log("Out of bullets! Find a refill point.");
        }
    }

    private void OnShoot()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    public void RefillBullets()
    {
        currentBullets = maxBullets;  // Refill bullets to the max capacity
        UpdateBulletCountText();      // Update the UI with the full bullet count
        Debug.Log("Bullets refilled!");
    }

    private void UpdateBulletCountText()
    {
        if (bulletCountText != null)
        {
            bulletCountText.text = $"Bullets: {currentBullets}/{maxBullets}";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RefillPoint"))  // Check if entering a refill zone
        {
            RefillBullets();
        }
    }
}
