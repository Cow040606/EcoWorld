using UnityEngine;

[CreateAssetMenu(fileName = "TimeSettings", menuName = "TimeSettings")]
public class TimeSettings : ScriptableObject
{
    [Header("Cấu hình chu kỳ (1 ngày game = 2 giờ thực tế)")]
    public float timeMultiplier = 12f;
    public float startHour = 12f;
    public float sunriseHour = 6f;
    public float sunsetHour = 18f;
}