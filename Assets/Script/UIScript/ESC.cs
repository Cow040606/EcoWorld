using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion; // Phải có thư viện mạng để đọc NetworkArray

public class ESC : MonoBehaviour 
{
    public static ESC instance;
    public GameObject khungESC; 
    
    // ĐÃ ĐỔI TÊN BIẾN ĐỂ KHÔNG BỊ TRÙNG VỚI TÊN CLASS
    public bool isESC_Open; 
    
    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // Vừa vào game là giấu cái bảng đi
        if (khungESC != null) khungESC.SetActive(false);
    }

    // Gắn hàm này vào Nút (Button) UI
    public void BatTatESC()
    {
        isESC_Open = !isESC_Open; // Đảo ngược trạng thái (Đang tắt thành bật, đang bật thành tắt)
        
        if (khungESC != null) 
        {
            khungESC.SetActive(isESC_Open);
        }
        if (isESC_Open)
        {
            Debug.Log("Đang mở bảng lên! Chạy code vẽ item trong Balo ra đây...");
        }
    }
}