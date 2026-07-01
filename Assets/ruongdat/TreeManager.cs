using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TreeManager : NetworkBehaviour
{
    public static TreeManager Instance;

    [Header("Prefabs & Hiệu ứng")]
    public NetworkPrefabRef woodPrefab;
    [Tooltip("Kéo Prefab Particle dăm gỗ vào đây")]
    public GameObject woodChipParticlePrefab;

    [Header("Thông số cấu hình")]
    public float chopRadius = 3.0f;

    // Lưu trữ số lần bị chém của từng cây.
    private Dictionary<string, int> treeHitCounts = new Dictionary<string, int>();

    // Lưu trữ dữ liệu gốc của các Terrain để phục hồi khi tắt game
    private Dictionary<Terrain, TreeInstance[]> originalTrees = new Dictionary<Terrain, TreeInstance[]>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (Terrain t in Terrain.activeTerrains)
        {
            originalTrees[t] = t.terrainData.treeInstances;
        }
    }

    private void OnApplicationQuit()
    {
        foreach (var kvp in originalTrees)
        {
            if (kvp.Key != null)
                kvp.Key.terrainData.treeInstances = kvp.Value;
        }
    }

    public void TryChopTree(Terrain targetTerrain, Vector3 hitPoint, NetworkRunner runner)
    {
        int treeIndex = GetClosestTreeIndexOnTerrain(targetTerrain, hitPoint);
        if (treeIndex < 0) return;

        // Chỉ Input Authority gọi lên Server
        RPC_RequestChopTree(targetTerrain.transform.position, treeIndex, hitPoint, runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestChopTree(Vector3 terrainPosition, int treeIndex, Vector3 hitPoint, PlayerRef chopper)
    {
        Terrain targetTerrain = GetTerrainByPosition(terrainPosition);
        if (targetTerrain == null) return;

        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;
        if (treeIndex < 0 || treeIndex >= trees.Length) return;

        string treeKey = $"{Mathf.RoundToInt(terrainPosition.x)}_{Mathf.RoundToInt(terrainPosition.z)}_{treeIndex}";

        if (!treeHitCounts.ContainsKey(treeKey))
        {
            treeHitCounts[treeKey] = 0;
        }

        // 1. Tăng máu/hit
        treeHitCounts[treeKey]++;

        // 2. Yêu cầu tất cả các máy khách phát hiệu ứng dăm gỗ tại tọa độ chém trúng
        RPC_PlayTreeHitEffect(hitPoint);

        // 3. Nếu đủ 3 hit thì cây gãy
        if (treeHitCounts[treeKey] >= 3)
        {
            Vector3 spawnPos = TreeToWorld(targetTerrain, trees[treeIndex]) + Vector3.up * 1.5f;

            if (woodPrefab.IsValid)
                Runner.Spawn(woodPrefab, spawnPos, Quaternion.identity, chopper);

            RPC_SyncRemoveTree(terrainPosition, treeIndex);
            treeHitCounts.Remove(treeKey);
        }
    }

    // GỌI HIỆU ỨNG CHO TOÀN BỘ CÁC MÁY TRONG MẠNG
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayTreeHitEffect(Vector3 hitPoint)
    {
        // Sinh ra hiệu ứng dăm gỗ văng ra
        if (woodChipParticlePrefab != null)
        {
            GameObject vfx = Instantiate(woodChipParticlePrefab, hitPoint, Quaternion.identity);
            Destroy(vfx, 1.5f); // Tự động dọn rác sau 1.5s để tránh nặng máy
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SyncRemoveTree(Vector3 terrainPosition, int treeIndex)
    {
        Terrain targetTerrain = GetTerrainByPosition(terrainPosition);
        if (targetTerrain == null) return;

        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;
        if (treeIndex < 0 || treeIndex >= trees.Length) return;

        List<TreeInstance> treeList = new List<TreeInstance>(trees);
        treeList.RemoveAt(treeIndex);

        targetTerrain.terrainData.treeInstances = treeList.ToArray();
        targetTerrain.terrainData.SetTreeInstances(treeList.ToArray(), false);
        targetTerrain.Flush();

        TerrainCollider terrainCollider = targetTerrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.enabled = false;
            terrainCollider.enabled = true;
        }
    }

    private int GetClosestTreeIndexOnTerrain(Terrain targetTerrain, Vector3 worldPos)
    {
        int closestIndex = -1;
        float closestDist = float.MaxValue;
        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 treeWorld = TreeToWorld(targetTerrain, trees[i]);
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
        if (minDistance <= 5.0f) return closestTerrain;
        return null;
    }
}