using Fusion;
using UnityEngine;

public class VillagerSpawner : NetworkBehaviour
{
    [Header("References")]
    public NetworkPrefabRef villagerPrefab;
    public Transform spawnPoint; // Ô này bạn đã kéo "Spawn (Transform)" vào trong ảnh 1

    [Header("Settings")]
    public Vector3 exitOffset = new Vector3(0, 0, 5); // Khoảng cách đi ra sau khi spawn

    public void SpawnVillager()
    {
        // Chỉ thực hiện nếu Network đang chạy
        if (Runner != null && Runner.IsRunning)
        {
            // Lấy vị trí từ Spawn Point cố định
            Vector3 pos = spawnPoint.position;
            Quaternion rot = spawnPoint.rotation;

            Debug.Log($"<color=cyan>Spawner:</color> Đang tạo dân làng tại {pos}");

            // Thực hiện Spawn
            NetworkObject obj = Runner.Spawn(villagerPrefab, pos, rot, Runner.LocalPlayer);

            // Ra lệnh cho dân làng đi ra khỏi điểm spawn ngay lập tức
            if (obj != null)
            {
                var ai = obj.GetComponent<VillagerAI>();
                if (ai != null)
                {
                    ai.MoveToInitialPosition(pos + exitOffset);
                }
            }
        }
    }

    private void Update()
    {
        // Nhấn phím K để test
        if (Input.GetKeyDown(KeyCode.K))
        {
            SpawnVillager();
        }
    }
}