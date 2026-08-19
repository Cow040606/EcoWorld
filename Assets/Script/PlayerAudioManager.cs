using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [Header("Loa của nhân vật")]
    public AudioSource loaNhanVat;

    [Header("Combo Tiếng Chém Riêng (Kéo 3 file âm thanh vào đây)")]
    public AudioClip tiengChem1;
    public AudioClip tiengChem2;
    public AudioClip tiengChem3;

    [Header("Danh Sách Tiếng Chặt Cây (Kéo thả nhiều file vào đây)")]
    public AudioClip[] danhSachTiengChatCay;

    [Header("Tiếng Kỹ Năng Sống Khác")]
    public AudioClip tiengCauCa;
    public AudioClip tiengDaoKhoang;
    public AudioClip tiengChatCay; // Giữ lại dự phòng

    // ===== THÊM 2 ÂM THANH PLAYER =====

    [Header("Âm Thanh Player")]
    public AudioClip tiengNhay;
    public AudioClip tiengBiThuong;

    // --- CÁC HÀM PHÁT TIẾNG CHÉM RIÊNG BIỆT (DÙNG CHO ANIMATION EVENT) ---

    // Cách 1: Chọn hàm này nếu Animation Event truyền tham số (1, 2, 3)
    public void PhatTiengChem(int buocChem)
    {
        if (buocChem == 1 && tiengChem1 != null) loaNhanVat.PlayOneShot(tiengChem1);
        else if (buocChem == 2 && tiengChem2 != null) loaNhanVat.PlayOneShot(tiengChem2);
        else if (buocChem == 3 && tiengChem3 != null) loaNhanVat.PlayOneShot(tiengChem3);
    }

    // Cách 2: Hoặc chọn trực tiếp từng hàm không tham số này trong Animation Event
    public void PhatTiengChem1()
    {
        if (tiengChem1 != null) loaNhanVat.PlayOneShot(tiengChem1);
    }

    public void PhatTiengChem2()
    {
        if (tiengChem2 != null) loaNhanVat.PlayOneShot(tiengChem2);
    }

    public void PhatTiengChem3()
    {
        if (tiengChem3 != null) loaNhanVat.PlayOneShot(tiengChem3);
    }

    // --- HÀM PHÁT TIẾNG CHẶT CÂY (RANDOM 1 TRONG DANH SÁCH) ---
    public void PhatTiengChatCay()
    {
        if (danhSachTiengChatCay != null && danhSachTiengChatCay.Length > 0)
        {
            int rand = Random.Range(0, danhSachTiengChatCay.Length);

            if (danhSachTiengChatCay[rand] != null)
            {
                loaNhanVat.PlayOneShot(danhSachTiengChatCay[rand]);
            }
        }
        else if (tiengChatCay != null)
        {
            loaNhanVat.PlayOneShot(tiengChatCay);
        }
    }

    // --- HÀM LÀM NGHỀ (CHỌN ĐÚNG ÂM THANH ĐỂ PHÁT) ---
    public void PhatTiengHanhDong(string tenHanhDong)
    {
        switch (tenHanhDong)
        {
            case "CauCa":
                if (tiengCauCa != null) loaNhanVat.PlayOneShot(tiengCauCa);
                break;

            case "DaoKhoang":
                if (tiengDaoKhoang != null) loaNhanVat.PlayOneShot(tiengDaoKhoang);
                break;

            default:
                Debug.LogWarning("Chưa có âm thanh cho hành động: " + tenHanhDong);
                break;
        }
    }

    // ===== ÂM THANH PLAYER =====

    // Âm thanh khi Player nhảy
    public void PhatTiengNhay()
    {
        if (tiengNhay != null)
        {
            loaNhanVat.PlayOneShot(tiengNhay);
        }
    }

    // Âm thanh khi Player bị quái vật tấn công / mất máu
    public void PhatTiengBiThuong()
    {
        if (tiengBiThuong != null)
        {
            loaNhanVat.PlayOneShot(tiengBiThuong);
        }
    }
}