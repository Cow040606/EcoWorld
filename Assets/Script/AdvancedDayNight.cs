using UnityEngine;

public class AdvancedDayNight : MonoBehaviour
{
    [Header("Time Settings")]
    public float dayDuration = 120f; // 1 ngày = 120s

    [Header("Lighting")]
    public Light sun;

    [Header("Skybox")]
    public Material daySkybox;
    public Material nightSkybox;

    private float timeOfDay = 0f; // 0 → 1
    private bool isDay;

    void Update()
    {
        // Tính thời gian
        timeOfDay += Time.deltaTime / dayDuration;
        if (timeOfDay >= 1f)
            timeOfDay = 0f;

        // Xoay mặt trời
        float sunAngle = timeOfDay * 360f - 90f;
        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);

        // Tính độ sáng
        float intensity = Mathf.Clamp01(Vector3.Dot(sun.transform.forward, Vector3.down));
        sun.intensity = intensity;

        // Đổi skybox khi sang ngày/đêm
        if (intensity > 0.2f && !isDay)
        {
            isDay = true;
            RenderSettings.skybox = daySkybox;
            DynamicGI.UpdateEnvironment();
        }
        else if (intensity <= 0.2f && isDay)
        {
            isDay = false;
            RenderSettings.skybox = nightSkybox;
            DynamicGI.UpdateEnvironment();
        }
    }
}