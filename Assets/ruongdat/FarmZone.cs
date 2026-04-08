using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class FarmZone : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask _farmTileLayer;
    [SerializeField] private float _interactDistance = 5f;
    
    private bool _isLocalPlayerInside = false;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Player_Controller player = other.GetComponent<Player_Controller>();
        if (player != null && player.HasInputAuthority) _isLocalPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        Player_Controller player = other.GetComponent<Player_Controller>();
        if (player != null && player.HasInputAuthority) _isLocalPlayerInside = false;
    }

    private void Update()
    {
        if (!_isLocalPlayerInside || Player_Controller.localPlayer == null) return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            InteractWithFarmTile();
        }
    }

    private void InteractWithFarmTile()
    {
        Transform camTransform = Player_Controller.localPlayer.cameraTransform;
        if (camTransform == null) return;

        Ray ray = new Ray(camTransform.position, camTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _farmTileLayer))
        {
            FarmTile targetTile = hit.collider.GetComponent<FarmTile>();
            if (targetTile != null)
            {
                int currentTool = Player_Controller.localPlayer.CurrentToolIndex;
                int seedId = 0;

                // SỬA Ở ĐÂY: Phím 4 tương ứng với ToolIndex = 3
                if (currentTool == 3)
                {
                    seedId = 101; 
                }

                Debug.Log($"[Client] Click vào đất! Đang cầm Tool: {currentTool}, Truyền SeedID: {seedId}");
                targetTile.RPC_InteractTile(Player_Controller.localPlayer.Object.InputAuthority, currentTool, seedId);
            }
        }
    }
}