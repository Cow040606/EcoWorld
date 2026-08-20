using UnityEngine;

public class PlayerAudioManager : MonoBehaviour
{
    [Header("Loa của nhân vật")]
    public AudioSource loaNhanVat;


    // =========================================================
    //                  ÂM THANH CHÉM
    // =========================================================

    [Header("Combo Tiếng Chém Riêng")]
    public AudioClip tiengChem1;
    public AudioClip tiengChem2;
    public AudioClip tiengChem3;


    // =========================================================
    //                  ÂM THANH CHẶT CÂY
    // =========================================================

    [Header("Danh Sách Tiếng Chặt Cây")]
    public AudioClip[] danhSachTiengChatCay;


    // =========================================================
    //                  ÂM THANH KỸ NĂNG SỐNG
    // =========================================================

    [Header("Tiếng Kỹ Năng Sống Khác")]
    public AudioClip tiengCauCa;
    public AudioClip tiengDaoKhoang;
    public AudioClip tiengChatCay;


    // =========================================================
    //                  ÂM THANH PLAYER
    // =========================================================

    [Header("Âm Thanh Player")]
    public AudioClip tiengNhay;
    public AudioClip tiengBiThuong;


    // =========================================================
    //                  ÂM THANH DI CHUYỂN
    // =========================================================

    [Header("Âm Thanh Walk")]
    public AudioClip tiengWalk;

    [Header("Âm Thanh Run")]
    public AudioClip tiengRun;


    // =========================================================
    //                  PLAYER CONTROLLER
    // =========================================================

    private Player_Controller playerController;


    // =========================================================
    //                  KHỞI TẠO
    // =========================================================

    private void Awake()
    {
        // Lấy Player_Controller trên cùng Player
        playerController = GetComponent<Player_Controller>();

        // Nếu chưa kéo AudioSource vào Inspector
        // thì tự tìm AudioSource trên Player
        if (loaNhanVat == null)
        {
            loaNhanVat = GetComponent<AudioSource>();
        }

        // Kiểm tra AudioSource
        if (loaNhanVat == null)
        {
            Debug.LogWarning(
                "PlayerAudioManager: Player chưa có AudioSource!"
            );
        }
    }


    // =========================================================
    //                  UPDATE
    // =========================================================

    private void Update()
    {
        // Không tìm thấy Player Controller
        if (playerController == null)
            return;


        // =====================================================
        // CHỈ PLAYER LOCAL MỚI PHÁT ÂM THANH
        // =====================================================

        if (!playerController.HasInputAuthority)
        {
            return;
        }


        // =====================================================
        // PLAYER CHẾT
        // =====================================================

        if (playerController.isDead)
        {
            DungTiengDiChuyen();
            return;
        }


        // =====================================================
        // PLAYER ĐỨNG YÊN
        // =====================================================

        if (!playerController.DangDiChuyen)
        {
            DungTiengDiChuyen();
            return;
        }


        // =====================================================
        // PLAYER ĐANG CHẠY
        // =====================================================

        if (playerController.DangChay)
        {
            PhatTiengRun();
        }


        // =====================================================
        // PLAYER ĐANG ĐI BỘ
        // =====================================================

        else
        {
            PhatTiengWalk();
        }
    }


    // =========================================================
    //                  PHÁT TIẾNG WALK
    // =========================================================

    public void PhatTiengWalk()
    {
        if (loaNhanVat == null)
            return;

        if (tiengWalk == null)
            return;


        // Nếu Walk đang phát rồi thì không phát lại
        if (loaNhanVat.isPlaying &&
            loaNhanVat.clip == tiengWalk)
        {
            return;
        }


        // Dừng âm thanh Run nếu đang phát
        if (loaNhanVat.isPlaying)
        {
            loaNhanVat.Stop();
        }


        // Gán âm thanh Walk
        loaNhanVat.clip = tiengWalk;

        // Lặp lại khi hết 14 giây
        loaNhanVat.loop = true;

        // Phát
        loaNhanVat.Play();
    }


    // =========================================================
    //                  PHÁT TIẾNG RUN
    // =========================================================

