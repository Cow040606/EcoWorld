using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class EXP_Manager : MonoBehaviour
{
    public Slider sliderExp;
    public Slider sliderExp2;
    public TextMeshProUGUI ExpText;
    
    [Tooltip("Kéo object HUD_PlayerLevel (có chứa Animator) vào đây")]
    public Animator levelUpAnimator;
    
    [Tooltip("Kéo chữ hiển thị số Level (khi đang nhảy thông báo Level Up) vào đây")]
    public TextMeshProUGUI levelUpText;
    
    [Header("XP LOG")]
    [Tooltip("Kéo object HUD_XPLog_Item (có chứa Animator) vào đây")]
    public Animator xpLogAnimator;
    [Tooltip("Kéo chữ Label_XP vào đây")]
    public TextMeshProUGUI xpLogText;

    private int currentLevel = -1; // Biến lưu trữ level để so sánh
    private float previousExp = -1f;
    private float previousExpToLevelUp = -1f;

    void Update()
    {
        Player_Controller myPlayer = Player_Controller.localPlayer;

        if(myPlayer != null) 
        {
            if (sliderExp != null) sliderExp.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            if (sliderExp2 != null) sliderExp2.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            if (ExpText != null) ExpText.text = myPlayer.level.ToString();

            // Khởi tạo level ban đầu khi vừa vào game
            if (currentLevel == -1) 
            {
                currentLevel = myPlayer.level;
                previousExp = myPlayer.ExpCurrent;
                previousExpToLevelUp = myPlayer.expToLevelUp;
            }
            else 
            {
                float gainedExp = 0;

                // Nếu EXP tăng và chưa lên cấp
                if (myPlayer.level == currentLevel && myPlayer.ExpCurrent > previousExp)
                {
                    gainedExp = myPlayer.ExpCurrent - previousExp;
                }
                // Nếu phát hiện level của player lớn hơn level đang lưu -> Lên cấp!
                else if (myPlayer.level > currentLevel)
                {
                    // Lượng EXP nhận được = (Lượng EXP cần để đầy thanh cũ - EXP cũ) + EXP dư bị tràn sang thanh mới
                    gainedExp = (previousExpToLevelUp - previousExp) + myPlayer.ExpCurrent;
                    
                    // Chạy animation level up
                    if (levelUpAnimator != null)
                    {
                        // Bật gameobject lên trước
                        levelUpAnimator.gameObject.SetActive(true);
                        
                        // Khôi phục độ sáng 100% để đề phòng trường hợp đang mờ dở thì lại lên cấp
                        CanvasGroup cg = levelUpAnimator.gameObject.GetComponent<CanvasGroup>();
                        if (cg != null) cg.alpha = 1f;
                        
                        levelUpAnimator.Play("LevelUp", -1, 0f); 
                        
                        // Gọi đồng hồ đếm ngược tự động tắt
                        StopCoroutine("HideLevelUpBanner");
                        StartCoroutine("HideLevelUpBanner");
                    }
                    
                    // Đổi số Level hiển thị trên chữ thông báo
                    if (levelUpText != null)
                    {
                        levelUpText.text = myPlayer.level.ToString();
                    }
                }

                // Nếu có cộng thêm EXP (và lượng cộng thêm đủ lớn, ví dụ >= 1) thì mới bật animation "+ XP"
                // Việc này giúp lọc lượng EXP thụ động (cộng rất nhỏ từng frame) không làm kẹt Animator
                if (gainedExp >= 1f)
                {
                    if (xpLogAnimator != null) 
                    {
                        xpLogAnimator.Play("In", -1, 0f); // Ép chạy lại animation "In" từ đầu (frame 0)
                    }
                    if (xpLogText != null) 
                    {
                        xpLogText.text = "+" + Mathf.RoundToInt(gainedExp) + " XP";
                    }
                }

                // Cập nhật lại các biến lưu trữ để vòng lặp sau so sánh tiếp
                currentLevel = myPlayer.level; 
                previousExp = myPlayer.ExpCurrent;
                previousExpToLevelUp = myPlayer.expToLevelUp;
            }
        }
    }

    // Hàm tự động tắt bảng Level Up mượt mà
    private System.Collections.IEnumerator HideLevelUpBanner()
    {
        // Chờ 2.5 giây cho hiệu ứng tung tóe chạy gần xong
        yield return new WaitForSeconds(2.5f);
        
        if (levelUpAnimator != null)
        {
            // Tự động gắn CanvasGroup nếu chưa có để làm mờ
            CanvasGroup canvasGroup = levelUpAnimator.gameObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = levelUpAnimator.gameObject.AddComponent<CanvasGroup>();

            float duration = 1f; // Tốn 1 giây để mờ dần
            float time = 0f;

            // Chạy vòng lặp giảm độ sáng từ 1 về 0
            while (time < duration)
            {
                time += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
                yield return null; 
            }

            // Mờ xong thì mới tắt hoàn toàn để tiết kiệm tài nguyên
            levelUpAnimator.gameObject.SetActive(false);
            
            // Trả lại độ sáng 100% để lần lên cấp tiếp theo không bị tàng hình
            canvasGroup.alpha = 1f;
        }
    }
}
