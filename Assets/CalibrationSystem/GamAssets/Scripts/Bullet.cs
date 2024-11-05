using UnityEngine;

public class Bullet : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Target"))
        {
            Target target = collision.gameObject.GetComponent<Target>();
            if (target != null)
            {
                target.OnHit();
            }
        }
        
        Destroy(gameObject); // Destroy the bullet upon impact
    }
}
