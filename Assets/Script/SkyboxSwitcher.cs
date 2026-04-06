using UnityEngine;

public class SkyboxSwitcher : MonoBehaviour
{
    public Material daySkybox;
    public Material nightSkybox;

    public float switchTime = 60f; // sau 60 giây đổi
    private float timer;
    private bool isDay = true;

    void Start()
    {
        RenderSettings.skybox = daySkybox;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= switchTime)
        {
            timer = 0f;
            isDay = !isDay;

            if (isDay)
                RenderSettings.skybox = daySkybox;
            else
                RenderSettings.skybox = nightSkybox;

            DynamicGI.UpdateEnvironment(); // cập nhật ánh sáng
        }
    }
}