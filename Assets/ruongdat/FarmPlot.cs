using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class FarmPlot : NetworkBehaviour
{
    // Chỉ còn 3 trạng thái: Đất trống -> Cây non -> Cây lớn
    public enum PlotState { DatTrong, CayCon, CayLon }

    [Networked, OnChangedRender(nameof(OnStateChanged))]
    public PlotState CurrentState { get; set; }

    [Networked]
    public TickTimer growTimer { get; set; }

    [Header("--- Models (Kéo Prefab từ thư mục Project vào đây) ---")]
    [SerializeField] private GameObject modelDatThuong;
    [SerializeField] private GameObject modelCayCon;
    [SerializeField] private GameObject modelCayLon;

    [Header("--- Điểm Neo (Vị trí mọc cây) ---")]
    [SerializeField] private Transform diemGieoHat; 

    [Header("--- Farm Settings ---")]
    [SerializeField] private int fruitItemID = 3;
    [SerializeField] private int harvestCount = 3;
    [SerializeField] private float growTime = 10f; 

    private List<GameObject> spawnedVisuals = new List<GameObject>();

    public override void Spawned()
    {
        MeshRenderer rootMesh = GetComponent<MeshRenderer>();
        if (rootMesh != null) rootMesh.enabled = false;

        OnStateChanged();
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (CurrentState == PlotState.CayCon && growTimer.Expired(Runner))
        {
            CurrentState = PlotState.CayLon;
            growTimer = TickTimer.None;
        }
    }

    private void ClearVisuals()
    {
        foreach (var obj in spawnedVisuals)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedVisuals.Clear();
    }

    private void SpawnVisual(GameObject prefab)
    {
        if (prefab == null) return;
        
        Transform viTriSinhRa = (diemGieoHat != null) ? diemGieoHat : transform;
        GameObject newVisual = Instantiate(prefab, viTriSinhRa.position, viTriSinhRa.rotation, transform);
        
        Collider[] cols = newVisual.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;
        
        spawnedVisuals.Add(newVisual);
    }

    private void OnStateChanged()
    {
        ClearVisuals(); 

        switch (CurrentState)
        {
            case PlotState.DatTrong:
                SpawnVisual(modelDatThuong);
                break;
            case PlotState.CayCon:
                SpawnVisual(modelDatThuong); 
                SpawnVisual(modelCayCon); 
                break;
            case PlotState.CayLon:
                SpawnVisual(modelDatThuong); 
                SpawnVisual(modelCayLon); 
                break;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_GieoHat()
    {
        if (CurrentState != PlotState.DatTrong) return; // Đất trống mới được gieo!

        CurrentState = PlotState.CayCon;
        growTimer = TickTimer.CreateFromSeconds(Runner, growTime); 
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ThuHoach(PlayerRef nguoi)
    {
        if (CurrentState != PlotState.CayLon) return;

        var playerObj = Runner.GetPlayerObject(nguoi);
        if (playerObj == null) return;

        Player_Controller player = playerObj.GetComponent<Player_Controller>();
        bool daCongXong = player.ThemDoVaoTui(fruitItemID, harvestCount);

        if (daCongXong)
        {
            Rpc_ThongBaoThuHoach(nguoi, harvestCount);
            
            // THU HOẠCH XONG LÀ VỀ LẠI ĐẤT TRỐNG ĐỂ GIEO TIẾP
            CurrentState = PlotState.DatTrong; 
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ThongBaoThuHoach([RpcTarget] PlayerRef targetPlayer, int soLuong)
    {
        if (ItemNotifyManager.Instance == null || InventoryManager.instance == null) return;

        Item thongTin = InventoryManager.instance.TraCuuItem(fruitItemID);
        if (thongTin != null)
        {
            ItemNotifyManager.Instance.ShowNotify(thongTin.itemName, soLuong, thongTin.icon);
        }  
    }
}