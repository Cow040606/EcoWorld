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
        // Đảm bảo Local Player phải tồn tại và đang đứng trong ruộng
        if (!_isLocalPlayerInside || Player_Controller.localPlayer == null) return;

        // Click chuột phải để cuốc đất / trồng cây
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            InteractWithFarmTile();
        }
    }

    private void InteractWithFarmTile()
    {
        // Lấy Camera Transform trực tiếp từ Player_Controller thay vì Camera.main
        Transform camTransform = Player_Controller.localPlayer.cameraTransform;
        
        // Nếu vì lý do nào đó Camera chưa kịp load thì thoát hàm để tránh lỗi Null
        if (camTransform == null) return;

        // Bắn tia Raycast từ vị trí camera, hướng về phía trước
        Ray ray = new Ray(camTransform.position, camTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _farmTileLayer))
        {
            FarmTile targetTile = hit.collider.GetComponent<FarmTile>();
            if (targetTile != null)
            {
                int currentTool = Player_Controller.localPlayer.CurrentToolIndex;
                int seedId = 0;

                // Nếu đang cầm Hạt Giống (Tool = 4), set ID hạt giống bằng SỐ (VD: 101)
                if (currentTool == 4)
                {
                    seedId = 101; 
                }

                targetTile.RPC_InteractTile(Player_Controller.localPlayer.Object.InputAuthority, currentTool, seedId);
            }
        }
    }
}