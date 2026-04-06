using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider))]
public class FarmZone : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private LayerMask _farmTileLayer;
    [SerializeField] private float _interactDistance = 5f;

    private Camera _mainCam;
    private bool _isLocalPlayerInside = false;

    private void Awake()
    {
        _mainCam = Camera.main; // Cache camera
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng vào có phải là Local Player không
        Player_Controller player = other.GetComponent<Player_Controller>();
        if (player != null && player.HasInputAuthority)
        {
            _isLocalPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Player_Controller player = other.GetComponent<Player_Controller>();
        if (player != null && player.HasInputAuthority)
        {
            _isLocalPlayerInside = false;
        }
    }

    private void Update()
    {
        // Chỉ xử lý Input nếu local player đang ở trong ruộng
        if (!_isLocalPlayerInside || Player_Controller.localPlayer == null) return;

        // Xử lý Input đổi công cụ (Phím 3, Phím 4, Phím 0)
        HandleToolInput();

        // Xử lý Click phải để tương tác ruộng
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            InteractWithFarmTile();
        }
    }

    private void HandleToolInput()
    {
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            Player_Controller.localPlayer.RPC_EquipTool(1); // 1 = Cuốc

        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            Player_Controller.localPlayer.RPC_EquipTool(4); // 4 = Hạt giống

        else if (Keyboard.current.digit1Key.wasPressedThisFrame)
            Player_Controller.localPlayer.RPC_EquipTool(0); // 0 = Tay không
    }

    private void InteractWithFarmTile()
    {
        // Raycast từ giữa màn hình (hoặc con trỏ chuột tùy bối cảnh camera của bạn)
        Ray ray = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, _interactDistance, _farmTileLayer))
        {
            FarmTile targetTile = hit.collider.GetComponent<FarmTile>();
            if (targetTile != null)
            {
                int currentTool = Player_Controller.localPlayer.CurrentToolIndex;
                string seedId = "";

                // Nếu đang cầm hạt giống, lấy ID hạt giống từ slot 4 của Player
                if (currentTool == 4)
                {
                    // Giả định: hàm GetSeedIdInHand() trả về ID của hạt giống đang cầm
                    seedId = "seed_tomato"; // Hardcode tạm thời theo mẫu Cà Chua
                }

                // Gửi RPC lên server yêu cầu thay đổi FarmTile
                targetTile.RPC_InteractTile(Player_Controller.localPlayer.Object.InputAuthority, currentTool, seedId);
            }
        }
    }
}