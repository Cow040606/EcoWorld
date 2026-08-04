using UnityEngine;

[CreateAssetMenu(fileName = "TimeSettings", menuName = "TimeSettings")]
public class TimeSettings : ScriptableObject
{
    [Header("Cấu hình chu kỳ (1 ngày game = 24 phút thực tế)")]
    public float timeMultiplier = 60f;
    public float startHour = 12f;
    public float sunriseHour = 6f;
    public float sunsetHour = 18f;

    [Header("Cấu hình chuyển đổi Sáng - Tối")]
    [Tooltip("Thời gian chuyển đổi mượt giữa Sáng và Tối (tính bằng giờ game). VD: 1.0 = chuyển đổi kéo dài 1 giờ xung quanh giờ bình minh/hoàng hôn")]
    public float transitionDuration = 1.0f;
}