using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro; // Nếu dùng TextMeshPro cho HintText

public class FarmInteraction : MonoBehaviour
{
    [Header("--- References ---")]
    public Player_Controller myPlayer;
    public Transform cameraTransform; // Kéo Main Camera hoặc Camera của Player vào đây
    public LayerMask farmlandLayer;   // Nhớ set layer "Farmland" cho Prefab Đất
    public float interactRange = 4f;

    [Header("--- UI ---")]
    public TextMeshProUGUI hintText;  // Text hiện gợi ý trên màn hình

    private FarmPlot currentLookedPlot;

    private void Update()
    {
        // Chỉ xử lý nếu Player_Controller có giá trị, có quyền điều khiển và không mở UI/Inventory
        if (myPlayer == null || myPlayer.Object == null || !myPlayer.Object.HasInputAuthority) 
            return;

        // Giả sử có hàm kiểm tra UI đang mở bên GameManager/InventoryManager
        // if (UIManager.Instance.IsUIOpen) { hintText.text = ""; return; }

        CheckRaycast();
        HandleInput();
    }

    private void CheckRaycast()
    {
        // Bắn tia raycast từ giữa màn hình (Camera) ra phía trước
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, farmlandLayer))
        {
            currentLookedPlot = hit.collider.GetComponentInParent<FarmPlot>();
            
            if (currentLookedPlot != null && hintText != null)
            {
                // Cập nhật text gợi ý theo State của mảnh đất
                switch (currentLookedPlot.CurrentState)
                {
                    case FarmPlot.PlotState.Normal:
                        hintText.text = "[Chuột phải] Cày đất";
                        break;
                    case FarmPlot.PlotState.Tilled:
                        hintText.text = "[2] Gieo hạt";
                        break;
                    case FarmPlot.PlotState.Seeded:
                        hintText.text = "Cây đang lớn...";
                        break;
                    case FarmPlot.PlotState.Grown:
                        hintText.text = "[E] Thu hoạch\n[Chuột phải] Thu hoạch";
                        break;
                }
            }
        }
        else
        {
            currentLookedPlot = null;
            if (hintText != null) hintText.text = ""; // Xóa text khi không nhìn vào đất
        }
    }

    private void HandleInput()
    {
        if (currentLookedPlot == null) return;

        // [a] Nhấn chuột phải
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Normal)
            {
                currentLookedPlot.RPC_CayDat();
            }
            else if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Grown)
            {
                currentLookedPlot.RPC_ThuHoach(myPlayer.Runner.LocalPlayer);
            }
        }

        // [b] Nhấn phím E
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Grown)
            {
                currentLookedPlot.RPC_ThuHoach(myPlayer.Runner.LocalPlayer);
            }
        }

        // [c] Nhấn phím 2
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Tilled)
            {
                currentLookedPlot.RPC_GieoHat(myPlayer.Runner.LocalPlayer);
            }
        }
    }
}