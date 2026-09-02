using UnityEngine;

public class OrcSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    public GameObject spikePrefab; // Kéo Prefab cụm gai vào đây
    public Transform attackPoint;  // Vị trí gai mọc lên (VD: trước mặt Orc)
    public float spikeDuration = 3f; // Thời gian gai tồn tại trước khi biến mất

    [Header("Cooldown Settings (Chống Spam Skill)")]
    public float skillCooldown = 8f; // Thời gian hồi chiêu tối thiểu (giây)
    private float lastCastTime = -999f;

    // Tên hàm này phải nhập CHÍNH XÁC với tên Function đã đặt ở Animation Event
    public void OnSmashGround()
    {
        // Kiểm tra thời gian hồi chiêu để không bị spam liên tục
        if (Time.time < lastCastTime + skillCooldown) return;

        if (spikePrefab != null && attackPoint != null)
        {
            lastCastTime = Time.time;
            // Sinh ra vùng gai tại vị trí của attackPoint
            GameObject spawnedSpikes = Instantiate(spikePrefab, attackPoint.position, attackPoint.rotation);
            
            // Xóa vùng gai sau một khoảng thời gian để dọn rác bộ nhớ
            Destroy(spawnedSpikes, spikeDuration);
        }
    }
}
