using UnityEngine;
using UnityEngine.InputSystem; 

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    [Header("Cài đặt Bản đồ")]
    public GameObject mapPanel; 
    public RectTransform mapPanelRect; // Dùng để đo khung nhìn của người chơi
    public RectTransform mapContent; 

    [Header("Cài đặt Zoom")]
    public float tocDoZoom = 0.1f;
    public float zoomNhoNhat = 0.5f; 
    public float zoomLonNhat = 2.5f; 

    public bool dangMoMap = false;

    void Update()
    {

        //if (ChatSystem.IsChatting) return;
        // 1. BẬT/TẮT BẢN ĐỒ BẰNG PHÍM M
        if (Keyboard.current.mKey.wasPressedThisFrame)
        {
            dangMoMap = !dangMoMap;
            mapPanel.SetActive(dangMoMap);

            if (dangMoMap)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // 2. PHÓNG TO / THU NHỎ
        if (dangMoMap)
        {
            float vungLanChuot = Mouse.current.scroll.ReadValue().y;
            
            if (vungLanChuot != 0)
            {
                float huongZoom = Mathf.Sign(vungLanChuot); 
                Vector3 scaleHienTai = mapContent.localScale;
                float scaleMoi = scaleHienTai.x + (huongZoom * tocDoZoom);
                
                scaleMoi = Mathf.Clamp(scaleMoi, zoomNhoNhat, zoomLonNhat);
                mapContent.localScale = new Vector3(scaleMoi, scaleMoi, 1f);
            }
        }
    }

    // ================= VÒNG KIM CÔ GIỚI HẠN MAP =================
    void LateUpdate()
    {
        if (dangMoMap && mapContent != null && mapPanelRect != null)
        {
            // Tính toán khoảng cách tối đa được phép kéo (tự động co giãn theo tỷ lệ Zoom)
            float gioiHanX = Mathf.Max(0, (mapContent.rect.width * mapContent.localScale.x - mapPanelRect.rect.width) / 2f);
            float gioiHanY = Mathf.Max(0, (mapContent.rect.height * mapContent.localScale.y - mapPanelRect.rect.height) / 2f);

            // Lấy tọa độ hiện tại của Map
            Vector2 toaDoHienTai = mapContent.anchoredPosition;

            // Ép tọa độ không được vượt qua giới hạn (-X đến X, -Y đến Y)
            toaDoHienTai.x = Mathf.Clamp(toaDoHienTai.x, -gioiHanX, gioiHanX);
            toaDoHienTai.y = Mathf.Clamp(toaDoHienTai.y, -gioiHanY, gioiHanY);
            
            // Gắn tọa độ đã sửa lại trả về cho Map
            mapContent.anchoredPosition = toaDoHienTai;
        }
    }

    // ==========================================
    // HÀM ĐÓNG MAP TỪ XA ĐỂ FIX LỖI CS1061
    // ==========================================
    public void DongMap()
    {
        dangMoMap = false;
        
        if (mapPanel != null)
        {
            mapPanel.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}