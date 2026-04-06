using Fusion;
using UnityEngine;

public enum FarmTileState { Untilled, Tilled, Planted, ReadyToHarvest }

public class FarmTile : NetworkBehaviour
{
    // --- NETWORKED VARIABLES ---
    [Networked, OnChangedRender(nameof(UpdateVisuals))]
    public FarmTileState State { get; set; }

    [Networked] public TickTimer GrowTimer { get; set; }

    // Lưu ID của hạt giống để lấy data khi thu hoạch và render prefab
    [Networked] public NetworkString<_32> PlantedSeedID { get; set; }

    // --- VISUAL REFERENCES ---
    [Header("Visuals")]
    [SerializeField] private MeshRenderer _tileRenderer;
    [SerializeField] private Material _untilledMat;
    [SerializeField] private Material _tilledMat;

    private GameObject _currentPlantVisual; // Cache cây đang hiển thị

    // Gọi trên Server để check thời gian lớn của cây
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (State == FarmTileState.Planted && GrowTimer.Expired(Runner))
        {
            State = FarmTileState.ReadyToHarvest;
        }
    }

    // --- LOGIC TƯƠNG TÁC (CHỈ GỌI TỪ SERVER/STATE AUTHORITY) ---
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_InteractTile(PlayerRef player, int toolIndex, string seedIdToPlant = "")
    {
        // 1. Cày đất (Cần cuốc = toolIndex 1)
        if (State == FarmTileState.Untilled && toolIndex == 1)
        {
            State = FarmTileState.Tilled;
            return;
        }

        // 2. Trồng cây (Cần hạt giống = toolIndex 4)
        if (State == FarmTileState.Tilled && toolIndex == 4 && !string.IsNullOrEmpty(seedIdToPlant))
        {
            // TODO: Trừ hạt giống trong TuiDo của Player_Controller tại đây
            // Player_Controller.Get(player).RemoveItem(seedIdToPlant, 1);

            PlantedSeedID = seedIdToPlant;
            State = FarmTileState.Planted;

            // Lấy data cây để set timer
            SO_SeedData seedData = SeedDatabase.GetSeedData(seedIdToPlant);
            if (seedData != null)
            {
                GrowTimer = TickTimer.CreateFromSeconds(Runner, seedData.GrowTimeSeconds);
            }
            return;
        }

        // 3. Thu hoạch (Dùng tay không = toolIndex 0)
        if (State == FarmTileState.ReadyToHarvest && toolIndex == 0)
        {
            SO_SeedData seedData = SeedDatabase.GetSeedData(PlantedSeedID.ToString());
            if (seedData != null)
            {
                // TODO: Add sản phẩm vào TuiDo của Player_Controller tại đây
                // Player_Controller.Get(player).AddItem(seedData.HarvestItemID, seedData.HarvestYield);
            }

            // Reset ô đất
            PlantedSeedID = "";
            State = FarmTileState.Tilled; // Reset về đất cày
        }
    }

    // --- VISUAL UPDATE (GỌI TRÊN MỌI CLIENT KHI STATE THAY ĐỔI) ---
    private void UpdateVisuals()
    {
        // Xóa cây cũ nếu có
        if (_currentPlantVisual != null)
        {
            Destroy(_currentPlantVisual);
        }

        // Cập nhật chất liệu đất
        _tileRenderer.material = (State == FarmTileState.Untilled) ? _untilledMat : _tilledMat;

        // Cập nhật Prefab cây nếu đang trồng hoặc thu hoạch
        if (State == FarmTileState.Planted || State == FarmTileState.ReadyToHarvest)
        {
            SO_SeedData seedData = SeedDatabase.GetSeedData(PlantedSeedID.ToString());
            if (seedData != null)
            {
                GameObject prefabToSpawn = (State == FarmTileState.Planted) ? seedData.SeedlingPrefab : seedData.MaturePrefab;
                if (prefabToSpawn != null)
                {
                    _currentPlantVisual = Instantiate(prefabToSpawn, transform.position, Quaternion.identity, transform);
                }
            }
        }
    }
}