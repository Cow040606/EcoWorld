using UnityEngine;
using TMPro;
using System.Linq; 
using Fusion; // Cần cái này để hiểu các biến mạng

public class BangXepHang : MonoBehaviour
{
    [Header("Gắn 2 cái chữ trên bảng gỗ vào đây")]
    public TextMeshProUGUI txtTopLevel;
    public TextMeshProUGUI txtTopTien;

    private float thoiGianCapNhat = 3f;
    private float demNguoc = 0f;

    void Update()
    {
        demNguoc -= Time.deltaTime;
        if (demNguoc <= 0)
        {
            QuetNguoiChoiVaXepHang();
            demNguoc = thoiGianCapNhat;
        }
    }

    void QuetNguoiChoiVaXepHang()
    {
        // // 1. Tìm tất cả các ông Player_Controller đang chạy trong Map
        Player_Controller[] tatCaNguoiChoi = FindObjectsOfType<Player_Controller>();

        if (tatCaNguoiChoi.Length == 0) return;

        // // ==========================================
        // // 2. XỬ LÝ BẢNG LEVEL (TOP CÀY CUỐC)
        // // ==========================================
        // var topLevel = tatCaNguoiChoi.OrderByDescending(p => p.CurrentLevel).Take(5).ToArray();

        // string chuoiLevel = "<color=yellow>TOP CÀY CUỐC</color>\n\n";
        // for (int i = 0; i < topLevel.Length; i++)
        // {
        //     // --- KHÚC NÀY LÀ TUYỆT CHIÊU LẤY TÊN ĐÂY BÒ ---
        //     // Thử tìm cái script Player_Data trên cùng con nhân vật đó
        //     Player_Data data = topLevel[i].GetComponent<Player_Data>();
            
        //     // Nếu có tên thì lấy tên, không có (lỗi) thì mới dùng Player ID cho chắc ăn
        //     string tenHienThi = (data != null && !string.IsNullOrEmpty(data.tenTrenMang.ToString())) 
        //                         ? data.tenTrenMang.ToString() 
        //                         : "Player " + topLevel[i].Object.InputAuthority.PlayerId;

        //     chuoiLevel += $"#{i + 1} - {tenHienThi} : Cấp {topLevel[i].CurrentLevel}\n";
        // }
        // txtTopLevel.text = chuoiLevel;


        // ==========================================
        // 3. XỬ LÝ BẢNG TIỀN (TOP ĐẠI GIA)
        // ==========================================
        var topTien = tatCaNguoiChoi.OrderByDescending(p => p.Gold).Take(5).ToArray();

        string chuoiTien = "<color=green>TOP ĐẠI GIA</color>\n\n";
        for (int i = 0; i < topTien.Length; i++)
        {
            // Tương tự bảng Level, lấy tên từ Player_Data
            Player_Data data = topTien[i].GetComponent<Player_Data>();
            string tenHienThi = (data != null && !string.IsNullOrEmpty(data.tenTrenMang.ToString())) 
                                ? data.tenTrenMang.ToString() 
                                : "Player " + topTien[i].Object.InputAuthority.PlayerId;

            chuoiTien += $"#{i + 1} - {tenHienThi} : {topTien[i].Gold} Vàng\n";
        }
        txtTopTien.text = chuoiTien;
    }
}