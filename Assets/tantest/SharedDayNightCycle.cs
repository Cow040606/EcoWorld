using Fusion;
using UnityEngine;

public class SharedDayNightCycle : NetworkBehaviour
{
    [Header("Cài đặt Thời gian")]
    [Tooltip("Thời gian tính bằng giây để hoàn thành 1 chu kỳ ngày đêm trong game")]
    public float dayDuration = 120f; 

    [Header("Cài đặt Ánh sáng")]
    [Tooltip("Trục ngang (0 -> 1): 0 và 1 là nửa đêm, 0.5 là giữa trưa. Trục dọc: Cường độ đèn mặt trời.")]
    public AnimationCurve lightIntensityCurve;

    // Biến mạng đồng bộ thời gian trôi giữa tất cả các máy (Giá trị từ 0.0 đến 1.0)
    [Networked] public float CurrentTimeOfDay { get; set; }

    private Light sunLight;

    public override void Spawned()
    {
        sunLight = GetComponent<Light>();
        
        if (sunLight == null)
        {
            Debug.LogError("Script này phải được gắn trực tiếp vào Directional Light (Mặt trời)!");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Trong chế độ Shared Mode, chỉ Client có quyền State Authority (người tạo phòng/object) mới được tính toán thời gian
        if (Object.HasStateAuthority)
        {
            CurrentTimeOfDay += Runner.DeltaTime / dayDuration;
            
            // Reset khi qua ngày mới
            if (CurrentTimeOfDay >= 1f)
            {
                CurrentTimeOfDay -= 1f;
            }
        }
    }

    public override void Render()
    {
        // 1. XOAY MẶT TRỜI (Bắt đầu từ giữa trưa và quay sang đêm)
        float sunAngle = CurrentTimeOfDay * 360f;
        
        // Cộng 90 độ để khi CurrentTimeOfDay = 0, Mặt trời ở góc 90 độ (Chiếu thẳng từ đỉnh đầu xuống - Giữa trưa)
        transform.rotation = Quaternion.Euler(new Vector3(sunAngle + 90f, 0f, 0f));

        // 2. TÍNH TOÁN ĐỘ HOÀ TRỘN (BLEND VALUE) BẰNG DOT PRODUCT
        // Lấy hướng tia sáng mặt trời so với hướng cắm thẳng xuống đất
        float sunDot = Vector3.Dot(transform.forward, Vector3.down);
        
        // Chuyển đổi dải giá trị từ [-1, 1] sang [0, 1] để truyền vào Shader Graph
        // Giữa trưa (sunDot = 1)   -> blendValue = 0 (100% Bầu trời ngày)
        // Hoàng hôn (sunDot = 0)   -> blendValue = 0.5 (Pha trộn 50/50)
        // Nửa đêm (sunDot = -1)    -> blendValue = 1 (100% Bầu trời đêm)
        float blendValue = (sunDot * -0.5f) + 0.5f;

        // 3. ĐẨY GIÁ TRỊ VÀO SHADER GRAPH GIÚP ĐỔI MÀU SKYBOX
        if (RenderSettings.skybox != null)
        {
            // Sử dụng chính xác tên Reference đã cấu hình trong Shader Graph
            RenderSettings.skybox.SetFloat("_Blendvaluelerp", blendValue);
        }

        // 4. ĐIỀU CHỈNH CƯỜNG ĐỘ ĐÈN THEO CURVE (TÙY CHỌN)
        if (lightIntensityCurve != null && lightIntensityCurve.keys.Length > 0)
        {
            // Chuẩn hóa đường cong: Giữa trưa là 0.5 (Đỉnh sáng nhất)
            float curveTime = (sunDot + 1f) / 2f; 
            sunLight.intensity = lightIntensityCurve.Evaluate(curveTime);
        }
    }
}