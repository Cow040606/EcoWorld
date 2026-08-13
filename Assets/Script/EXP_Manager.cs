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

        if(sliderExp != null && myPlayer != null) 
        {
            sliderExp.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            sliderExp2.value = myPlayer.ExpCurrent / myPlayer.expToLevelUp;
            ExpText.text = myPlayer.level.ToString();

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
                        levelUpAnimator.SetTrigger("LevelUp");
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
}
