using UnityEngine;

public class BlacksmithSound : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip hammerSound;

    // Hàm này sẽ được Animation Event gọi trực tiếp
    public void PlayHammerStrike()
    {
        if (audioSource != null && hammerSound != null)
        {
            // Dùng PlayOneShot để âm thanh có thể đè lên nhau nếu gõ nhanh, không bị ngắt quãng
            audioSource.PlayOneShot(hammerSound);
        }
    }
}