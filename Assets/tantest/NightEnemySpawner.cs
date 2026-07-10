using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class NightEnemySpawner : NetworkBehaviour
{
    [Header("Liên Kết Thời Gian")]
    [Tooltip("Kéo GameObject chứa script TimeManager vào đây")]
    public TimeManager timeManager;

    [Header("Cấu hình Spawn")]
    public NetworkPrefabRef enemyPrefab;
    public Transform[] spawnPoints;
    public int maxEnemies = 5;

    private List<NetworkObject> spawnedEnemies = new List<NetworkObject>();
    private bool isNightSpawned = false;

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return; // Chỉ Server/Host mới có quyền kiểm soát việc sinh tồn của quái

        if (timeManager == null)
        {
            Debug.LogWarning("❌ [NightEnemySpawner] Chưa gắn TimeManager vào Inspector!");
            return;
        }

        // Gọi hàm GetCurrentHour() bên TimeManager
        float currentHour = timeManager.GetCurrentHour();

        // Định nghĩa trời tối: Từ 22h đêm đến trước 5h sáng
        bool isNight = (currentHour >= 22f || currentHour < 5f);

        // Chu kỳ 1: Chuyển giao từ Ngày sang Đêm -> Spawn quái
        if (isNight && !isNightSpawned)
        {
            SpawnEnemies();
            isNightSpawned = true;
        }
        // Chu kỳ 2: Chuyển giao từ Đêm sang Ngày -> Xóa sạch quái
        else if (!isNight && isNightSpawned)
        {
            DespawnAllEnemies();
            isNightSpawned = false;
        }
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < maxEnemies; i++)
        {
            if (spawnPoints.Length == 0) break;

            // Lấy ngẫu nhiên 1 điểm trong danh sách SpawnPoints
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            NetworkObject enemy = Runner.Spawn(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
            }
        }
        Debug.Log($"<color=purple>Đã 22h tối! Bắt đầu Spawn {spawnedEnemies.Count} quái vật.</color>");
    }

    private void DespawnAllEnemies()
    {
        // 1. Lọc bỏ khỏi danh sách những con quái đã bị người chơi giết (Object đã bị xóa = null)
        spawnedEnemies.RemoveAll(item => item == null);

        // 2. Tiêu diệt những con quái còn sống sót khi trời sáng
        foreach (var enemy in spawnedEnemies)
        {
            if (enemy.IsValid)
            {
                Runner.Despawn(enemy);
            }
        }

        spawnedEnemies.Clear();
        Debug.Log("<color=orange>Đã 5h sáng! Dọn dẹp sạch sẽ quái vật đêm.</color>");
    }
}