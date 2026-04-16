using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TreeManager : NetworkBehaviour
{
    public static TreeManager Instance;

    public Terrain activeTerrain;
    public NetworkPrefabRef woodPrefab; // Kéo prefab gỗ vào đây
    
    private TreeInstance[] originalTrees; // Lưu lại cây gốc để reset
    private TerrainData terrainData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (activeTerrain == null) activeTerrain = Terrain.activeTerrain;
        terrainData = activeTerrain.terrainData;

        // Lưu lại dữ liệu cây gốc
        originalTrees = terrainData.treeInstances;
    }

    private void OnDestroy()
    {
        // Khôi phục lại cây khi thoát game (để không bị mất trong Editor)
        if (terrainData != null && originalTrees != null)
        {
            terrainData.treeInstances = originalTrees;
        }
    }

    // Hàm này được gọi từ Player_Controller khi click trúng cây
    public void TryChopTree(Vector3 hitPoint, NetworkRunner runner)
    {
        int treeIndex = GetClosestTreeIndex(hitPoint);
        if (treeIndex != -1)
        {
            // Gửi RPC tới tất cả mọi người để đồng bộ việc chặt cây
            // Truyền theo thông tin người chặt (runner.LocalPlayer)
            RPC_ChopTreeSync(treeIndex, runner.LocalPlayer);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ChopTreeSync(int treeIndex, PlayerRef chopper)
    {
        // 1. Lấy mảng cây hiện tại, chuyển thành List để dễ thao tác
        List<TreeInstance> trees = new List<TreeInstance>(terrainData.treeInstances);
        
        // Cẩn thận out of bounds nếu 2 người chặt cùng 1 lúc
        if (treeIndex < 0 || treeIndex >= trees.Count) return;

        // Lấy vị trí thế giới của cây trước khi xóa (để spawn gỗ)
        Vector3 treeLocalPos = trees[treeIndex].position;
        Vector3 treeWorldPos = Vector3.Scale(treeLocalPos, terrainData.size) + activeTerrain.transform.position;

        // 2. Xóa cây khỏi danh sách và cập nhật lại Terrain
        trees.RemoveAt(treeIndex);
        terrainData.treeInstances = trees.ToArray();

        // 3. Spawn khối gỗ (CHỈ người chặt - StateAuthority - mới được spawn để tránh duplicate)
        if (Runner.LocalPlayer == chopper)
        {
            // Spawn khúc gỗ cao hơn mặt đất 1 chút
            Vector3 spawnPos = treeWorldPos + Vector3.up * 1.5f;
            Runner.Spawn(woodPrefab, spawnPos, Quaternion.identity, chopper);
        }
    }

    // Thuật toán tìm Index của cây gần với điểm Raycast Hit nhất
    private int GetClosestTreeIndex(Vector3 hitPoint)
    {
        TreeInstance[] trees = terrainData.treeInstances;
        int closestIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < trees.Length; i++)
        {
            // Chuyển vị trí local của cây sang world position
            Vector3 treeWorldPos = Vector3.Scale(trees[i].position, terrainData.size) + activeTerrain.transform.position;
            
            // Tính khoảng cách từ điểm bắn trúng đến gốc cây
            // Ép Y về 0 để bỏ qua độ cao, chỉ đo khoảng cách theo mặt phẳng ngang XZ
            Vector3 treePosXZ = new Vector3(treeWorldPos.x, 0, treeWorldPos.z);
            Vector3 hitPosXZ = new Vector3(hitPoint.x, 0, hitPoint.z);
            
            float distance = Vector3.Distance(treePosXZ, hitPosXZ);

            // Bán kính sai số khoảng 2 đơn vị (tùy độ to của thân cây)
            if (distance < minDistance && distance < 2.0f) 
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}