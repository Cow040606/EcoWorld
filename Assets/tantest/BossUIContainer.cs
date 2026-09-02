using UnityEngine;
using UnityEngine.UI;

public class BossUIManager : MonoBehaviour
{
    [SerializeField] private GameObject uiContainer; // Kéo BossHealthUI (hoặc cha của nó) vào đây
    [SerializeField] private Slider hpSlider;
    [SerializeField] private float showRadius = 30f; // Tầm nhìn thấy thanh máu

    private BossController currentBoss;

    void Start()
    {
        uiContainer.SetActive(false);
    }

    void Update()
    {
        // 1. Nếu chưa có Boss, tìm Boss trên Scene (Nhưng chỉ tìm mỗi giây 1 lần để tránh giật lag)
        if (currentBoss == null)
        {
            if (Time.frameCount % 60 == 0) // Chỉ tìm 1 lần mỗi 60 frame (khoảng 1 giây)
            {
                currentBoss = FindObjectOfType<BossController>();
            }
            if (uiContainer.activeSelf) uiContainer.SetActive(false);
            return;
        }

        // 2. Lấy Local Player
        if (Player_Controller.localPlayer == null) return;

        // 3. Tính khoảng cách
        float dist = Vector3.Distance(Player_Controller.localPlayer.transform.position, currentBoss.transform.position);

        if (dist <= showRadius && currentBoss.CurrentHealth > 0)
        {
            if (!uiContainer.activeSelf) uiContainer.SetActive(true);
            hpSlider.maxValue = currentBoss.maxHealth;
            hpSlider.value = currentBoss.CurrentHealth;
        }
        else
        {
            if (uiContainer.activeSelf) uiContainer.SetActive(false);
        }
    }
}