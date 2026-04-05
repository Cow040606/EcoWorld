using UnityEngine;
using Unity.Netcode;

public class PlayerFarming : NetworkBehaviour
{
    public float interactRange = 10f;
    public GameObject cropPrefab; // Kéo thả Crop_Rice prefab vào ô này trong Inspector của người chơi

    void Update()
    {
        // Chỉ cho phép người chủ của nhân vật này được điều khiển
        if (!IsOwner) return;

        // Bấm chuột trái để Cuốc đất
        if (Input.GetMouseButtonDown(0))
        {
            InteractWithSoil(false);
        }
        // Bấm chuột phải để Trồng cây
        else if (Input.GetMouseButtonDown(1))
        {
            InteractWithSoil(true);
        }
    }

    private void InteractWithSoil(bool isPlanting)
    {
        // Bắn tia từ camera đến vị trí chuột trên màn hình
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            SoilBlock soil = hit.collider.GetComponent<SoilBlock>();
            if (soil != null)
            {
                if (!isPlanting)
                {
                    // Gọi hàm cuốc đất (đã viết sẵn) trên Server
                    soil.TillSoilServerRpc();
                }
                else
                {
                    // Trồng cây: Kiểm tra đất đã cuốc và chưa có cây chưa
                    if (soil.isTilled.Value && !soil.hasPlant.Value)
                    {
                        // Gọi Server để sinh ra cây
                        PlantCropServerRpc(soil.NetworkObjectId);
                    }
                }
            }
        }
    }

    [ServerRpc]
    public void PlantCropServerRpc(ulong soilNetworkObjectId)
    {
        // 1. Tìm mảnh đất trên Server dựa vào ID
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(soilNetworkObjectId, out NetworkObject soilNetObj))
        {
            SoilBlock soil = soilNetObj.GetComponent<SoilBlock>();

            // 2. Kiểm tra lại một lần nữa trên Server cho an toàn
            if (soil.isTilled.Value && !soil.hasPlant.Value)
            {
                soil.hasPlant.Value = true; // Đánh dấu là đã có cây

                // 3. Tạo ra cây từ Prefab
                GameObject newCrop = Instantiate(cropPrefab, soil.transform.position, Quaternion.identity);
                NetworkObject cropNetObj = newCrop.GetComponent<NetworkObject>();

                // 4. Spawn cây qua mạng để tất cả người chơi đều thấy
                cropNetObj.Spawn();

                // 5. Báo cho cây biết nó đang nằm trên mảnh đất nào
                Crop cropScript = newCrop.GetComponent<Crop>();
                cropScript.SetParentSoil(soilNetworkObjectId);
            }
        }
    }
}