using UnityEngine;
using TMPro;
using System.Linq; // Tấm bùa gọi thần LINQ để sắp xếp cực mạnh

public class BangXepHang : MonoBehaviour
{
    [Header("Gắn 2 cái chữ trên bảng gỗ vào đây")]
    public TextMeshProUGUI txtTopLevel;
    public TextMeshProUGUI txtTopTien;

    // Không dùng Update chạy liên tục 60 lần/s vì rất nặng máy
    // Mình sẽ cho nó chớp cập nhật 3 giây 1 lần
    private float thoiGianCapNhat = 3f;
    private float demNguoc = 0f;

    void Update()
    {
        demNguoc -= Time.deltaTime;
        if (demNguoc <= 0)
        {
            QuetNguoiChoiVaXepHang();
            demNguoc = thoiGianCapNhat; // Đặt lại đồng hồ
        }
    }

    void QuetNguoiChoiVaXepHang()
    {
        // 1. Gom cổ tất cả người chơi đang có mặt trên bản đồ
        Player_Controller[] tatCaNguoiChoi = FindObjectsOfType<Player_Controller>();

        // Nếu phòng chưa có ai thì nghỉ
        if (tatCaNguoiChoi.Length == 0) return;

        // ==========================================
        // 2. XỬ LÝ BẢNG LEVEL (TOP CÀY CUỐC)
        // Lấy danh sách -> Sắp xếp giảm dần theo CurrentLevel -> Cắt lấy 5 ông đầu tiên
        // ==========================================
        // var topLevel = tatCaNguoiChoi.OrderByDescending(p => p.CurrentLevel).Take(5).ToArray();

        // string chuoiLevel = "<color=yellow>TOP CÀY CUỐC</color>\n\n";
        // for (int i = 0; i < topLevel.Length; i++)
        // {
        //     // Nếu Bò có biến Tên nhân vật thì thay vào chỗ "Player..." nhé
        //     chuoiLevel += $"#{i + 1} - Player {topLevel[i].Object.InputAuthority.PlayerId} : Cấp {topLevel[i].CurrentLevel}\n";
        // }
        // txtTopLevel.text = chuoiLevel;


        // ==========================================
        // 3. XỬ LÝ BẢNG TIỀN (TOP ĐẠI GIA)
        // Lấy danh sách -> Sắp xếp giảm dần theo Gold -> Cắt lấy 5 ông đầu tiên
        // ==========================================
        var topTien = tatCaNguoiChoi.OrderByDescending(p => p.Gold).Take(5).ToArray();

        string chuoiTien = "<color=green>TOP ĐẠI GIA</color>\n\n";
        for (int i = 0; i < topTien.Length; i++)
        {
            chuoiTien += $"#{i + 1} - Player {topTien[i].Object.InputAuthority.PlayerId} : {topTien[i].Gold} Vàng\n";
        }
        txtTopTien.text = chuoiTien;
    }
}