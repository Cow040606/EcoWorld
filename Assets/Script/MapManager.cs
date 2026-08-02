using UnityEngine;
using UnityEngine.InputSystem;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [Header("Cài đặt UI Bản đồ Tĩnh")]
    public GameObject mapPanel;             // Khung chứa toàn bộ Map UI
    public RectTransform mapImageRect;      // RectTransform của Ảnh Bản Đồ Tĩnh (hoặc Container chứa ảnh)

    [Header("Cài đặt Zoom UI Map")]
    public float tocDoZoom = 0.15f;         // Tốc độ phóng to/thu nhỏ khi lăn chuột
    public float zoomNhoNhat = 1f;          // Tỷ lệ scale nhỏ nhất (Mặc định: 1)
    public float zoomLonNhat = 4f;          // Tỷ lệ scale lớn nhất (Ví dụ: 3 - 5)

    [Header("Cài đặt Lia (Drag/Pan) UI Map")]
    public float tocDoKeo = 1f;             // Hệ số tốc độ kéo chuột

    [HideInInspector]
    public bool dangMoMap = false;

    private float currentScale = 1f;
    private Vector2 viTriChuotCu;
    private Canvas parentCanvas;

    void Start()
    {
        if (mapImageRect != null)
        {
            currentScale = mapImageRect.localScale.x;
            parentCanvas = mapImageRect.GetComponentInParent<Canvas>();
        }
    }

    public void DongMap()
    {
        dangMoMap = false;
        if (mapPanel != null) mapPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void MoMap()
    {
        dangMoMap = true;
        if (mapPanel != null) mapPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (parentCanvas == null && mapImageRect != null)
        {
            parentCanvas = mapImageRect.GetComponentInParent<Canvas>();
        }
    }

    void Update()
    {
        // 1. BẬT / TẮT MAP BẰNG PHÍM M
        if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
        {
            if (dangMoMap) DongMap();
            else MoMap();
        }

        if (!dangMoMap || mapImageRect == null) return;

        // 2. ZOOM BẢN ĐỒ TĨNH (LĂN CHUỘT)
        if (Mouse.current != null)
        {
            float scrollDelta = Mouse.current.scroll.ReadValue().y;
            if (scrollDelta != 0f)
            {
                float deltaScale = Mathf.Sign(scrollDelta) * tocDoZoom;
                currentScale = Mathf.Clamp(currentScale + deltaScale, zoomNhoNhat, zoomLonNhat);
                mapImageRect.localScale = Vector3.one * currentScale;

                GioiHanViTriMap();
            }

            // 3. KÉO LIA BẢN ĐỒ (DRAG CHUỘT TRÁI / GIỮA)
            if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame)
            {
                viTriChuotCu = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.isPressed || Mouse.current.middleButton.isPressed)
            {
                Vector2 viTriChuotMoi = Mouse.current.position.ReadValue();
                Vector2 lechChuotScreen = viTriChuotMoi - viTriChuotCu;

                // Quy đổi độ lệch chuột theo Scale Factor của Canvas để kéo chính xác trên mọi độ phân giải
                float scaleFactor = (parentCanvas != null && parentCanvas.scaleFactor > 0) ? parentCanvas.scaleFactor : 1f;
                Vector2 lechCanvas = (lechChuotScreen / scaleFactor) * tocDoKeo;

                mapImageRect.anchoredPosition += lechCanvas;
                viTriChuotCu = viTriChuotMoi;

                GioiHanViTriMap();
            }
        }
    }

    // Giới hạn vị trí kéo chính xác tuyệt đối, không bao giờ để map bị trôi/lọt khỏi khung Viewport
    private void GioiHanViTriMap()
    {
        if (mapImageRect.parent == null) return;

        RectTransform parentRect = mapImageRect.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 parentSize = parentRect.rect.size;
        Vector2 mapSize = new Vector2(mapImageRect.rect.width * currentScale, mapImageRect.rect.height * currentScale);

        Vector2 parentPivot = parentRect.pivot;
        Vector2 mapPivot = mapImageRect.pivot;

        // Tính toán giới hạn nhỏ nhất và lớn nhất cho anchoredPosition dựa trên Pivot & Size thực tế
        float minX = (1f - parentPivot.x) * parentSize.x - (1f - mapPivot.x) * mapSize.x;
        float maxX = mapPivot.x * mapSize.x - parentPivot.x * parentSize.x;

        float minY = (1f - parentPivot.y) * parentSize.y - (1f - mapPivot.y) * mapSize.y;
        float maxY = mapPivot.y * mapSize.y - parentPivot.y * parentSize.y;

        Vector2 pos = mapImageRect.anchoredPosition;

        // Nếu kích thước map lớn hơn khung nhìn -> Clamp giữa [min, max]
        // Nếu kích thước map nhỏ hơn hoặc bằng khung nhìn -> Tự động căn giữa khung
        pos.x = (minX <= maxX) ? Mathf.Clamp(pos.x, minX, maxX) : (mapPivot.x - parentPivot.x) * parentSize.x;
        pos.y = (minY <= maxY) ? Mathf.Clamp(pos.y, minY, maxY) : (mapPivot.y - parentPivot.y) * parentSize.y;

        mapImageRect.anchoredPosition = pos;
    }
}