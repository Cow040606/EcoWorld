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

    [Header("Sun & Moon Rotation Settings")]
    [SerializeField] Vector3 daySunEuler = new Vector3(50f, -30f, 0f);
    [SerializeField] Vector3 nightSunEuler = new Vector3(-50f, -30f, 0f);

    [Header("Volume & Material (Shared Mode)")]
    [SerializeField] Volume volume;
    [SerializeField] Material skyboxMaterial;

    [Header("UI Dial")]
    [SerializeField] RectTransform dial;

    // Biến cờ để ngăn xung đột thời gian khi đang chạy Cutscene
    public bool isCutscenePlaying = false; 

    float initialDialRotation;
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

        if (dial != null)
        {
            initialDialRotation = dial.rotation.eulerAngles.z;
        }
    }

    void Update()
    {
        // Chỉ cho phép dùng phím tua thời gian khi KHÔNG có Cutscene
        if (!isCutscenePlaying)
        {
            HandleDebugControls();
        }
        
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
        UpdateSkyBlend();
    }

    // ==========================================
    // HÀM DÀNH CHO TIMELINE (CUTSCENE)
    // ==========================================
    public void ForceNightTimeForCutscene()
    {
        isCutscenePlaying = true; // Bật cờ khóa nút Debug
        service.SetTime(20);      // Ép về 20h
        timeSettings.timeMultiplier = 0f; // Đóng băng thời gian
    }

    public void EndCutsceneTime()
    {
        isCutscenePlaying = false; // Tắt cờ, cho phép dùng nút Debug lại
        timeSettings.timeMultiplier = baseMultiplier; // Trả lại tốc độ bình thường
    }
    // ==========================================

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

    float GetLightFactor()
    {
        if (service == null) return 1f;

        if (lightIntensityCurve != null && lightIntensityCurve.keys != null && lightIntensityCurve.keys.Length > 0)
        {
            float maxKeyTime = lightIntensityCurve.keys[lightIntensityCurve.keys.Length - 1].time;
            if (maxKeyTime > 1.1f)
            {
                // Đường cong được vẽ theo mốc thời gian 24 giờ (0 - 24) trong Inspector
                float timeInHours = (float)service.CurrentTime.TimeOfDay.TotalHours;
                return Mathf.Clamp01(lightIntensityCurve.Evaluate(timeInHours));
            }
            else
            {
                // Đường cong chuẩn hóa từ 0.0 đến 1.0
                float dayFactor = service.GetDayFactor();
                return Mathf.Clamp01(lightIntensityCurve.Evaluate(dayFactor));
            }
        }

        return service.GetDayFactor();
    }

    void UpdateSkyBlend()
    {
        float lightFactor = GetLightFactor();
        float blend = Mathf.Lerp(1f, 0f, lightFactor); // 0 = Ban Ngày, 1 = Ban Đêm

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Blend", blend);
        }

        if (RenderSettings.skybox != null)
        {
            if (RenderSettings.skybox.HasProperty("_Blend"))
            {
                RenderSettings.skybox.SetFloat("_Blend", blend);
            }
            if (RenderSettings.skybox.HasProperty("_Blendvaluelerp"))
            {
                RenderSettings.skybox.SetFloat("_Blendvaluelerp", blend);
            }
        }
    }

    void UpdateLightSettings()
    {
        if (sun == null || moon == null) return;

        float lightFactor = GetLightFactor();

        sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightFactor);
        moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, lightFactor);

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, lightFactor);
        }
    }

    void RotateSun()
    {
        float lightFactor = GetLightFactor();

        if (sun != null)
        {
            // Lerp hướng chiếu của Mặt trời giữa Ban Đêm và Ban Ngày thay vì tự xoay 360 độ liên tục
            sun.transform.rotation = Quaternion.Lerp(
                Quaternion.Euler(nightSunEuler),
                Quaternion.Euler(daySunEuler),
                lightFactor
            );
        }

        if (dial != null && service != null)
        {
            float rotation = service.CalculateSunAngle();
            dial.rotation = Quaternion.Euler(0, 0, -rotation + initialDialRotation);
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