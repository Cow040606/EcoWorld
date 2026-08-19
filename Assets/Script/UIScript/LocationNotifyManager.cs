using UnityEngine;
using TMPro;

public class LocationNotifyManager : MonoBehaviour
{
    public static LocationNotifyManager Instance;

    [Header("Cấu Hình Bảng Thông Báo Vùng Đất")]
    [Tooltip("Kéo cục Location từ Hierarchy vào đây để làm BẢN GỐC (Nó sẽ tự động được nhân bản ra)")]
    public GameObject locationBanner; 
    
    // Giữ lại các biến này để Unity không báo lỗi mất link Inspector, dù không dùng trực tiếp nữa
    [HideInInspector] public Animator locationAnimator;
    [HideInInspector] public TextMeshProUGUI textLocationName;

    [Header("Thời Gian")]
    public float displayTime = 4f;

    private string lastLocation = ""; 
    private GameObject currentBannerClone; // Bản sao đang chạy trên màn hình
    private float hideTime = 0f;
    private int bannerState = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Tắt bản gốc đi, chỉ dùng nó làm khuôn đúc
        if (locationBanner != null) locationBanner.SetActive(false);
    }

    public void ShowLocation(string locationName)
    {
        if (locationName == lastLocation) return;
        lastLocation = locationName;

        // Nếu đang có một bản sao cũ đang múa trên màn hình -> Hủy diệt nó ngay lập tức
        if (currentBannerClone != null) Destroy(currentBannerClone);

        if (locationBanner == null) return;

        // TẠO RA MỘT BẢN SAO MỚI TINH TƯƠM 100% (Đảm bảo thông số RectTransform luôn chuẩn như lúc đầu)
        currentBannerClone = Instantiate(locationBanner, locationBanner.transform.parent);
        currentBannerClone.SetActive(true);

        // Tìm và đổi chữ bên trong bản sao
        Transform textObj = currentBannerClone.transform.Find("Location/Label_Location");
        if (textObj != null)
        {
            TextMeshProUGUI txt = textObj.GetComponent<TextMeshProUGUI>();
            if (txt != null) txt.text = locationName;
        }

        // Gọi Animator của bản sao
        Animator anim = currentBannerClone.GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetBool("Active", true);
        }

        hideTime = Time.unscaledTime + displayTime;
        bannerState = 1;
    }

    void Update()
    {
        if (bannerState == 1 && Time.unscaledTime >= hideTime)
        {
            // Bắt đầu gọi animation bay ra (Out)
            if (currentBannerClone != null)
            {
                Animator anim = currentBannerClone.GetComponent<Animator>();
                if (anim != null) anim.SetBool("Active", false);
            }
            hideTime = Time.unscaledTime + 1f; // Chờ 1 giây cho múa xong
            bannerState = 2;
        }
        else if (bannerState == 2 && Time.unscaledTime >= hideTime)
        {
            // Múa xong rồi thì Hủy diệt bản sao luôn cho sạch sẽ bộ nhớ
            if (currentBannerClone != null)
            {
                Destroy(currentBannerClone);
            }
            bannerState = 0;
        }
    }

    public void ResetLastLocation()
    {
        lastLocation = "";
    }
}
