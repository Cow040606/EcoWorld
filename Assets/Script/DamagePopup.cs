using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("--- THIẾT LẬP MÀU SẮC ---")]
    public static Color MauDanhThuong = new Color(1f, 0.95f, 0.75f, 1f);      // Vàng ngà sáng
    public static Color MauChiMang = new Color(1f, 0.55f, 0.05f, 1f);          // Vàng cam rực rỡ
    public static Color MauPlayerBiThuong = new Color(1f, 0.22f, 0.22f, 1f);   // Đỏ tươi
    public static Color MauHoiMau = new Color(0.25f, 1f, 0.35f, 1f);           // Xanh lá cây hồi máu

    [Header("--- THIẾT LẬP KÍCH THƯỚC CHỮ ---")]
    public static float SizeDanhThuong = 5.2f;
    public static float SizeChiMang = 7.0f;
    public static string TienToChiMang = "<size=65%>CRIT!</size> ";

    [Header("--- THIẾT LẬP VIỀN ĐEN NỔI BẬT (OUTLINE) ---")]
    public static bool BatVienDen = true;
    public static Color MauVien = Color.black;
    public static float DoDayVien = 0.22f;

    [Header("--- FONT ASSET (GRENZE-SEMIBOLD SDF) ---")]
    public static TMP_FontAsset CustomFontAsset = null;

    private TextMeshPro textMesh;
    private float disappearTimer;
    private float disappearSpeed = 3f;
    private Color textColor;
    private Vector3 moveVector;
    private Transform mainCameraTransform;

    private const float DISAPPEAR_TIMER_MAX = 0.85f;

    /// <summary>
    /// Tạo Pop Dame nổi lên tại tọa độ chỉ định trong không gian 3D
    /// </summary>
    /// <param name="position">Vị trí xuất hiện</param>
    /// <param name="damageAmount">Số lượng sát thương</param>
    /// <param name="isCriticalHit">Có phải đòn chí mạng / gồng max lực không</param>
    /// <param name="isPlayerHurt">Người chơi bị dính sát thương (Màu đỏ)</param>
    public static DamagePopup Create(Vector3 position, int damageAmount, bool isCriticalHit = false, bool isPlayerHurt = false)
    {
        if (damageAmount <= 0) return null;

        GameObject popupObj = null;

        // Ưu tiên load Prefab từ Resources nếu có
        GameObject prefab = Resources.Load<GameObject>("DamagePopup");
        if (prefab != null)
        {
            popupObj = Instantiate(prefab);
        }
        else
        {
            popupObj = new GameObject("DamagePopup");
        }
        
        // Dịch ngẫu nhiên nhẹ để các số sát thương liên tiếp không bị đè lên nhau
        Vector3 randomOffset = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.1f, 0.25f), Random.Range(-0.35f, 0.35f));
        popupObj.transform.position = position + randomOffset;

        DamagePopup damagePopup = popupObj.GetComponent<DamagePopup>();
        if (damagePopup == null) damagePopup = popupObj.AddComponent<DamagePopup>();
        
        damagePopup.Setup(damageAmount, isCriticalHit, isPlayerHurt);

        return damagePopup;
    }

    private void Setup(int damageAmount, bool isCriticalHit, bool isPlayerHurt)
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null) textMesh = gameObject.AddComponent<TextMeshPro>();

        // 1. Tự động nạp Font Grenze-SemiBold SDF
        if (CustomFontAsset != null)
        {
            textMesh.font = CustomFontAsset;
        }
        else
        {
            TMP_FontAsset grenzeFont = Resources.Load<TMP_FontAsset>("Fonts/Grenze-SemiBold SDF");
            if (grenzeFont == null) grenzeFont = Resources.Load<TMP_FontAsset>("Grenze-SemiBold SDF");
            
#if UNITY_EDITOR
            if (grenzeFont == null)
            {
                grenzeFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Synty/InterfaceFantasyWarriorHUD/Fonts/Grenze/Grenze-SemiBold SDF.asset");
            }
#endif

            if (grenzeFont != null)
            {
                textMesh.font = grenzeFont;
            }
        }

        // 2. Cấu hình nội dung chữ & IN ĐẬM (Bold)
        textMesh.text = damageAmount.ToString();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = isCriticalHit ? SizeChiMang : SizeDanhThuong;
        textMesh.fontStyle = FontStyles.Bold; // Luôn luôn IN ĐẬM theo yêu cầu

        // 3. Cấu hình màu sắc
        if (isPlayerHurt)
        {
            textColor = MauPlayerBiThuong;
        }
        else if (isCriticalHit)
        {
            textColor = MauChiMang;
            textMesh.text = TienToChiMang + damageAmount;
        }
        else
        {
            textColor = MauDanhThuong;
        }

        textMesh.color = textColor;

        // 4. Kích hoạt viền đen (Outline) để chữ nổi bật trên mọi nền
        if (BatVienDen && textMesh.fontMaterial != null)
        {
            textMesh.outlineColor = MauVien;
            textMesh.outlineWidth = DoDayVien;
        }

        disappearTimer = DISAPPEAR_TIMER_MAX;

        // 5. Tìm Camera để xoay mặt (Billboard)
        if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        else
        {
            Camera cam = FindFirstObjectByType<Camera>();
            if (cam != null) mainCameraTransform = cam.transform;
        }

        if (mainCameraTransform != null)
        {
            transform.forward = mainCameraTransform.forward;
        }

        // Vector vận tốc bay lên
        moveVector = new Vector3(Random.Range(-0.6f, 0.6f), 2.2f, Random.Range(-0.6f, 0.6f));
        transform.localScale = Vector3.one * 0.5f;
    }

    private void Update()
    {
        // 1. Bay lên trên và hãm dần vận tốc
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * (3.5f * Time.deltaTime);

        // 2. Hiệu ứng nảy phóng to (Pop Scale) ở nửa thời gian đầu
        if (disappearTimer > DISAPPEAR_TIMER_MAX * 0.55f)
        {
            float scaleIncrease = 1.6f;
            transform.localScale += Vector3.one * (scaleIncrease * Time.deltaTime);
        }
        else
        {
            // Nửa thời gian sau: Thu nhỏ nhẹ
            transform.localScale -= Vector3.one * (0.35f * Time.deltaTime);
        }

        // 3. Đếm ngược và làm mờ Alpha
        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            textColor.a -= disappearSpeed * Time.deltaTime;
            textMesh.color = textColor;
            if (textColor.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }

    private void LateUpdate()
    {
        // Luôn luôn quay mặt chuẩn xác về phía Camera (Billboard effect)
        if (mainCameraTransform != null)
        {
            transform.forward = mainCameraTransform.forward;
        }
        else if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            transform.forward = mainCameraTransform.forward;
        }
    }
}
