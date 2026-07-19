using System.Collections.Generic;
using UnityEngine;
using Fusion;

[System.Serializable]
public struct SpawnZone
{
    public string zoneName;
    public Vector3 centerPos;
    public float radius;

    [Tooltip("Kéo Prefab của loại cây muốn rải vào đây")]
    public NetworkPrefabRef treePrefab;

    public int amount;

    [Range(0, 90)]
    public float maxSlope;
}

public class TreeSpawner : NetworkBehaviour
{
    [Header("Cấu Hình Vùng Rải Cây")]
    public List<SpawnZone> treeZones = new List<SpawnZone>();

    [Header("Cấu Hình Địa Hình")]
    public float castHeight = 200f;
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;
    public float checkRadius = 1.5f;

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            GenerateTrees();
        }
    }

    private void GenerateTrees()
    {
        foreach (var zone in treeZones)
        {
            int spawnedCount = 0;

            // Đã tăng số lần thử lên 20 lần để rải cây dễ trúng hơn
            int maxAttempts = zone.amount * 20;
            int attempts = 0;

            // BỘ ĐẾM LỖI ĐỂ TÌM NGUYÊN NHÂN
            int loiTiaLaser = 0;
            int loiDoDoc = 0;
            int loiVatCan = 0;

            while (spawnedCount < zone.amount && attempts < maxAttempts)
            {
                attempts++;

                Vector2 randCircle = Random.insideUnitCircle * zone.radius;
                Vector3 rayStartPos = zone.centerPos + new Vector3(randCircle.x, castHeight, randCircle.y);

                if (Physics.Raycast(rayStartPos, Vector3.down, out RaycastHit hit, castHeight * 2f, groundLayer))
                {
                    float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                    if (slopeAngle > zone.maxSlope)
                    {
                        loiDoDoc++;
                        continue;
                    }

                    if (!Physics.CheckSphere(hit.point, checkRadius, obstacleLayer))
                    {
                        Runner.Spawn(zone.treePrefab, hit.point, Quaternion.identity);
                        spawnedCount++;
                    }
                    else
                    {
                        loiVatCan++;
                    }
                }
                else
                {
                    loiTiaLaser++;
                }
            }

            // In báo cáo chi tiết ra Console
            Debug.Log($"<color=cyan>[TreeSpawner]</color> Vùng {zone.zoneName}: Rải được {spawnedCount}/{zone.amount} cây. \n<color=yellow>Báo cáo {attempts} lần thử nghiệm bị từ chối do:</color> Bắn trượt mặt đất ({loiTiaLaser}), Đất quá dốc ({loiDoDoc}), Vướng vật cản ({loiVatCan}).");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        foreach (var zone in treeZones)
        {
            Gizmos.DrawWireSphere(zone.centerPos, zone.radius);
        }
    }
}