using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarHUD : MonoBehaviour
{
    // Cầu nối tĩnh (Singleton) giúp các Boss tự gọi UI
    public static BossHealthBarHUD Instance;

    [Header("Giao diện HUD Fantasy")]
    [Tooltip("Kéo object Slider_Horizontal vào đây")]
    public Slider healthSlider;

    [Tooltip("Kéo object Label_BossName vào đây")]
    public TextMeshProUGUI txtBossName;

    [Tooltip("Kéo object Label_HP vào đây (để hiển thị số 100/100)")]
    public TextMeshProUGUI txtHP;

    private void Awake()
    {
        // Khởi tạo Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Cập nhật tên của Boss lên giao diện
    /// </summary>
    public void CapNhatTenBoss(string ten)
    {
        if (txtBossName != null)
        {
            txtBossName.text = ten;
        }
    }

    /// <summary>
    /// Đặt lại thanh máu về trạng thái đầy đủ
    /// </summary>
    public void ResetHealthBar()
    {
        if (healthSlider != null) healthSlider.value = 1f;
        if (txtHP != null) txtHP.text = "MAX";
    }

    /// <summary>
    /// Gọi hàm này khi Boss nhận sát thương hoặc hồi máu
    /// </summary>
    public void UpdateHealthBar(float currentHealth, float maxHealth)
    {
        // Đảm bảo máu không bị âm
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        float targetHealthRatio = currentHealth / maxHealth;

        // Cập nhật text máu (Ví dụ: 1500 / 2000)
        if (txtHP != null)
        {
            txtHP.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }

        // Cập nhật thanh Slider chính ngay lập tức
        if (healthSlider != null)
        {
            healthSlider.value = targetHealthRatio;
        }
    }
}