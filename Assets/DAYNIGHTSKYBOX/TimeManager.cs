using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [Header("UI & Dependencies")]
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TimeSettings timeSettings;

    [Header("Lighting & Skybox")]
    [SerializeField] Light sun;
    [SerializeField] Light moon;
    [SerializeField] AnimationCurve lightIntensityCurve;
    [SerializeField] float maxSunIntensity = 1;
    [SerializeField] float maxMoonIntensity = 0.5f;
    [SerializeField] Color dayAmbientLight;
    [SerializeField] Color nightAmbientLight;

    [Header("Volume & Material (Shared Mode)")]
    [SerializeField] Volume volume;
    [SerializeField] Material skyboxMaterial;

    ColorAdjustments colorAdjustments;
    TimeService service;

    // Lưu trữ giá trị gốc để reset khi tắt game (bảo vệ Asset trong Editor)
    private Color originalAmbientColor;
    private float originalSkyBlend;
    private float baseMultiplier; // Lưu lại tốc độ gốc (12) để khôi phục khi thả phím

    public event Action OnSunrise
    {
        add => service.OnSunrise += value;
        remove => service.OnSunrise -= value;
    }

    public event Action OnSunset
    {
        add => service.OnSunset += value;
        remove => service.OnSunset -= value;
    }

    public event Action OnHourChange
    {
        add => service.OnHourChange += value;
        remove => service.OnHourChange -= value;
    }

    void Start()
    {
        service = new TimeService(timeSettings);

        // Lưu lại hệ số tốc độ gốc khi bắt đầu game
        baseMultiplier = timeSettings.timeMultiplier;

        // SỬ DỤNG SHARED PROFILE THAY VÌ PROFILE (Tối ưu Unity 6)
        if (volume != null && volume.sharedProfile != null)
        {
            volume.sharedProfile.TryGet(out colorAdjustments);
            if (colorAdjustments != null)
            {
                originalAmbientColor = colorAdjustments.colorFilter.value;
            }
        }

        if (skyboxMaterial != null)
        {
            originalSkyBlend = skyboxMaterial.GetFloat("_Blend");
        }

        OnSunrise += () => Debug.Log("Sunrise");
        OnSunset += () => Debug.Log("Sunset");
        OnHourChange += () => Debug.Log("Hour change");
    }

    void Update()
    {
        // Chỉ cho phép can thiệp thời gian bằng phím khi không có Cutscene
        if (!isCutscenePlaying) 
        {
            HandleDebugControls();
        }
        
        UpdateTimeOfDay();
        UpdateLightSettings();
    }
    private bool isCutscenePlaying = false;
    public void ForceNightTimeForCutscene()
    {
        // Gọi hàm SetTime bên TimeService để ép về 20h
        service.SetTime(20); 
        
        // Đóng băng thời gian để không bị nhảy sang 21h lúc đang chiếu Cutscene
        timeSettings.timeMultiplier = 0f; 
    }

    // 2. Hàm gọi lúc KẾT THÚC Cutscene
    public void EndCutsceneTime()
    {
        // Trả lại tốc độ thời gian chạy bình thường
        timeSettings.timeMultiplier = baseMultiplier;
    }
    public void ForceNightTimeForCutscene()
    {
        // Gọi hàm SetTime bên TimeService để ép về 20h
        service.SetTime(20); 
        
        // Đóng băng thời gian để không bị nhảy sang 21h lúc đang chiếu Cutscene
        timeSettings.timeMultiplier = 0f; 
    }

    // 2. Hàm gọi lúc KẾT THÚC Cutscene
    public void EndCutsceneTime()
    {
        // Trả lại tốc độ thời gian chạy bình thường
        timeSettings.timeMultiplier = baseMultiplier;
    }

    void HandleDebugControls()
    {
        // 1. Phím LeftShift: Đè giữ để DỪNG thời gian
        if (Input.GetKey(KeyCode.LeftShift))
        {
            timeSettings.timeMultiplier = 0f;
        }
        // 2. Phím U: Đè giữ để TUA NHANH thời gian gấp 10 lần
        else if (Input.GetKey(KeyCode.U))
        {
            timeSettings.timeMultiplier = baseMultiplier * 1000f;
        }
        // 3. Khôi phục lại tốc độ cố định mặc định khi không bấm gì
        else
        {
            timeSettings.timeMultiplier = baseMultiplier;
        }
    }

    void UpdateLightSettings()
    {
        if (service == null || timeSettings == null) return;

        float currentHour = (float)service.CurrentTime.TimeOfDay.TotalHours;

        float daylightFactor;

        // Nếu có Curve trong Inspector (trục X là khung giờ 0..24h)
        if (lightIntensityCurve != null && lightIntensityCurve.length > 0)
        {
            daylightFactor = Mathf.Clamp01(lightIntensityCurve.Evaluate(currentHour));
        }
        else
        {
            // Dự phòng nếu chưa thiết lập Curve
            float sunriseHour = timeSettings.sunriseHour;
            float sunsetHour = timeSettings.sunsetHour;

            if (currentHour >= sunriseHour && currentHour < sunsetHour)
            {
                float dayDuration = sunsetHour - sunriseHour;
                float dayProgress = (currentHour - sunriseHour) / dayDuration;
                daylightFactor = Mathf.Sin(dayProgress * Mathf.PI);
            }
            else
            {
                daylightFactor = 0f;
            }
        }

        if (sun != null)
        {
            sun.intensity = Mathf.Lerp(0, maxSunIntensity, daylightFactor);
        }

        if (moon != null)
        {
            moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, daylightFactor);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, daylightFactor);
        }

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Blend", 1f - daylightFactor);
        }
    }

    void UpdateTimeOfDay()
    {
        service.UpdateTime(Time.deltaTime);
        if (timeText != null)
        {
            timeText.text = service.CurrentTime.ToString("HH:mm"); // Dùng HH:mm để chuẩn định dạng 24h
        }
    }

    // RESET DỮ LIỆU KHI TẮT GAME (Rất quan trọng khi dùng Shared Mode để bảo vệ asset asset)
    private void OnDestroy()
    {
        // Khôi phục lại tốc độ chuẩn trong ScriptableObject tránh bị lưu đè trong Editor
        if (timeSettings != null)
        {
            timeSettings.timeMultiplier = baseMultiplier;
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = originalAmbientColor;
        }
        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Blend", originalSkyBlend);
        }
    }
}