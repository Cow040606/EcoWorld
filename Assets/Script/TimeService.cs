using UnityEngine;
using System;

public class TimeService 
{
    readonly TimeSetting timeSetting;
    DateTime currentTime;
    readonly TimeSpan sunriseTime;
    readonly TimeSpan sunsetTime;
    public TimeService(TimeSetting settings)
    {
        this.timeSetting = settings;
        currentTime = DateTime.Now.Date + TimeSpan.FromHours(timeSetting.startHour);
        sunriseTime = TimeSpan.FromHours(timeSetting.sunriseHour);
        sunsetTime = TimeSpan.FromHours(timeSetting.sunsetHour);
    }
    public void UpdateTime(float deltaTime)
    {
        currentTime = currentTime.AddSeconds(deltaTime * timeSetting.timeMultiplier);
    }
    bool IsDayTime()
    => currentTime.TimeOfDay >= sunriseTime && currentTime.TimeOfDay < sunsetTime;
    TimeSpan CalculateDifference(TimeSpan from, TimeSpan to)
    {
       TimeSpan difference = to - from;
    }
}
      