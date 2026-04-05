using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Crop : NetworkBehaviour
{
    public CropType cropType;
    public NetworkVariable<bool> isReadyToHarvest = new NetworkVariable<bool>(false);
    public float growthTime = 300f;
    public GameObject fullyGrownModel;
    public GameObject seedlingModel;
    private ulong parentSoilId;

    public override void OnNetworkSpawn()
    {
        UpdateModels(isReadyToHarvest.Value);
        isReadyToHarvest.OnValueChanged += (oldVal, newVal) => UpdateModels(newVal);

        if (IsServer)
        {
            StartCoroutine(GrowRoutine());
        }
    }

    public void SetParentSoil(ulong soilId)
    {
        parentSoilId = soilId;
    }

    private void UpdateModels(bool isReady)
    {
        seedlingModel.SetActive(!isReady);
        fullyGrownModel.SetActive(isReady);
    }

    private IEnumerator GrowRoutine()
    {
        yield return new WaitForSeconds(growthTime);
        isReadyToHarvest.Value = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void HarvestServerRpc()
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentSoilId, out NetworkObject soilNetObj))
        {
            SoilBlock soil = soilNetObj.GetComponent<SoilBlock>();
            if (soil != null)
            {
                soil.ResetSoilServer();
            }
        }
        GetComponent<NetworkObject>().Despawn();
    }
}