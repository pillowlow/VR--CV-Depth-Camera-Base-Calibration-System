using UnityEngine;

public class RefillPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Gun"))
        {
            VRGun gun = other.GetComponent<VRGun>();
            if (gun != null)
            {
                gun.RefillBullets();
            }
        }
    }
}
