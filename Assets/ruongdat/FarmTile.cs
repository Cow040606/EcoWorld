using Fusion;
using UnityEngine;

public enum FarmTileState { Untilled, Tilled, Planted, ReadyToHarvest }

public class FarmTile : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(UpdateVisuals))]
    public FarmTileState State { get; set; }

    [Networked] public TickTimer GrowTimer { get; set; }
    [Networked] public int PlantedSeedID { get; set; }

    [Header("Visuals")]
    [SerializeField] private MeshRenderer _tileRenderer;
    [SerializeField] private Material _untilledMat;
    [SerializeField] private Material _tilledMat;

    private GameObject _currentPlantVisual;

    // THÊM HÀM NÀY: Để ô đất cập nhật màu ngay khi vừa xuất hiện trong game
    public override void Spawned()
    {
        UpdateVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        if (State == FarmTileState.Planted && GrowTimer.Expired(Runner))
        {
            State = FarmTileState.ReadyToHarvest;
        }
    }

    // SỬA Ở ĐÂY: Đổi RpcSources.InputAuthority thành RpcSources.All 
    // Để bất kỳ ai đi ngang qua cũng có thể click vào ô đất này
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_InteractTile(PlayerRef player, int toolIndex, int seedIdToPlant = 0)
    {
        if (State == FarmTileState.Untilled && toolIndex == 1)
        {
            State = FarmTileState.Tilled;
            return;
        }

        if (State == FarmTileState.Tilled && toolIndex == 4 && seedIdToPlant != 0)
        {
            if (TryRemoveSeedFromPlayer(player, seedIdToPlant))
            {
                PlantedSeedID = seedIdToPlant;
                State = FarmTileState.Planted;
                SO_SeedData seedData = GlobalSeedDatabase.GetSeed(seedIdToPlant);
                if (seedData != null) GrowTimer = TickTimer.CreateFromSeconds(Runner, seedData.GrowTimeSeconds);
            }
            return;
        }

        if (State == FarmTileState.ReadyToHarvest && toolIndex == 0)
        {
            SO_SeedData seedData = GlobalSeedDatabase.GetSeed(PlantedSeedID);
            if (seedData != null) AddCropToPlayer(player, seedData.HarvestItemID, seedData.HarvestYield);
            PlantedSeedID = 0;
            State = FarmTileState.Tilled; 
        }
    }

    private bool TryRemoveSeedFromPlayer(PlayerRef playerRef, int seedID)
    {
        var player = Runner.GetPlayerObject(playerRef).GetComponent<Player_Controller>();
        if (player == null) return false;

        for (int i = 0; i < player.TuiDo.Length; i++)
        {
            if (player.TuiDo[i].ItemID == seedID && player.TuiDo[i].SoLuong > 0)
            {
                var item = player.TuiDo[i];
                item.SoLuong--;
                if (item.SoLuong <= 0) item.ItemID = 0;
                player.TuiDo.Set(i, item); 
                return true;
            }
        }
        return false;
    }

    private void AddCropToPlayer(PlayerRef playerRef, int cropID, int amount)
    {
        var player = Runner.GetPlayerObject(playerRef).GetComponent<Player_Controller>();
        if (player != null)
        {
            // Tạm thời log ra, bạn có thể nối với hàm nhặt đồ sau
            Debug.Log($"[Server] Đã thu hoạch {amount} vật phẩm {cropID}");
        }
    }

    private void UpdateVisuals()
    {
        if (_currentPlantVisual != null) Destroy(_currentPlantVisual);
        
        if (_tileRenderer != null) 
            _tileRenderer.material = (State == FarmTileState.Untilled) ? _untilledMat : _tilledMat;

        if (State == FarmTileState.Planted || State == FarmTileState.ReadyToHarvest)
        {
            SO_SeedData seedData = GlobalSeedDatabase.GetSeed(PlantedSeedID);
            if (seedData != null)
            {
                GameObject prefab = (State == FarmTileState.Planted) ? seedData.SeedlingPrefab : seedData.MaturePrefab;
                if (prefab != null) _currentPlantVisual = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            }
        }
    }
}