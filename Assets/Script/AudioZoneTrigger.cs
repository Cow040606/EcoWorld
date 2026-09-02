using UnityEngine;

public class AudioZoneTrigger : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        // Lấy component Audio Source đang gắn trên chính GameObject này
        audioSource = GetComponent<AudioSource>();
    }

    // Khi có một vật thể bước vào vùng Trigger
    void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem vật thể đó có phải là người chơi không (cần gắn tag "Player" cho nhân vật)
        if (other.CompareTag("Player"))
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    // Khi vật thể bước ra khỏi vùng Trigger
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop(); 
                // Có thể dùng audioSource.Pause() nếu muốn giữ lại thời gian đang phát dở
            }
        }
    }
}