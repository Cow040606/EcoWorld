using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    // Cầu nối tĩnh (Singleton) giúp các Boss tự gọi UI mà không cần lệnh Find nặng nề
    public static HealthBar Instance;

    [Header("Giao diện UI")]
    public Slider healthSlider;
    public Slider lazySlider;
    public TextMeshProUGUI txtTenBoss;

    [Header("Cài đặt hiệu ứng")]
    public float lazyDelay = 0.5f;
    public float lerpSpeed = 10f;

    private float lazyCatchupTime;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // Đảm bảo chỉ có 1 thanh máu duy nhất tồn tại
        }
    }

    public void CapNhatTenBoss(string ten)
    {
        if (txtTenBoss != null)
        {
            txtTenBoss.text = ten;
        }
    }

    public void ResetHealthBar()
    {
        if (healthSlider != null) healthSlider.value = 1f;
        if (lazySlider != null) lazySlider.value = 1f;
    }

    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        float targetHealthRatio = currentHealth / maxHealth;

        if (healthSlider != null && healthSlider.value != targetHealthRatio)
        {
            bool isTakingDamage = targetHealthRatio < healthSlider.value;
            healthSlider.value = targetHealthRatio;

            if (isTakingDamage)
                lazyCatchupTime = Time.time + lazyDelay;
            else if (lazySlider != null)
                lazySlider.value = healthSlider.value;
        }

        if (lazySlider != null && lazySlider.value != targetHealthRatio)
        {
            if (Time.time >= lazyCatchupTime)
            {
                lazySlider.value = Mathf.Lerp(lazySlider.value, targetHealthRatio, Time.deltaTime * lerpSpeed);
            }
        }
    }
}