using UnityEngine;
using System;

public class TimeService
{
    readonly TimeSetting timeSetting;

    DateTime currentTime;

    readonly TimeSpan sunriseTime;
    readonly TimeSpan sunsetTime;

    public event Action Onsunrise = delegate { };
    public event Action Onsunset = delegate { };
    public event Action OnHourChanged = delegate { };

    bool isDayTime;
    int currentHour;

    public TimeService(TimeSetting settings)
    {
        timeSetting = settings;

        currentTime = DateTime.Now.Date +
                      TimeSpan.FromHours(timeSetting.startHour);

        sunriseTime = TimeSpan.FromHours(timeSetting.sunriseHour);
        sunsetTime = TimeSpan.FromHours(timeSetting.sunsetHour);

        isDayTime = IsDayTime();
        currentHour = currentTime.Hour;
    }

    public void UpdateTime(float deltaTime)
    {
        currentTime = currentTime.AddSeconds(
            deltaTime * timeSetting.timeMultiplier
        );

        bool newDayTime = IsDayTime();

        // Ki?m tra chuy?n ??i ngày/?êm
        if (newDayTime != isDayTime)
        {
            isDayTime = newDayTime;

            if (isDayTime)
                Onsunrise.Invoke();
            else
                Onsunset.Invoke();
        }

        // Ki?m tra ??i gi?
        if (currentTime.Hour != currentHour)
        {
            currentHour = currentTime.Hour;
            OnHourChanged.Invoke();
        }
    }

    public float CalculateSunAngle()
    {
        bool isDay = IsDayTime();

        float startDegree = isDay ? 0f : 180f;

        TimeSpan start = isDay
            ? sunriseTime
            : sunsetTime;

        TimeSpan end = isDay
            ? sunsetTime
            : sunriseTime;

        TimeSpan totalTime =
            CalculateDifference(start, end);

        TimeSpan elapsedTime =
            CalculateDifference(
                start,
                currentTime.TimeOfDay
            );

        double percentage =
            elapsedTime.TotalSeconds /
            totalTime.TotalSeconds;

        return Mathf.Lerp(
            startDegree,
            startDegree + 180f,
            (float)percentage
        );
    }

    public DateTime GetCurrentTime()
    {
        return currentTime;
    }

    bool IsDayTime()
    {
        return currentTime.TimeOfDay >= sunriseTime
            && currentTime.TimeOfDay < sunsetTime;
    }

    TimeSpan CalculateDifference(
        TimeSpan from,
        TimeSpan to
    )
    {
        TimeSpan difference = to - from;

        if (difference.TotalHours < 0)
        {
            difference += TimeSpan.FromHours(24);
        }

        return difference;
    }
}