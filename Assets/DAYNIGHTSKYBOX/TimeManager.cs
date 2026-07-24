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
    
    [Tooltip("Trục X là thời gian (0 đến 24h). Trục Y là độ sáng (0 = Đêm, 1 = Ngày)")]
    [SerializeField] AnimationCurve dayNightCurve; // Curve mới thay thế curve cũ
    
    [SerializeField] float maxSunIntensity = 1;
    [SerializeField] float maxMoonIntensity = 0.5f;

    [Header("Colors (Màu sắc)")]
    [SerializeField] Color dayAmbientLight;
    [SerializeField] Color nightAmbientLight;
    [Tooltip("Màu của mặt trời ban ngày (thường là Trắng/Vàng nhạt)")]
    [SerializeField] Color daySunColor = Color.white;
    [Tooltip("Màu của mặt trời ban đêm (thường là Xanh dương đậm)")]
    [SerializeField] Color nightSunColor = new Color(0.1f, 0.2f, 0.4f);

    [Header("Volume & Material")]
    [SerializeField] Volume volume;
    [SerializeField] Material skyboxMaterial;

    [Header("UI Dial")]
    [SerializeField] RectTransform dial;

    float initialDialRotation;
    ColorAdjustments colorAdjustments;
    TimeService service;

    private Color originalAmbientColor;
    private float originalSkyBlend;
    private float baseMultiplier;

    public event Action OnSunrise
    {
        add { if (service != null) service.OnSunrise += value; }
        remove { if (service != null) service.OnSunrise -= value; }
    }

    public event Action OnSunset
    {
        add { if (service != null) service.OnSunset += value; }
        remove { if (service != null) service.OnSunset -= value; }
    }

    public event Action OnHourChange
    {
        add { if (service != null) service.OnHourChange += value; }
        remove { if (service != null) service.OnHourChange -= value; }
    }

    void Awake()
    {
        if (timeSettings != null)
        {
            service = new TimeService(timeSettings);
            baseMultiplier = timeSettings.timeMultiplier;
        }
    }

    void Start()
    {
        if (volume != null && volume.profile != null)
        {
            if (volume.profile.TryGet(out colorAdjustments))
            {
                originalAmbientColor = colorAdjustments.colorFilter.value;
            }
        }

        if (skyboxMaterial != null)
        {
            originalSkyBlend = skyboxMaterial.GetFloat("_Blend");
        }

        if (dial != null)
        {
            initialDialRotation = dial.rotation.eulerAngles.z;
        }
    }

    void Update()
    {
        if (service == null) return;

        HandleDebugControls();
        UpdateTimeOfDay();
        UpdateLightingAndColors(); // Gom chung hàm tính toán ánh sáng
    }

    void HandleDebugControls()
    {
        if (timeSettings == null) return;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            timeSettings.timeMultiplier = 0f;
        }
        else if (Input.GetKey(KeyCode.U))
        {
            timeSettings.timeMultiplier = baseMultiplier * 1000f;
        }
        else
        {
            timeSettings.timeMultiplier = baseMultiplier;
        }
    }

    void UpdateLightingAndColors()
    {
        // 1. Lấy giờ hiện tại quy ra số thập phân (Ví dụ 12h30 -> 12.5)
        float currentHour = service.CurrentTime.Hour + (service.CurrentTime.Minute / 60f);

        // 2. Tra đồ thị Curve để biết thời điểm này ánh sáng là bao nhiêu (0 -> 1)
        float lightFactor = dayNightCurve.Evaluate(currentHour);

        // 3. Đổi độ sáng Mặt trời & Mặt trăng
        if (sun != null)
        {
            sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightFactor);
            // Đổi luôn màu của mặt trời (Ngày thì vàng, đêm thì xanh mờ)
            sun.color = Color.Lerp(nightSunColor, daySunColor, lightFactor); 
        }

        if (moon != null)
        {
            moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, lightFactor);
        }

        // 4. Đổi màu Volume (Môi trường)
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, lightFactor);
        }

        // 5. Đổi Skybox
        if (skyboxMaterial != null)
        {
            // Đảo ngược giá trị: Ngày (1) biến thành 0, Đêm (0) biến thành 1
            skyboxMaterial.SetFloat("_Blend", 1f - lightFactor); 
        }

        // 6. Xoay UI đồng hồ (xoay 360 độ theo chu kỳ 24h)
        if (dial != null)
        {
            float dialAngle = (currentHour / 24f) * 360f;
            dial.rotation = Quaternion.Euler(0, 0, -dialAngle + initialDialRotation);
        }
        RenderSettings.ambientLight = Color.Lerp(nightAmbientLight, dayAmbientLight, lightFactor);
    }

    void UpdateTimeOfDay()
    {
        service.UpdateTime(Time.deltaTime);
        if (timeText != null)
        {
            timeText.text = service.CurrentTime.ToString("HH:mm");
        }
    }

    private void OnDestroy()
    {
        if (timeSettings != null)
        {
            timeSettings.timeMultiplier = baseMultiplier;
        }

        if (skyboxMaterial != null)
        {
            skyboxMaterial.SetFloat("_Blend", originalSkyBlend);
        }
    }
}