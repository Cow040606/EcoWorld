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

    [Header("UI Dial")]
    [SerializeField] RectTransform dial;

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
        HandleDebugControls();
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
        UpdateSkyBlend();
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

    void UpdateSkyBlend()
    {
        if (skyboxMaterial == null || sun == null) return;

        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.up);
        float blend = Mathf.Lerp(0, 1, lightIntensityCurve.Evaluate(dotProduct));

        // Modifying shared material directly
        skyboxMaterial.SetFloat("_Blend", blend);
    }

    void UpdateLightSettings()
    {
        if (sun == null || moon == null) return;

        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.down);
        float lightIntensity = lightIntensityCurve.Evaluate(Mathf.Clamp01(dotProduct));

        sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightIntensity);
        moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, lightIntensity);

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, lightIntensity);
        }
    }

    void RotateSun()
    {
        if (sun == null) return;

        float rotation = service.CalculateSunAngle();
        sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);

        if (dial != null)
        {
            dial.rotation = Quaternion.Euler(0, 0, rotation + initialDialRotation);
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