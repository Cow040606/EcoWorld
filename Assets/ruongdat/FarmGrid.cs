using Fusion;
using UnityEngine;

public class FarmGrid : NetworkBehaviour
{
    [Header("Cài Đặt Lưới Đất")]
    [Tooltip("Kéo Prefab của 1 Ô ĐẤT NHỎ vào đây")]
    public NetworkPrefabRef farmTilePrefab; 
    
    public int columns = 5; // Số ô theo chiều ngang (Trục X)
    public int rows = 5;    // Số ô theo chiều dọc (Trục Z)
    public float tileSize = 1f; // Kích thước 1 ô (VD: 1 mét)

    public override void Spawned()
    {
        // CHỈ SERVER mới có quyền Spawn các vật thể mạng ra thế giới
        if (HasStateAuthority)
        {
            SpawnGrid();
        }
    }

    private void SpawnGrid()
    {
        // Điểm bắt đầu rải đất chính là vị trí của GameObject chứa script này
        Vector3 startPos = transform.position;

        for (int x = 0; x < columns; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                // Tính tọa độ cho từng ô nhỏ xếp cạnh nhau
                Vector3 tilePosition = startPos + new Vector3(x * tileSize, 0, z * tileSize);
                
                // Server gọi lệnh Spawn. Fusion sẽ tự động tạo ô đất này trên máy mọi người chơi khác!
                NetworkObject spawnedTile = Runner.Spawn(farmTilePrefab, tilePosition, Quaternion.identity);
                
                // Gom các ô đất làm con của FarmZone cho Hierarchy gọn gàng
                spawnedTile.transform.SetParent(transform);
            }
        }
    }
}