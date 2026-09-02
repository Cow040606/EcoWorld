using UnityEngine;
using UnityEngine.UI;

public class ObjectiveTarget : MonoBehaviour
{
    [Header("UI Prefabs")]
    public GameObject worldSpaceMarkerPrefab; 
    public GameObject compassIconPrefab; 

    [Header("Settings")]
    public Sprite iconSprite;
    public Color iconColor = Color.white; 
    public Vector3 markerOffset = new Vector3(0, 2f, 0);
    public string canvasName = "UI_Canvas";

    [Header("Quest & Default State")]
    [Tooltip("Tích vào đây nếu Object này là NPC giao nhiệm vụ. Nó sẽ ẩn đi ban đầu và chỉ hiện khi có Quest.")]
    public bool hideOnStart = false;

    [Header("Minimap Settings")]
    [Tooltip("Bật nếu bạn dùng Minimap dạng Camera chiếu từ trên xuống (Render Texture)")]
    public bool showOnMinimap = true;
    public Vector3 minimapIconScale = new Vector3(5f, 5f, 5f);
    [Tooltip("Điền tên Layer của Minimap (ví dụ: Minimap, UI...). Để trống sẽ dùng Default.")]
    public string minimapLayerName = "Minimap"; 

    private GameObject activeWorldSpaceMarker;
    private GameObject activeCompassIcon;
    private RectTransform compassIconRect;
    private GameObject minimapIconObj;

    private void Start()
    {
        if (!hideOnStart)
        {
            CreateMarkers();
        }
    }

    public void CreateMarkers()
    {
        if (activeWorldSpaceMarker != null || activeCompassIcon != null) return;

        // 1. Tạo World Space Marker
        if (worldSpaceMarkerPrefab != null)
        {
            Canvas targetCanvas = null;
            GameObject canvasObj = GameObject.Find(canvasName);
            if (canvasObj != null) targetCanvas = canvasObj.GetComponent<Canvas>();
            if (targetCanvas == null) targetCanvas = FindObjectOfType<Canvas>();

            if (targetCanvas != null)
            {
                activeWorldSpaceMarker = Instantiate(worldSpaceMarkerPrefab, targetCanvas.transform);
                WorldSpaceMarkerUI markerScript = activeWorldSpaceMarker.GetComponent<WorldSpaceMarkerUI>();
                if (markerScript != null)
                {
                    markerScript.Setup(transform, iconSprite, iconColor, markerOffset);
                }
            }
        }

        // 2. Tạo Compass Icon
        if (compassIconPrefab != null && CompassController.Instance != null)
        {
            Transform iconsParent = null;
            if (CompassController.Instance.compassContent.parent != null)
            {
                iconsParent = CompassController.Instance.compassContent.parent.Find("Icons");
            }
            if (iconsParent == null) iconsParent = CompassController.Instance.compassContent;

            activeCompassIcon = Instantiate(compassIconPrefab, iconsParent);
            compassIconRect = activeCompassIcon.GetComponent<RectTransform>();
            
            Image img = activeCompassIcon.GetComponent<Image>();
            if (img != null)
            {
                if (iconSprite != null) img.sprite = iconSprite;
                img.color = iconColor; 
                img.preserveAspect = true; 
            }
        }

        // 3. Tạo Minimap Icon (Dạng Sprite 3D chiếu lên trời)
        if (showOnMinimap && iconSprite != null)
        {
            minimapIconObj = new GameObject("MinimapIcon_" + gameObject.name);
            minimapIconObj.transform.SetParent(transform);
            
            // Đặt icon cao lên một chút và ngửa mặt lên trời để camera minimap nhìn thấy
            minimapIconObj.transform.localPosition = new Vector3(0, 10f, 0); 
            minimapIconObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); 
            minimapIconObj.transform.localScale = minimapIconScale;

            SpriteRenderer sr = minimapIconObj.AddComponent<SpriteRenderer>();
            sr.sprite = iconSprite;
            sr.color = iconColor;

            // Đổi layer để Camera chính không thấy (nếu bạn setup Culling Mask)
            int layerIndex = LayerMask.NameToLayer(minimapLayerName);
            if (layerIndex != -1) minimapIconObj.layer = layerIndex;
        }
    }

    public void HideMarker()
    {
        if (activeWorldSpaceMarker != null) activeWorldSpaceMarker.SetActive(false);
        if (activeCompassIcon != null) activeCompassIcon.SetActive(false);
        if (minimapIconObj != null) minimapIconObj.SetActive(false);
    }

    public void ShowMarker()
    {
        if (activeWorldSpaceMarker == null || activeCompassIcon == null) CreateMarkers(); 
        
        if (activeWorldSpaceMarker != null) activeWorldSpaceMarker.SetActive(true);
        if (activeCompassIcon != null) activeCompassIcon.SetActive(true);
        if (minimapIconObj != null) minimapIconObj.SetActive(true);
    }

    private void Update()
    {
        if (compassIconRect != null && activeCompassIcon.activeSelf && CompassController.Instance != null)
        {
            float posX = CompassController.Instance.GetCompassPositionX(transform.position);
            compassIconRect.anchoredPosition = new Vector2(posX, 0f);
        }
    }

    private void LateUpdate()
    {
        if (minimapIconObj != null)
        {
            minimapIconObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void OnDestroy()
    {
        if (activeWorldSpaceMarker != null) Destroy(activeWorldSpaceMarker);
        if (activeCompassIcon != null) Destroy(activeCompassIcon);
        if (minimapIconObj != null) Destroy(minimapIconObj);
    }
}
