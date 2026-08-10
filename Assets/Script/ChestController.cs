using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChestItem
{
    public Item itemData;
    public int amount = 1;
}

public class ChestController : MonoBehaviour
{
    [Header("Cài đặt Rương")]
    [Tooltip("Cho phép F đi F lại nhiều lần (liên tục) hay chỉ 1 lần?")]
    public bool moNhieuLan = true;
    [Tooltip("Thời gian giữ phím F để mở rương")]
    public float thoiGianMo = 2f;
    [Tooltip("Danh sách các item trong rương")]
    public List<ChestItem> danhSachItem = new List<ChestItem>();
    
    [Header("Thành phần UI")]
    [Tooltip("Kéo Object UI nhắc nhở (VD: Hình phím F) trên đầu rương vào đây")]
    public GameObject uiTrenDauRuong;

    private Animator animator;
    private bool isPlayerNear = false;
    private float holdTimer = 0f;
    private Player_Controller currentPlayer;
    private bool daMo = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (uiTrenDauRuong != null)
        {
            uiTrenDauRuong.SetActive(false);
        }
    }

    void Update()
    {
        if (isPlayerNear && (moNhieuLan || !daMo))
        {
            // Kiểm tra người chơi giữ phím F
            if (Input.GetKey(KeyCode.F))
            {
                holdTimer += Time.deltaTime;
                
                // Hiển thị vòng tròn tiến trình (UI_TienTrinhDung)
                if (UI_TienTrinhDung.instance != null)
                {
                    float timeLeft = thoiGianMo - holdTimer;
                    if (timeLeft < 0) timeLeft = 0;
                    UI_TienTrinhDung.instance.CapNhatUI(timeLeft, thoiGianMo);
                }

                // Nếu giữ đủ thời gian thì mở rương
                if (holdTimer >= thoiGianMo)
                {
                    MoRuong();
                }
            }
            else
            {
                // Nếu nhả phím F ra giữa chừng thì reset thời gian và ẩn UI tiến trình
                if (holdTimer > 0)
                {
                    holdTimer = 0f;
                    if (UI_TienTrinhDung.instance != null)
                    {
                        UI_TienTrinhDung.instance.AnUI();
                    }
                }
            }
        }
    }

    private void MoRuong()
    {
        holdTimer = 0f;
        daMo = true; // Đánh dấu là đã mở
        
        // Ẩn UI tiến trình đếm ngược
        if (UI_TienTrinhDung.instance != null)
        {
            UI_TienTrinhDung.instance.AnUI();
        }

        // Ẩn UI hình phím F trên đầu nếu chỉ cho phép mở 1 lần
        if (!moNhieuLan && uiTrenDauRuong != null)
        {
            uiTrenDauRuong.SetActive(false);
        }

        // Chạy animation mở rương (phát lại từ đầu bằng cách thêm tham số thời gian 0f)
        if (animator != null)
        {
            animator.Play("Chest_Open", -1, 0f);
        }

        // Truyền item cho người chơi vô hạn lần (không xóa item khỏi danh sách)
        if (currentPlayer != null)
        {
            foreach (var chestItem in danhSachItem)
            {
                if (chestItem.itemData != null && chestItem.amount > 0)
                {
                    // Thêm đồ vào túi người chơi
                    currentPlayer.ThemDoVaoTui(chestItem.itemData.itemID, chestItem.amount);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player_Controller player = other.GetComponent<Player_Controller>();
            
            if (player != null)
            {
                isPlayerNear = true;
                currentPlayer = player;
                
                // Hiện UI nhắc nhở nếu được phép
                if (uiTrenDauRuong != null && (moNhieuLan || !daMo))
                {
                    uiTrenDauRuong.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player_Controller player = other.GetComponent<Player_Controller>();
            if (player != null && player == currentPlayer)
            {
                isPlayerNear = false;
                currentPlayer = null;
                holdTimer = 0f;

                if (uiTrenDauRuong != null)
                {
                    uiTrenDauRuong.SetActive(false);
                }

                if (UI_TienTrinhDung.instance != null)
                {
                    UI_TienTrinhDung.instance.AnUI();
                }
            }
        }
    }
}
