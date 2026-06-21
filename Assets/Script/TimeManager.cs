using UnityEngine;
using System;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TimeManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] Light Sun;
    [SerializeField] Light Moon;
    [SerializeField] AnimationCurve lightIntensityCurve;
    [SerializeField] float maxSunIntensity = 1f;
    [SerializeField] float maxMoonIntensity = 0.5f;
    [SerializeField] Color dayAmbientLight;
    [SerializeField] Color nightAmbientLight;
    [SerializeField] Volume volume;
    [SerializeField] TimeSetting timeSetting;

    ColorAdjustments colorAdjustments;
    TimeService timeService;

    // Biến để lưu lại tốc độ gốc, bảo vệ ScriptableObject không bị lưu đè số tào lao
    float defaultTimeMultiplier;

    void Start()
    {
        timeService = new TimeService(timeSetting);
        volume.profile.TryGet(out colorAdjustments);

        // Lưu lại tốc độ thời gian ban đầu khi game vừa bắt đầu (ví dụ: 360)
        defaultTimeMultiplier = timeSetting.timeMultiplier;
    }

    void Update()
    {
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
        
        // Tách phần kiểm tra phím bấm ra hàm riêng cho sạch code
        HandleTimeSpeedInput(); 
    }

    void HandleTimeSpeedInput()
    {
        // Khi BẤM nút U: Gán thẳng tốc độ tua nhanh là 3600 (Không dùng dấu *= nữa)
        if (Input.GetKeyDown(KeyCode.U))
        {
            timeSetting.timeMultiplier = 3600;
        }

        // Khi NHẢ nút U: Trả về tốc độ bình thường ban đầu
        if (Input.GetKeyUp(KeyCode.U))
        {
            timeSetting.timeMultiplier = defaultTimeMultiplier;
        }
    }

    void UpdateLightSettings()
    {
        float dotProduct = Vector3.Dot(Sun.transform.forward, Vector3.down);

        Sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightIntensityCurve.Evaluate(dotProduct));
        Moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, lightIntensityCurve.Evaluate(dotProduct));

        if (colorAdjustments == null)
        {
            return;
        }

        colorAdjustments.colorFilter.value =
            Color.Lerp(nightAmbientLight, dayAmbientLight, lightIntensityCurve.Evaluate(dotProduct));
    }

    void RotateSun()
    {
        float rotation = timeService.CalculateSunAngle();
        Sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);
    }

    void UpdateTimeOfDay()
    {
        timeService.UpdateTime(Time.deltaTime);

        if (timeText != null)
        {
            timeText.text = timeService.GetCurrentTime().ToString("HH:mm");
        }
    }

    // Đảm bảo khi tắt chế độ Play, ScriptableObject luôn được trả về mốc chuẩn
    void OnDisable()
    {
        if (timeSetting != null)
        {
            timeSetting.timeMultiplier = defaultTimeMultiplier;
        }
    }
}