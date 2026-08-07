using System.Collections.Generic;
using UnityEngine;
using Fusion;

[System.Serializable]
public struct TreeZone
{
    public string zoneName;
    public Vector3 centerPos;
    public float radius;
    public int prototypeIndex;
    public int amount;
    [Range(0, 90)]
    public float maxSlope;
}

public class TreeManager : NetworkBehaviour
{
    public static TreeManager Instance;

    [Header("Cấu Hình Vật Phẩm Rớt")]
    [Tooltip("Chỉ cần kéo 1 Prefab Gỗ duy nhất vào đây, cây nào chặt cũng rớt ra cái này")]
    public NetworkPrefabRef woodPrefab;
    public float chopRadius = 3.0f;

    [Header("Thuật Toán Sinh Cây Theo Vùng")]
    public bool generateOnStart = true;
    public List<TreeZone> treeZones = new List<TreeZone>();

    [Networked] public int MapSeed { get; set; }
    private ChangeDetector _changeDetector;

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
            if (kvp.Key != null) kvp.Key.terrainData.treeInstances = kvp.Value;
        }
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority && generateOnStart)
        {
            MapSeed = Random.Range(1, 999999);
        }

        if (MapSeed != 0)
        {
            GenerateTreesLocally(MapSeed);
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(MapSeed) && MapSeed != 0)
            {
                GenerateTreesLocally(MapSeed);
            }
        }
    }

    #region THUẬT TOÁN SINH CÂY THÔNG MINH
    private void GenerateTreesLocally(int seed)
    {
        Random.InitState(seed);

        foreach (Terrain t in Terrain.activeTerrains)
        {
            List<TreeInstance> newTrees = new List<TreeInstance>(originalTrees[t]);
            TerrainData tData = t.terrainData;

            foreach (var zone in treeZones)
            {
                int spawned = 0;
                int attempts = 0;

                while (spawned < zone.amount && attempts < zone.amount * 3)
                {
                    attempts++;

                    Vector2 randCircle = Random.insideUnitCircle * zone.radius;
                    Vector3 worldPos = zone.centerPos + new Vector3(randCircle.x, 0, randCircle.y);

                    Vector3 localPos = t.transform.InverseTransformPoint(worldPos);
                    Vector3 normPos = new Vector3(localPos.x / tData.size.x, 0, localPos.z / tData.size.z);

                    if (normPos.x < 0 || normPos.x > 1 || normPos.z < 0 || normPos.z > 1) continue;

                    float steepness = tData.GetSteepness(normPos.x, normPos.z);
                    if (steepness > zone.maxSlope) continue;

                    normPos.y = tData.GetInterpolatedHeight(normPos.x, normPos.z) / tData.size.y;

                    TreeInstance tree = new TreeInstance();
                    tree.position = normPos;
                    tree.prototypeIndex = zone.prototypeIndex;
                    tree.widthScale = 1f;
                    tree.heightScale = 1f;
                    tree.color = Color.white;
                    tree.lightmapColor = Color.white;

                    newTrees.Add(tree);
                    spawned++;
                }
            }

            tData.SetTreeInstances(newTrees.ToArray(), false);
            t.Flush();

            TerrainCollider tc = t.GetComponent<TerrainCollider>();
            if (tc != null)
            {
                tc.enabled = false;
                tc.enabled = true;
            }
        }
    }
    #endregion

    #region HỆ THỐNG CHẶT VÀ RỚT ĐỒ CHUNG 1 LOẠI
    public void TryChopTree(Terrain targetTerrain, Vector3 hitPoint, NetworkRunner runner)
    {
        int treeIndex = GetClosestTreeIndexOnTerrain(targetTerrain, hitPoint);
        if (treeIndex < 0) return;

        RPC_RequestChopTree(targetTerrain.transform.position, treeIndex, hitPoint, runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestChopTree(Vector3 terrainPosition, int treeIndex, Vector3 hitPoint, PlayerRef chopper)
    {
        Terrain targetTerrain = GetTerrainByPosition(terrainPosition);
        if (targetTerrain == null) return;

        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;
        if (treeIndex < 0 || treeIndex >= trees.Length) return;

        // Vị trí rớt gỗ chỉ cách gốc 1.5f để không bị rớt từ trên trời xuống
        Vector3 spawnPos = TreeToWorld(targetTerrain, trees[treeIndex]) + Vector3.up * 1.5f;
        // Thêm một chút xê dịch ngẫu nhiên để rớt tự nhiên hơn
        spawnPos += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));

        if (woodPrefab.IsValid)
        {
            Runner.Spawn(woodPrefab, spawnPos, Quaternion.identity, chopper);
        }

        RPC_SyncRemoveTree(terrainPosition, treeIndex);
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

        TerrainCollider tc = targetTerrain.GetComponent<TerrainCollider>();
        if (tc != null)
        {
            tc.enabled = false;
            tc.enabled = true;
        }
    }
    #endregion

    #region HÀM TIỆN ÍCH
    private int GetClosestTreeIndexOnTerrain(Terrain targetTerrain, Vector3 worldPos)
    {
        int closestIndex = -1;
        float closestDist = float.MaxValue;
        TreeInstance[] trees = targetTerrain.terrainData.treeInstances;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 treeWorld = TreeToWorld(targetTerrain, trees[i]);
            float dist = Vector2.Distance(new Vector2(worldPos.x, worldPos.z), new Vector2(treeWorld.x, treeWorld.z));

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
        return minDistance <= 5.0f ? closestTerrain : null;
    }
    #endregion
}