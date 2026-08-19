using UnityEngine;
using TMPro;

public class QuestNotifyManager : MonoBehaviour
{
    public static QuestNotifyManager Instance;

    [Header("Cấu Hình Bảng Thông Báo")]
    [Tooltip("Kéo nguyên cục Nhiemvuhoanthanh vào đây")]
    public GameObject questBanner; 
    [Tooltip("Kéo Animator của Nhiemvuhoanthanh vào đây")]
    public Animator questAnimator;
    
    [Header("Các Ô Chữ (Kéo từ con của Nhiemvuhoanthanh vào)")]
    [Tooltip("Kéo chữ Label_QuestName vào đây")]
    public TextMeshProUGUI textQuestName;
    [Tooltip("Kéo chữ Label_XPNum vào đây")]
    public TextMeshProUGUI textExpReward;
    [Tooltip("Kéo chữ Label_CurrencyNum vào đây")]
    public TextMeshProUGUI textCurrencyReward;

    [Header("Thời Gian")]
    [Tooltip("Thời gian bảng thông báo dừng lại trên màn hình trước khi cất đi")]
    public float displayTime = 3f;

    private float hideTime = 0f;
    private int bannerState = 0; // 0: Đang tắt, 1: Đang hiện, 2: Đang bay ra

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Tắt bảng đi lúc mới vào game để đỡ vướng
        if (questBanner != null) questBanner.SetActive(false);
    }

    public void ShowQuestComplete(string questName, int expReward, int currencyReward)
    {
        // 1. Bật cục Gameobject lên trước
        if (questBanner != null) questBanner.SetActive(true);

        // 2. Điền thông tin vào các ô chữ
        if (textQuestName != null) textQuestName.text = questName;
        if (textExpReward != null) textExpReward.text = "+" + expReward;
        if (textCurrencyReward != null) textCurrencyReward.text = "+" + currencyReward;

        // 3. Kích hoạt Animation "In"
        if (questAnimator != null)
        {
            questAnimator.SetBool("Active", true);
        }

        // 4. Bắt đầu đếm ngược bằng thời gian thực (Time.unscaledTime)
        hideTime = Time.unscaledTime + displayTime;
        bannerState = 1;
    }

    void Update()
    {
        // Hết giờ đọc thông báo -> Chuyển sang bay ra
        if (bannerState == 1 && Time.unscaledTime >= hideTime)
        {
            if (questAnimator != null) 
            {
                questAnimator.SetBool("Active", false);
            }
            
            // Cho nó 1 giây để thực hiện hiệu ứng bay ra
            hideTime = Time.unscaledTime + 1f; 
            bannerState = 2;
        }
        // Hết 1 giây bay ra -> Tắt nguồn
        else if (bannerState == 2 && Time.unscaledTime >= hideTime)
        {
            if (questBanner != null) 
            {
                questBanner.SetActive(false);
            }
            bannerState = 0;
        }
    }
}
