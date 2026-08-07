using Fusion;
using UnityEngine;

public class WeatherController : NetworkBehaviour
{
    [Header("Cài đặt đối tượng")]
    [Tooltip("Không cần kéo thả bằng tay nữa, code sẽ tự tìm!")]
    public Transform playerTransform;
    public ParticleSystem rainParticle;

    [Header("Cài đặt vị trí")]
    public float heightOffset = 15f;

    [Header("Cài đặt thời gian nắng chờ mưa (Tính bằng giây)")]
    public float minTimeBetweenRain = 2400f;
    public float maxTimeBetweenRain = 3600f;

    [Header("Cài đặt thời gian mưa (Tính bằng giây)")]
    public float minRainDuration = 60f;
    public float maxRainDuration = 180f;

    // --- BIẾN ĐỒNG BỘ MẠNG (Chỉ Host mới được sửa, Client tự động cập nhật theo) ---
    [Networked]
    public NetworkBool IsRaining { get; set; }

    [Networked]
    public TickTimer WeatherTimer { get; set; }

    // Biến nội bộ để kiểm tra xem state vừa thay đổi hay không
    private bool _wasRaining;

    public override void Spawned()
    {
        // Tắt mưa lúc mới vào game trên mọi máy
        if (rainParticle != null) rainParticle.Stop();
        _wasRaining = false;

        // CHỈ HOST (State Authority) mới được quyền khởi tạo thời tiết ban đầu
        if (HasStateAuthority)
        {
            IsRaining = false;
            float waitTime = Random.Range(minTimeBetweenRain, maxTimeBetweenRain);
            WeatherTimer = TickTimer.CreateFromSeconds(Runner, waitTime); // Đặt giờ nắng
            Debug.Log($"[Host] Trời nắng. Cơn mưa tiếp theo sẽ đến sau: {waitTime / 60f} phút.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // CHỈ HOST mới chạy logic đếm thời gian
        if (HasStateAuthority)
        {
            // Kiểm tra xem bộ đếm thời gian đã chạy hết chưa
            if (WeatherTimer.Expired(Runner))
            {
                if (IsRaining)
                {
                    // Đang mưa -> Chuyển sang Nắng
                    IsRaining = false;
                    float waitTime = Random.Range(minTimeBetweenRain, maxTimeBetweenRain);
                    WeatherTimer = TickTimer.CreateFromSeconds(Runner, waitTime);
                    Debug.Log($"[Host] Đã tạnh mưa. Nắng trong {waitTime / 60f} phút.");
                }
                else
                {
                    // Đang nắng -> Chuyển sang Mưa
                    IsRaining = true;
                    float rainTime = Random.Range(minRainDuration, maxRainDuration);
                    WeatherTimer = TickTimer.CreateFromSeconds(Runner, rainTime);
                    Debug.Log($"[Host] Bắt đầu mưa. Mưa kéo dài trong {rainTime / 60f} phút.");
                }
            }
        }
    }

    public override void Render()
    {
        // 1. Máy nào cũng tự tìm Player và gắn mây trên đầu (Logic local)
        UpdateCloudPosition();

        // 2. Bật/Tắt hiệu ứng mưa dựa trên biến đồng bộ mạng của Host
        if (IsRaining != _wasRaining)
        {
            _wasRaining = IsRaining; // Cập nhật lại state hiện tại

            if (IsRaining)
            {
                if (rainParticle != null)
                {
                    rainParticle.gameObject.SetActive(true);
                    rainParticle.Clear();
                    rainParticle.Play(true);
                }
            }
            else
            {
                if (rainParticle != null)
                {
                    rainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }
    }

    private void UpdateCloudPosition()
    {
        // Liên tục tìm Player nếu chưa thấy
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.Find("Player_Character(Clone)");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        // Cập nhật vị trí mây
        if (playerTransform != null)
        {
            transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y + heightOffset, playerTransform.position.z);
        }
    }
}