using Fusion;
using UnityEngine;

public class TuHuyGianGiao : MonoBehaviour
{
    void Awake()
    {
        // Tìm xem trong game hiện tại có thằng NetworkRunner nào đang sống không?
        NetworkRunner[] cacRunner = FindObjectsOfType<NetworkRunner>();

        // Nếu có ít nhất 1 thằng Runner (Tức là nó vừa bay từ Menu qua đây)
        if (cacRunner.Length > 0)
        {
            Debug.Log("<color=red>Giàn giáo Test:</color> Thấy Chủ tịch từ Menu xuống, tôi xin phép tự hủy để nhường chỗ!");
            
            // Xóa ngay lập tức cái cục [TEST_FUSION] này đi
            Destroy(this.gameObject); 
        }
        else
        {
            Debug.Log("<color=green>Giàn giáo Test:</color> Không có ai ở đây cả, để tôi tự tạo Server test cho Bò!");
        }
    }
}