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

    // --- SỬA Ở ĐÂY: Gắn trực tiếp 2 Prefab vào đây để khỏi cần gọi Database ---
    [Header("Plant Models (Kéo Prefab vào đây)")]
    public GameObject m_SeedlingPrefab; // Kéo model cây mầm vào đây
    public GameObject m_MaturePrefab;   // Kéo model cây to vào đây
    // --------------------------------------------------------------------------

    private GameObject _currentPlantVisual;

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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_InteractTile(PlayerRef player, int toolIndex, int seedIdToPlant = 0)
    {
        // 1. Cày đất
        if (State == FarmTileState.Untilled && toolIndex == 1)
        {
            State = FarmTileState.Tilled;
            return;
        }

        // 2. Trồng cây
        if (State == FarmTileState.Tilled && toolIndex == 3 && seedIdToPlant == 101)
        {
            // TẠM THỜI BYPASS TÚI ĐỒ (Luôn cho trồng để test mọc cây trước)
            bool coHatGiong = true; 
            
            if (coHatGiong)
            {
                Debug.Log("[Server] Trồng thành công! Bắt đầu đếm 5 giây...");
                PlantedSeedID = seedIdToPlant;
                State = FarmTileState.Planted;
                
                // Đặt thẳng 5 giây không cần hỏi Database
                GrowTimer = TickTimer.CreateFromSeconds(Runner, 5f); 
            }
            return;
        }

        // 3. Thu hoạch
        if (State == FarmTileState.ReadyToHarvest && toolIndex == 0)
        {
            Debug.Log("[Server] Thu hoạch thành công!");
            PlantedSeedID = 0;
            State = FarmTileState.Tilled; 
        }
    }

    private void UpdateVisuals()
    {
        if (_currentPlantVisual != null) Destroy(_currentPlantVisual);
        
        if (_tileRenderer != null) 
            _tileRenderer.material = (State == FarmTileState.Untilled) ? _untilledMat : _tilledMat;

        // Sinh hình ảnh cây dựa trên 2 biến Prefab vừa khai báo ở trên
        if (State == FarmTileState.Planted && m_SeedlingPrefab != null)
        {
            _currentPlantVisual = Instantiate(m_SeedlingPrefab, transform.position, Quaternion.identity, transform);
        }
        else if (State == FarmTileState.ReadyToHarvest && m_MaturePrefab != null)
        {
            _currentPlantVisual = Instantiate(m_MaturePrefab, transform.position, Quaternion.identity, transform);
        }
    }
}