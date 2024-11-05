using UnityEngine;

public class Target : MonoBehaviour
{
    public ParticleSystem hitEffect; // VFX when hit
    public AudioClip hitSound; // Sound effect for hit
    public int scoreValue = 10; // Points given for hitting this target

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void OnHit()
    {
        // Play VFX and sound
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        if (audioSource != null && hitSound != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // Update score
        ScoreManager.Instance.AddScore(scoreValue);

        // Optional: Destroy target or apply other hit logic here
    }
}