    public void PhatTiengRun()
    {
        if (loaNhanVat == null)
            return;

        if (tiengRun == null)
            return;


        // Nếu Run đang phát rồi thì không phát lại
        if (loaNhanVat.isPlaying &&
            loaNhanVat.clip == tiengRun)
        {
            return;
        }


        // Dừng âm thanh Walk nếu đang phát
        if (loaNhanVat.isPlaying)
        {
            loaNhanVat.Stop();
        }


        // Gán âm thanh Run
        loaNhanVat.clip = tiengRun;

        // Lặp lại khi hết 14 giây
        loaNhanVat.loop = true;

        // Phát
        loaNhanVat.Play();
    }


    // =========================================================
    //                  DỪNG WALK / RUN
    // =========================================================

    public void DungTiengDiChuyen()
    {
        if (loaNhanVat == null)
            return;


        // Chỉ dừng Walk hoặc Run
        if (loaNhanVat.clip == tiengWalk ||
            loaNhanVat.clip == tiengRun)
        {
            loaNhanVat.Stop();

            loaNhanVat.clip = null;

            loaNhanVat.loop = false;
        }
    }


    // =========================================================
    //                  TIẾNG CHÉM
    // =========================================================

    public void PhatTiengChem(int buocChem)
    {
        if (loaNhanVat == null)
            return;


        if (buocChem == 1 && tiengChem1 != null)
        {
            loaNhanVat.PlayOneShot(tiengChem1);
        }
        else if (buocChem == 2 && tiengChem2 != null)
        {
            loaNhanVat.PlayOneShot(tiengChem2);
        }
        else if (buocChem == 3 && tiengChem3 != null)
        {
            loaNhanVat.PlayOneShot(tiengChem3);
        }
    }


    public void PhatTiengChem1()
    {
        PhatAmThanh(tiengChem1);
    }


    public void PhatTiengChem2()
    {
        PhatAmThanh(tiengChem2);
    }


    public void PhatTiengChem3()
    {
        PhatAmThanh(tiengChem3);
    }


    // =========================================================
    //                  TIẾNG CHẶT CÂY
    // =========================================================

    public void PhatTiengChatCay()
    {
        if (loaNhanVat == null)
            return;


        if (danhSachTiengChatCay != null &&
            danhSachTiengChatCay.Length > 0)
        {
            int rand = Random.Range(
                0,
                danhSachTiengChatCay.Length
            );


            if (danhSachTiengChatCay[rand] != null)
            {
                loaNhanVat.PlayOneShot(
                    danhSachTiengChatCay[rand]
                );
            }
        }
        else
        {
            PhatAmThanh(tiengChatCay);
        }
    }


    // =========================================================
    //                  ÂM THANH HÀNH ĐỘNG
    // =========================================================

    public void PhatTiengHanhDong(string tenHanhDong)
    {
        switch (tenHanhDong)
        {
            case "CauCa":

                PhatAmThanh(tiengCauCa);

                break;


            case "DaoKhoang":

                PhatAmThanh(tiengDaoKhoang);

                break;


            case "ChatCay":

                PhatTiengChatCay();

                break;


            default:

                Debug.LogWarning(
                    "Chưa có âm thanh cho hành động: "
                    + tenHanhDong
                );

                break;
        }
    }


    // =========================================================
    //                  ÂM THANH NHẢY
    // =========================================================

    public void PhatTiengNhay()
    {
        PhatAmThanh(tiengNhay);
    }


    // =========================================================
    //                  ÂM THANH BỊ THƯƠNG
    // =========================================================

    public void PhatTiengBiThuong()
    {
        PhatAmThanh(tiengBiThuong);
    }


    // =========================================================
    //                  PHÁT ÂM THANH ONE SHOT
    // =========================================================

    private void PhatAmThanh(AudioClip amThanh)
    {
        if (loaNhanVat == null)
            return;

        if (amThanh == null)
            return;


        // Âm thanh hành động không ảnh hưởng
        // đến Walk / Run hiện tại
        loaNhanVat.PlayOneShot(amThanh);
    }
}