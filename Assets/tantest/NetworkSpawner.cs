using Fusion;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NetworkSpawner : NetworkBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("Danh sách các Prefab cần spawn (Phải có NetworkObject)")]
    public List<NetworkObject> prefabsToSpawn;

    [Tooltip("Bán kính khu vực spawn tính từ vị trí của GameObject này")]
    public float spawnRadius = 10f;

    [Tooltip("Thời gian chờ giữa mỗi lần spawn (giây)")]
    public float spawnInterval = 5f;

    [Tooltip("Số lượng tối đa được phép tồn tại cùng lúc được sinh ra từ Spawner này")]
    public int maxEntities = 5;

    // Danh sách lưu trữ các object đã spawn để kiểm soát số lượng
    private List<NetworkObject> spawnedEntities = new List<NetworkObject>();
    private float timer = 0f;

    public override void FixedUpdateNetwork()
    {
        // CHỈ HỆ THỐNG HOST/SERVER MỚI CÓ QUYỀN SPAWN
        if (!HasStateAuthority) return;

        // Dọn dẹp danh sách: Xóa các object đã bị tiêu diệt (null)
        spawnedEntities.RemoveAll(item => item == null);

        // Nếu số lượng hiện tại đã đạt giới hạn thì không spawn thêm
        if (spawnedEntities.Count >= maxEntities) return;

        // Đếm ngược thời gian
        timer += Runner.DeltaTime;
        if (timer >= spawnInterval)
        {
            SpawnRandomEntity();
            timer = 0f; // Reset timer
        }
    }

    private void SpawnRandomEntity()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0) return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;

        // Bắt đầu từ trên cao 100 unit để bắn tia xuống
        Vector3 rayStartPos = transform.position + new Vector3(randomCircle.x, 100f, randomCircle.y);

        // Bắn tia Raycast xuống đất với khoảng cách 200f
        if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hitInfo, 200f))
        {
            NavMeshHit navHit;

            // ÉP BÁN KÍNH KIỂM TRA NHỎ LẠI (1.5f). 
            // Nếu để quá to, nó sẽ quét xa và lại hút về rìa.
            if (NavMesh.SamplePosition(hitInfo.point, out navHit, 1.5f, NavMesh.AllAreas))
            {
                // KIỂM TRA CHÉO: Tính khoảng cách từ điểm chạm đất đến điểm NavMesh
                // Nếu xa hơn 2 mét, chứng tỏ nó đang cố gắng "hút" ra rìa -> Huỷ bỏ
                if (Vector3.Distance(hitInfo.point, navHit.position) < 2f)
                {
                    int randomIndex = Random.Range(0, prefabsToSpawn.Count);
                    NetworkObject prefab = prefabsToSpawn[randomIndex];

                    if (prefab != null)
                    {
                        NetworkObject spawnedObj = Runner.Spawn(prefab, navHit.position, Quaternion.identity);
                        spawnedEntities.Add(spawnedObj);
                    }
                }
            }
        }
    }

    // =========================================================================
    // HÀM VẼ GIZMOS ĐỂ DỄ QUAN SÁT KHU VỰC SPAWN TRONG EDITOR
    // =========================================================================
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Màu xanh lá trong suốt
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}