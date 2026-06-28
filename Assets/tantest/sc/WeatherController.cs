using System.Collections;
using UnityEngine;

public class WeatherController : MonoBehaviour
{
    [Header("Cài đặt đối tượng")]
    [Tooltip("Kéo Transform của Player vào đây")]
    public Transform playerTransform;
    [Tooltip("Kéo Component Particle System của Prefab Mưa vào đây")]
    public ParticleSystem rainParticle;

    [Header("Cài đặt vị trí")]
    [Tooltip("Độ cao của đám mây mưa so với người chơi")]
    public float heightOffset = 15f; 

    [Header("Cài đặt thời gian (Tính bằng giây)")]
    public float minTimeBetweenRain = 2400f; // Tối thiểu 40 phút (40 * 60)
    public float maxTimeBetweenRain = 3600f; // Tối đa 60 phút (60 * 60) -> TB là ~50p
    public float rainDuration = 120f;        // Kéo dài 2 phút (2 * 60)

    private void Start()
    {
        // Đảm bảo mưa không rơi khi vừa vào game
        if (rainParticle != null)
        {
            rainParticle.Stop();
        }

        // Bắt đầu vòng lặp thời tiết
        StartCoroutine(WeatherRoutine());
    }

    private void LateUpdate()
    {
        // Mưa luôn di chuyển theo Player (chỉ chạy khi có player)
        if (playerTransform != null)
        {
            // Đặt vị trí mây mưa ngay trên đầu người chơi
            transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y + heightOffset, playerTransform.position.z);
        }
    }

    private IEnumerator WeatherRoutine()
    {
        while (true)
        {
            // 1. Random thời gian chờ đến cơn mưa tiếp theo
            float waitTime = Random.Range(minTimeBetweenRain, maxTimeBetweenRain);
            Debug.Log($"Trời đang nắng. Cơn mưa tiếp theo sẽ đến sau: {waitTime / 60f} phút.");
            
            // Chờ hết thời gian
            yield return new WaitForSeconds(waitTime);

            // 2. Bắt đầu mưa
            Debug.Log("Bắt đầu mưa!");
            if (rainParticle != null) rainParticle.Play();

            // 3. Chờ cho đến khi tạnh mưa (2 phút)
            yield return new WaitForSeconds(rainDuration);

            // 4. Tắt mưa
            Debug.Log("Đã tạnh mưa.");
            if (rainParticle != null) rainParticle.Stop();
        }
    }
}