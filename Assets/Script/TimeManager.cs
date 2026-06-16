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

    void Start()
    {
        timeService = new TimeService(timeSetting);
        volume.profile.TryGet(out colorAdjustments);
    }

    void Update()
    {
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            timeSetting.timeMultiplier *= 3600;
        }

        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            timeSetting.timeMultiplier /= 2;
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
}