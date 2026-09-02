using Fusion;
using UnityEngine;

public class RockSpawner : NetworkBehaviour
{
    [Header("Cấu hình Spawner")]
    public NetworkPrefabRef rockPrefab;
    public int numberOfRocks = 50;
    public float areaSize = 50f;

    [Header("Thuật toán Raycast & Địa hình")]
    [Tooltip("Độ cao bắt đầu bắn tia Raycast từ trên trời xuống")]
    public float castHeight = 100f;

    [Tooltip("Layer của mặt đất (Terrain) để đá bám vào")]
    public LayerMask groundLayer;

    [Tooltip("Layer của vật cản (Nước, Cây cối, Đá khác) để tránh đẻ đè lên")]
    public LayerMask obstacleLayer;

    [Tooltip("Bán kính không gian an toàn xung quanh cục đá")]
    public float checkRadius = 1f;

    [Tooltip("Số lần thử lại nếu random trúng chỗ có vật cản")]
    public int maxRetries = 10;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SpawnRocks();
        }
    }

    private void SpawnRocks()
    {
        int spawnedCount = 0;

        for (int i = 0; i < numberOfRocks; i++)
        {
            Vector3 validPosition = Vector3.zero;
            bool foundPosition = false;

            // Vòng lặp Retry: Cố gắng tìm vị trí hợp lệ tối đa 'maxRetries' lần
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                // 1. Random vị trí X, Z theo hình tròn quanh Spawner
                Vector2 randomCircle = Random.insideUnitCircle * areaSize;

                // 2. Đưa điểm bắt đầu lên thật cao (Dùng vị trí hiện tại của Spawner làm gốc)
                Vector3 rayStartPos = transform.position + new Vector3(randomCircle.x, castHeight, randomCircle.y);

                // 3. Bắn tia Raycast thẳng xuống mặt đất (chỉ chạm groundLayer)
                if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, castHeight * 2f, groundLayer))
                {
                    // 4. KIỂM TRA CHỒNG LẤP: Quét 1 khối cầu xem có vướng vật cản/nước/đá khác không
                    if (!Physics.CheckSphere(hit.point, checkRadius, obstacleLayer))
                    {
                        // Tìm được vị trí NGON -> Chốt!
                        validPosition = hit.point;
                        foundPosition = true;
                        break;
                    }
                }
            }

            // Nếu tìm được vị trí hợp lệ thì gọi Fusion Spawn
            if (foundPosition)
            {
                Runner.Spawn(rockPrefab, validPosition, Quaternion.identity);
                spawnedCount++;
            }
        }

        // Debug.Log($"[Spawner] Đã rải thành công {spawnedCount}/{numberOfRocks} cục đá.");
    }

    // Vẽ hình tròn ranh giới màu xanh lá cây trong cửa sổ Scene để dễ căn chỉnh
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, areaSize);
    }
}