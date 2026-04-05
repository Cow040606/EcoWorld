using UnityEngine;
using Unity.Netcode;

public class SoilBlock : NetworkBehaviour
{
    public NetworkVariable<bool> isTilled = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> hasPlant = new NetworkVariable<bool>(false);
    public Material tilledMaterial;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        isTilled.OnValueChanged += OnTilledStateChanged;
        if (isTilled.Value)
        {
            UpdateMaterial();
        }
    }

    public override void OnNetworkDespawn()
    {
        isTilled.OnValueChanged -= OnTilledStateChanged;
    }

    private void OnTilledStateChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            UpdateMaterial();
        }
    }

    private void UpdateMaterial()
    {
        if (tilledMaterial != null && meshRenderer != null)
        {
            meshRenderer.material = tilledMaterial;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TillSoilServerRpc()
    {
        if (!isTilled.Value && !hasPlant.Value)
        {
            isTilled.Value = true;
        }
    }

    public void ResetSoilServer()
    {
        if (IsServer)
        {
            hasPlant.Value = false;
        }
    }
}