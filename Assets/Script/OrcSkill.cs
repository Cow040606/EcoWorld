using UnityEngine;

public class OrcSkill : MonoBehaviour
{
    [Header("Skill Settings")]
    public GameObject spikePrefab; // Kéo Prefab cụm gai vào đây
    public Transform attackPoint;  // Vị trí gai mọc lên (VD: trước mặt Orc)
    public float spikeDuration = 3f; // Thời gian gai tồn tại trước khi biến mất

    // Tên hàm này phải nhập CHÍNH XÁC với tên Function đã đặt ở Animation Event
    public void OnSmashGround()
    {
        if (spikePrefab != null && attackPoint != null)
        {
            // Sinh ra vùng gai tại vị trí của attackPoint
            GameObject spawnedSpikes = Instantiate(spikePrefab, attackPoint.position, attackPoint.rotation);
            
            // Xóa vùng gai sau một khoảng thời gian để dọn rác bộ nhớ
            Destroy(spawnedSpikes, spikeDuration);
        }
    }
}