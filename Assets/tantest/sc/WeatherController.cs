using Fusion;
using System.Collections;
using UnityEngine;

public class WeatherController : NetworkBehaviour 
{
    [Header("Cài đặt đối tượng")]
    [Tooltip("Không cần kéo thả bằng tay nữa, code sẽ tự tìm!")]
    public Transform playerTransform;
    public ParticleSystem rainParticle;

    [Header("Cài đặt vị trí")]
    public float heightOffset = 15f; 

    [Header("Cài đặt thời gian (Tính bằng giây)")]
    [Tooltip("Tối thiểu 40 phút (40 * 60)")]
    public float minTimeBetweenRain = 2400f; 
    [Tooltip("Tối đa 60 phút (60 * 60) -> TB là 50p")]
    public float maxTimeBetweenRain = 3600f; 
    [Tooltip("Kéo dài 2 phút (2 * 60)")]
    public float rainDuration = 120f;        

    public override void Spawned()
    {
        // Tắt mưa lúc mới vào game
        if (rainParticle != null) rainParticle.Stop();

        // Khởi động luồng thời tiết
        StartCoroutine(WeatherRoutine());
    }

    private void LateUpdate()
    {
        // Nếu đã có Player thì đám mây luôn đi theo trên đầu
        if (playerTransform != null)
        {
            transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y + heightOffset, playerTransform.position.z);
        }
    }

    private IEnumerator WeatherRoutine()
    {
        // 1. TỰ ĐỘNG TÌM PLAYER (Vòng lặp này sẽ chạy cho đến khi tìm thấy)
        Debug.Log("Đang chờ Player spawn...");
        while (playerTransform == null)
        {
            // Tìm object có tên là "Player_Character(Clone)" trên Scene
            GameObject playerObj = GameObject.Find("Player_Character(Clone)");
            
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                Debug.Log("Đã gắp thành công Player: " + playerObj.name);
            }
            
            // Đợi 0.5s rồi tìm lại để không làm lag game
            yield return new WaitForSeconds(0.5f); 
        }

        // 2. TÌM THẤY PLAYER XONG LÀ MƯA LUÔN LẦN ĐẦU
        while (true)
        {
            Debug.Log("Bắt đầu mưa!");
            if (rainParticle != null)
            {
                rainParticle.gameObject.SetActive(true); 
                rainParticle.Clear(); 
                rainParticle.Play(true); 
            }

            // 3. Chờ hết thời gian mưa (120s = 2 phút)
            yield return new WaitForSeconds(rainDuration);

            // 4. Tạnh mưa
            Debug.Log("Đã tạnh mưa.");
            if (rainParticle != null)
            {
                rainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            // 5. Chờ đến cơn mưa ngẫu nhiên tiếp theo
            float waitTime = Random.Range(minTimeBetweenRain, maxTimeBetweenRain);
            Debug.Log($"Trời nắng. Cơn mưa tiếp theo sẽ đến sau: {waitTime / 60f} phút.");
            yield return new WaitForSeconds(waitTime);
        }
    }
}