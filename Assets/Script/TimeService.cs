using UnityEngine;
using System;
using UniRx;

public class TimeService
{
    readonly TimeSetting timeSetting;
    DateTime currentTime;
    readonly TimeSpan sunriseTime;
    readonly TimeSpan sunsetTime;

    public event Action Onsunrise = delegate { };
    public event Action Onsunset = delegate { };
    public event Action OnHourChanged = delegate { };

    readonly ReactiveProperty<bool> isDayTime;
    readonly ReactiveProperty<int> currentHour;

    public TimeService(TimeSetting settings)
    {
        timeSetting = settings;
        currentTime = DateTime.Now.Date + TimeSpan.FromHours(timeSetting.startHour);
        sunriseTime = TimeSpan.FromHours(timeSetting.sunriseHour);
        sunsetTime = TimeSpan.FromHours(timeSetting.sunsetHour);

        isDayTime = new ReactiveProperty<bool>(IsDayTime());
        currentHour = new ReactiveProperty<int>(currentTime.Hour);

        isDayTime.Subscribe(day =>
        {
            if (day) Onsunrise.Invoke();
            else Onsunset.Invoke();
        });

        currentHour.Subscribe(_ => OnHourChanged.Invoke());
    }

    public void UpdateTime(float deltaTime)
    {
        currentTime = currentTime.AddSeconds(deltaTime * timeSetting.timeMultiplier);
        isDayTime.Value = IsDayTime();
        currentHour.Value = currentTime.Hour;
    }

    public float CalculateSunAngle()
    {
        bool isDay = IsDayTime();
        float startDegree = isDay ? 0 : 180;
        TimeSpan start = isDay ? sunriseTime : sunsetTime;
        TimeSpan end = isDay ? sunsetTime : sunriseTime;

        TimeSpan totalTime = CalculateDifference(start, end);
        TimeSpan elapsedTime = CalculateDifference(start, currentTime.TimeOfDay);

        double percentage = elapsedTime.TotalSeconds / totalTime.TotalSeconds;
        return Mathf.Lerp(startDegree, startDegree + 180, (float)percentage);
    }

    public DateTime GetCurrentTime() => currentTime;

    bool IsDayTime()
        => currentTime.TimeOfDay >= sunriseTime && currentTime.TimeOfDay < sunsetTime;

    TimeSpan CalculateDifference(TimeSpan from, TimeSpan to)
    {
        TimeSpan difference = to - from;
        return difference.TotalHours < 0
            ? difference + TimeSpan.FromHours(24)
            : difference;
    }
}