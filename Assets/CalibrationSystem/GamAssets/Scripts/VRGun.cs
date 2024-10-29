using System.Collections;
using UnityEngine;

public class VRGunOVR : MonoBehaviour
{
    public GameObject bulletPrefab;       // Bullet prefab to spawn
    public Transform bulletSpawnPoint;    // Where bullets are spawned from the gun
    public float bulletSpeed = 20f;       // Speed at which bullets are fired
    public float fireRate = 0.5f;         // Fire rate to control how often bullets are fired
    public OVRInput.Controller controller; // The VR controller to use (e.g., OVRInput.Controller.RTouch)

    private OVRGrabbable grabbable;       // To detect when the gun is grabbed
    private bool canShoot = true;         // To control firing rate

    void Start()
    {
        // Get the OVRGrabbable component attached to this gun
        grabbable = GetComponent<OVRGrabbable>();
    }

    // Update is called once per frame
    void Update()
    {
        // Only allow shooting when the gun is grabbed
        if (grabbable.isGrabbed)
        {
            // Detect when the index trigger is pressed on the specified controller
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller) && canShoot)
            {
                StartCoroutine(ShootBullet());
            }
        }
    }

    // Coroutine to handle firing rate and bullet instantiation
    private IEnumerator ShootBullet()
    {
        canShoot = false;

        // Instantiate the bullet at the spawn point and apply force
        GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        bulletRb.velocity = bulletSpawnPoint.forward * bulletSpeed;

        // Wait for the fire rate cooldown
        yield return new WaitForSeconds(fireRate);

        canShoot = true;
    }
}
