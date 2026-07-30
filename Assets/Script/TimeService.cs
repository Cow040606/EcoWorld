using System;
using UnityEngine;

public class TimeService {
    readonly TimeSettings settings;
    DateTime currentTime;
    readonly TimeSpan sunriseTime;
    readonly TimeSpan sunsetTime;

    public DateTime CurrentTime => currentTime;

    public event Action OnSunrise = delegate { };
    public event Action OnSunset = delegate { };
    public event Action OnHourChange = delegate { };

    readonly Observable<bool> isDayTime;
    readonly Observable<int> currentHour;

    public TimeService(TimeSettings settings) {
        this.settings = settings;
        currentTime = DateTime.Now.Date + TimeSpan.FromHours(settings.startHour);
        sunriseTime = TimeSpan.FromHours(settings.sunriseHour);
        sunsetTime = TimeSpan.FromHours(settings.sunsetHour);
        
        isDayTime = new Observable<bool>(IsDayTime());
        currentHour = new Observable<int>(currentTime.Hour);
        
        isDayTime.ValueChanged += day => (day ? OnSunrise : OnSunset)?.Invoke();
        currentHour.ValueChanged += _ => OnHourChange?.Invoke();
    }

    public void UpdateTime(float deltaTime) {
        currentTime = currentTime.AddSeconds(deltaTime * settings.timeMultiplier);
        isDayTime.Value = IsDayTime();
        currentHour.Value = currentTime.Hour;
    }
    public void SetTime(int targetHour) {
        currentTime = currentTime.Date + TimeSpan.FromHours(targetHour);
        isDayTime.Value = IsDayTime();
        currentHour.Value = currentTime.Hour;
    }
    public float GetDayFactor() {
        float timeInHours = (float)currentTime.TimeOfDay.TotalHours;
        float sunrise = settings.sunriseHour;
        float sunset = settings.sunsetHour;
        float trans = settings.transitionDuration;

        if (trans <= 0f)
        {
            return IsDayTime() ? 1f : 0f;
        }

        float halfTrans = trans / 2f;

        float sunriseStart = sunrise - halfTrans;
        float sunriseEnd = sunrise + halfTrans;

        float sunsetStart = sunset - halfTrans;
        float sunsetEnd = sunset + halfTrans;

        if (timeInHours >= sunriseStart && timeInHours <= sunriseEnd)
        {
            return Mathf.Clamp01((timeInHours - sunriseStart) / trans);
        }
        else if (timeInHours > sunriseEnd && timeInHours < sunsetStart)
        {
            return 1f;
        }
        else if (timeInHours >= sunsetStart && timeInHours <= sunsetEnd)
        {
            return Mathf.Clamp01(1f - (timeInHours - sunsetStart) / trans);
        }
        else
        {
            return 0f;
        }
    }

    public float CalculateSunAngle() {
        double percentage = currentTime.TimeOfDay.TotalHours / 24.0;
        return (float)(percentage * 360.0);
    }

    // ĐÃ SỬA: Thêm ">=" để tránh lọt khung hình tại thời điểm chuyển giao
    bool IsDayTime() => currentTime.TimeOfDay >= sunriseTime && currentTime.TimeOfDay < sunsetTime;
    
    TimeSpan CalculateDifference(TimeSpan from, TimeSpan to) {
        TimeSpan difference = to - from;
        return difference.TotalHours < 0 ? difference + TimeSpan.FromHours(24) : difference;
    }
}