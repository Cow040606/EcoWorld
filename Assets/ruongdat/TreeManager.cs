using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TreeManager : NetworkBehaviour
{
    public static TreeManager Instance;
    public NetworkPrefabRef woodPrefab;
    public float chopRadius = 3.0f;

    // Lưu trữ dữ liệu gốc của các Terrain để phục hồi khi tắt game (tránh mất cây vĩnh viễn trong file Asset)
    private Dictionary<Terrain, TreeInstance[]> originalTrees = new Dictionary<Terrain, TreeInstance[]>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Backup toàn bộ cây của mọi Terrain đang có trong Scene
        foreach (Terrain t in Terrain.activeTerrains)
        {
            originalTrees[t] = t.terrainData.treeInstances;
        }
    }

    private void OnApplicationQuit()
    {
        // Trả lại toàn bộ cây cho Terrain khi thoát game để bảo vệ dữ liệu Asset gốc
        foreach (var kvp in originalTrees)
        {
            if (kvp.Key != null)
            {
                kvp.Key.terrainData.treeInstances = kvp.Value;
            }
        }
    }

    public void TryChopTree(Terrain targetTerrain, Vector3 hitPoint, NetworkRunner runner)
    {
        int treeIndex = GetClosestTreeIndexOnTerrain(targetTerrain, hitPoint);
        
        if (treeIndex < 0) return;

        Debug.Log($"[TreeManager] ✅ Gửi yêu cầu chặt cây #{treeIndex} lên Server...");
        // Chỉ người chơi (Input Authority) gọi lệnh này lên Server
        RPC_RequestChopTree(targetTerrain.transform.position, treeIndex, hitPoint, runner.LocalPlayer);
    }

    // 1. Client yêu cầu Server xử lý chặt cây
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestChopTree(Vector3 terrainPosition, int treeIndex, Vector3 hitPoint, PlayerRef chopper)
    {
        Terrain targetTerrain = GetTerrainByPosition(terrainPosition);
        if (targetTerrain == null) return;

        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;
        if (treeIndex < 0 || treeIndex >= trees.Length) return;

        // Tính toán vị trí sinh ra gỗ (cộng thêm 1.5f trục Y để rớt từ trên xuống)
        Vector3 spawnPos = TreeToWorld(targetTerrain, trees[treeIndex]) + Vector3.up * 1.5f;

        // Sinh gỗ rớt ra (Chỉ Server thực hiện để tránh đẻ ra nhiều cục gỗ trùng lặp)
        if (woodPrefab.IsValid)
            Runner.Spawn(woodPrefab, spawnPos, Quaternion.identity, chopper);

        // 2. Server ra lệnh cho TẤT CẢ các máy (bao gồm cả nó) xóa cây đó đi
        RPC_SyncRemoveTree(terrainPosition, treeIndex);
    }

    // 3. Tất cả các máy cùng đồng bộ việc xóa cây
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncRemoveTree(Vector3 terrainPosition, int treeIndex)
    {
        Terrain targetTerrain = GetTerrainByPosition(terrainPosition);
        if (targetTerrain == null) 
        {
            Debug.LogError($"[TreeManager] ❌ Máy Client không tìm thấy Terrain tại {terrainPosition} để xóa cây!");
            return;
        }

        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;
        if (treeIndex < 0 || treeIndex >= trees.Length) return;

        // Tạo mảng mới và loại bỏ cây bị chặt
        List<TreeInstance> treeList = new List<TreeInstance>(trees);
        treeList.RemoveAt(treeIndex);
        
        // Gán lại mảng cây mới cho Terrain
        targetTerrain.terrainData.treeInstances = treeList.ToArray();

        // --- CỰC KỲ QUAN TRỌNG ---
        // BƯỚC 1: CẬP NHẬT ĐỒ HỌA: Ép Unity xóa hình ảnh cái cây
        targetTerrain.terrainData.SetTreeInstances(treeList.ToArray(), false); 
        targetTerrain.Flush(); 

        // BƯỚC 2: CẬP NHẬT VẬT LÝ (FIX LỖI KẸT COLLIDER): 
        // Tắt và bật lại TerrainCollider để ép Unity dọn dẹp va chạm của cây cũ
        TerrainCollider terrainCollider = targetTerrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.enabled = false;
            terrainCollider.enabled = true;
        }

        Debug.Log($"[TreeManager] 🌲 Đã xóa cây #{treeIndex} và dọn sạch Collider trên máy tính này!");
    }

    private int GetClosestTreeIndexOnTerrain(Terrain targetTerrain, Vector3 worldPos)
    {
        int closestIndex = -1;
        float closestDist = float.MaxValue;
        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 treeWorld = TreeToWorld(targetTerrain, trees[i]);

            // Chỉ so sánh mặt phẳng XZ (bỏ qua độ cao Y để check bán kính chính xác hơn)
            float dist = Vector2.Distance(
                new Vector2(worldPos.x, worldPos.z),
                new Vector2(treeWorld.x, treeWorld.z)
            );

            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        return closestDist <= chopRadius ? closestIndex : -1;
    }

    // Hàm tiện ích: Tính tọa độ World của một cây dựa trên Terrain chứa nó
    private Vector3 TreeToWorld(Terrain terrain, TreeInstance tree)
    {
        TerrainData td = terrain.terrainData;
        Vector3 tPos = terrain.transform.position;
        return new Vector3(
            tPos.x + tree.position.x * td.size.x,
            tPos.y + tree.position.y * td.size.y,
            tPos.z + tree.position.z * td.size.z
        );
    }

    // Hàm tiện ích: Tìm Terrain dựa trên tọa độ (Khắc phục lỗi sai số thập phân qua mạng)
    private Terrain GetTerrainByPosition(Vector3 position)
    {
        Terrain closestTerrain = null;
        float minDistance = float.MaxValue;

        foreach (Terrain t in Terrain.activeTerrains)
        {
            float dist = Vector3.Distance(t.transform.position, position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestTerrain = t;
            }
        }

        // Nếu khoảng cách sai số dưới 5m thì chấp nhận đó chính là Terrain cần tìm
        if (minDistance <= 5.0f) return closestTerrain;
        
        return null;
    }
}