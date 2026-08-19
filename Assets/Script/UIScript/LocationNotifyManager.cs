using UnityEngine;
using TMPro;

public class LocationNotifyManager : MonoBehaviour
{
    public static LocationNotifyManager Instance;

    [Header("Cấu Hình Bảng Thông Báo Vùng Đất")]
    [Tooltip("Kéo cục Location từ Hierarchy vào đây")]
    public GameObject locationBanner; 
    [Tooltip("Kéo Animator của cục Location vào đây")]
    public Animator locationAnimator;
    
    [Header("Các Ô Chữ")]
    [Tooltip("Kéo chữ hiển thị tên vùng đất vào đây (Tìm trong ruột cục Location)")]
    public TextMeshProUGUI textLocationName;

    [Header("Thời Gian")]
    [Tooltip("Thời gian bảng thông báo hiển thị trước khi bay ra")]
    public float displayTime = 4f;

    private float hideTime = 0f;
    private int bannerState = 0; // 0: Đang tắt, 1: Đang hiện, 2: Đang bay ra
    private string lastLocation = ""; // Nhớ tên vùng đất gần nhất để không hiện lại liên tục

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (locationBanner != null) locationBanner.SetActive(false);
    }

    public void ShowLocation(string locationName)
    {
        // Nếu vẫn đang đứng ở vùng cũ thì không hiện lại thông báo làm phiền
        if (locationName == lastLocation) return;
        
        lastLocation = locationName;

        if (locationBanner != null) locationBanner.SetActive(true);
        if (textLocationName != null) textLocationName.text = locationName;

        if (locationAnimator != null)
        {
            locationAnimator.SetBool("Active", true);
        }

        hideTime = Time.unscaledTime + displayTime;
        bannerState = 1;
    }

    void Update()
    {
        if (bannerState == 1 && Time.unscaledTime >= hideTime)
        {
            if (locationAnimator != null) 
            {
                locationAnimator.SetBool("Active", false);
            }
            hideTime = Time.unscaledTime + 1f; // Chờ 1 giây cho animation Out
            bannerState = 2;
        }
        else if (bannerState == 2 && Time.unscaledTime >= hideTime)
        {
            if (locationBanner != null) 
            {
                locationBanner.SetActive(false);
            }
            bannerState = 0;
        }
    }

    // Hàm này dùng để reset khi người chơi dịch chuyển (Teleport) xa
    public void ResetLastLocation()
    {
        lastLocation = "";
    }
}
