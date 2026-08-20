using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor; 
using UnityEngine.InputSystem; 
using UnityEngine.Playables; 

public class NPC_DialogueTrigger : MonoBehaviour
{
    [Header("Định danh NPC")]
    public int npcID; // ID NPC (khớp với targetID trong QuestSO)

    [Header("Kéo thả cuộc hội thoại vào đây")]
    public NPCConversation cuocHoiThoaiCuaNPC; 

    [Header("Khoảng cách được phép chat (Mét)")]
    public float tamHoatDong = 5f; 

    [Header("Icon Dấu Chấm Cảm (!) Nhiệm Vụ")]
    public GameObject iconDauChamCam; // GameObject icon ! trên đầu NPC hoặc trên Map

    [Header("Danh Sách UI Cần Ẩn Khi Chạy Cutscene")]
    public List<GameObject> danhSachUIAnKhiChayCutscene = new List<GameObject>();

    [Header("Danh Sách UI Bắt Buộc HIỆN Khi Chạy Cutscene")]
    public List<GameObject> danhSachUIHienKhiChayCutscene = new List<GameObject>();

    [Header("Thời gian ép buộc chạy Cutscene (Nếu = 0: tự động tính)")]
    public float thoiGianCutsceneThuCong = 0f;

    [Header("Danh sách Model NPC cần ẩn khi chạy cutscene")]
    public List<GameObject> danhSachModelNPCCanAn = new List<GameObject>();

    // =========================================================================
    // CODE MỚI THÊM: Danh sách Model sẽ bị ẨN LUÔN sau khi Cutscene kết thúc
    // =========================================================================
    [Header("Danh sách Model NPC cần ẨN LUÔN sau khi Cutscene")]
    public List<GameObject> danhSachModelNPCAnSauCutscene = new List<GameObject>();

    private List<Renderer> playerRenderersDaAn = new List<Renderer>();

    private bool dangNóiChuyenVoiNPCNay = false;

    private void OnEnable()
    {
        ConversationManager.OnConversationEnded += KhiKetThucHoiThoai;
    }

    private void OnDisable()
    {
        ConversationManager.OnConversationEnded -= KhiKetThucHoiThoai;
    }

    private void Start()
    {
        // Ẩn icon ban đầu
        CapNhatIconNhiemVu(false);
    }

    private void Update()
    {
        if (Player_Controller.localPlayer == null) return;
        if (ShopUIController.instance != null && ShopUIController.instance.isShopOpen) return; 
        if (InventoryManager.instance != null && InventoryManager.instance.trangThaiBalo) return;
        if (QuestManager.instance != null && QuestManager.instance.isQuest_Open) return;

        float khoangCach = Vector3.Distance(transform.position, Player_Controller.localPlayer.transform.position);

        if (khoangCach <= tamHoatDong)
        {
            if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            {
                if (cuocHoiThoaiCuaNPC != null && ConversationManager.Instance != null)
                {
                    if (!ConversationManager.Instance.IsConversationActive)
                    {
                        dangNóiChuyenVoiNPCNay = true;
                        ConversationManager.Instance.StartConversation(cuocHoiThoaiCuaNPC);
                    }
                }
            }
        }
    }

    private void KhiKetThucHoiThoai()
    {
        if (dangNóiChuyenVoiNPCNay)
        {
            dangNóiChuyenVoiNPCNay = false;

            // Báo cho QuestManager hoàn thành nhiệm vụ và tự nhận thưởng!
            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.HoanThanhNhiemVuNPC(npcID);
            }
        }
    }

    // =========================================================================
    // CÁC HÀM DÙNG GẮN VÀO EVENT TRONG DIALOGUE EDITOR (OPTION NODE / SPEECH NODE)
    // =========================================================================

    // 1. Chạy 1 GameObject Cutscene CỤ THỂ (Tự ẩn UI, hiện chuột, chạy xong tự tắt Cutscene & hiện lại UI)
    public void ChayCutsceneCuThe(GameObject objCutscene)
    {
        if (objCutscene != null)
        {
            AnDanhSachUI();
            objCutscene.SetActive(true);
            
            PlayableDirector director = objCutscene.GetComponent<PlayableDirector>();
            if (director == null) director = objCutscene.GetComponentInChildren<PlayableDirector>();

            if (director != null)
            {
                // QUAN TRỌNG: Đảm bảo Timeline vẫn chạy mượt mà ngay cả khi Hội thoại (Dialogue Editor) làm dừng game (Time.timeScale = 0)
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                director.time = 0;
                director.Play();
            }

            StopAllCoroutines();
            StartCoroutine(TienTrinhTheoDoiCutscene(objCutscene, director));
            Debug.Log($"<color=green>[NPC Cutscene]:</color> Bắt đầu phát Cutscene: {objCutscene.name}");
        }
    }

    // 2. Phát 1 Timeline PlayableDirector CỤ THỂ
    public void ChayTimelineCuThe(PlayableDirector timeline)
    {
        if (timeline != null)
        {
            AnDanhSachUI();
            timeline.gameObject.SetActive(true);
            
            // Cấu hình UnscaledGameTime để không bị đóng băng
            timeline.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            timeline.time = 0;
            timeline.Play();

            StopAllCoroutines();
            StartCoroutine(TienTrinhTheoDoiCutscene(timeline.gameObject, timeline));
            Debug.Log($"<color=green>[NPC Cutscene]:</color> Đã phát Timeline Cutscene: {timeline.name}");
        }
    }

    // 3. Ẩn/Tắt 1 GameObject Cutscene CỤ THỂ bất kỳ thủ công
    public void TatCutsceneCuThe(GameObject objCutscene)
    {
        if (objCutscene != null)
        {
            objCutscene.SetActive(false);
            HienDanhSachUI();
            
            // CODE MỚI THÊM: Ẩn các model sau khi tắt thủ công
            XuLyAnModelSauCutscene();
            
            Debug.Log($"<color=yellow>[NPC Cutscene]:</color> Đã tắt Cutscene GameObject: {objCutscene.name}");
        }
    }

    public static bool isCutsceneActive = false;

    // --- HÀM ẨN / HIỆN UI & QUẢN LÝ CHUỘT ---
    public void AnDanhSachUI()
    {
        isCutsceneActive = true;

        if (danhSachUIAnKhiChayCutscene != null && danhSachUIAnKhiChayCutscene.Count > 0)
        {
            foreach (var ui in danhSachUIAnKhiChayCutscene)
            {
                if (ui != null) ui.SetActive(false);
            }
        }
        
        if (danhSachUIHienKhiChayCutscene != null && danhSachUIHienKhiChayCutscene.Count > 0)
        {
            foreach (var ui in danhSachUIHienKhiChayCutscene)
            {
                if (ui != null) ui.SetActive(true);
            }
        }

        AnModelNhanVat();

        // Ép chuột hiển thị
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HienDanhSachUI()
    {
        isCutsceneActive = false;

        if (danhSachUIAnKhiChayCutscene != null)
        {
            foreach (var ui in danhSachUIAnKhiChayCutscene)
            {
                if (ui != null) ui.SetActive(true);
            }
        }
        
        if (danhSachUIHienKhiChayCutscene != null)
        {
            foreach (var ui in danhSachUIHienKhiChayCutscene)
            {
                if (ui != null) ui.SetActive(false);
            }
        }
        
        HienModelNhanVat();

        // Khóa lại chuột khi xong Cutscene
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator TienTrinhTheoDoiCutscene(GameObject objCutscene, PlayableDirector director)
    {
        // Đợi 1 frame để Timeline bắt đầu Play()
        yield return null; 

        // Ẩn danh sách UI lần đầu
        AnDanhSachUI();

        // NẾU NGƯỜI DÙNG NHẬP THỜI GIAN THỦ CÔNG (> 0)
        if (thoiGianCutsceneThuCong > 0f)
        {
            float timer = 0f;
            while (objCutscene != null && objCutscene.activeSelf && timer < thoiGianCutsceneThuCong)
            {
                timer += Time.unscaledDeltaTime;
                
                // ÉP TRẠNG THÁI UI LIÊN TỤC MỖI FRAME
                EpTrangThaiUILienTuc();

                isCutsceneActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                yield return null;
            }
        }
        // NẾU DÙNG TỰ ĐỘNG BẰNG TIMELINE
        else if (director != null)
        {
            while (director != null && objCutscene != null && objCutscene.activeSelf && director.state == PlayState.Playing)
            {
                // ÉP TRẠNG THÁI UI LIÊN TỤC MỖI FRAME
                EpTrangThaiUILienTuc();

                isCutsceneActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                yield return null;
            }
        }
        // DỰ PHÒNG NẾU KHÔNG CÓ TIMELINE MÀ CŨNG KHÔNG NHẬP SỐ GIÂY (Mặc định 5s)
        else
        {
            float timer = 0f;
            while (objCutscene != null && objCutscene.activeSelf && timer < 5f)
            {
                timer += Time.unscaledDeltaTime;
                
                // ÉP TRẠNG THÁI UI LIÊN TỤC MỖI FRAME
                EpTrangThaiUILienTuc();

                isCutsceneActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                yield return null;
            }
        }

        // Tự động tắt Cutscene khi hoàn thành
        if (objCutscene != null)
        {
            objCutscene.SetActive(false);
        }
        
        // Gọi hàm HideDialogue() để ẩn panel UI của DialogueUI.cs
        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
        }

        HienDanhSachUI();

        // =========================================================================
        // CODE MỚI THÊM: Gọi hàm ẩn danh sách Model vĩnh viễn sau khi Cutscene chạy xong
        // =========================================================================
        XuLyAnModelSauCutscene();

        Debug.Log($"<color=green>[NPC Cutscene]:</color> Đã hoàn thành Cutscene. Tự tắt Cutscene {objCutscene?.name}, hiện lại UI và ẩn chuột.");
    }

    // =========================================================================
    // CODE MỚI THÊM: Hàm duyệt qua danh sách và tắt các model
    // =========================================================================
    private void XuLyAnModelSauCutscene()
    {
        if (danhSachModelNPCAnSauCutscene != null && danhSachModelNPCAnSauCutscene.Count > 0)
        {
            foreach (var model in danhSachModelNPCAnSauCutscene)
            {
                if (model != null)
                {
                    model.SetActive(false);
                }
            }
        }
    }

    private void EpTrangThaiUILienTuc()
    {
        // 1. Ép Ẩn
        if (danhSachUIAnKhiChayCutscene != null)
        {
            foreach (var ui in danhSachUIAnKhiChayCutscene)
            {
                if (ui != null && ui.activeSelf) 
                {
                    ui.SetActive(false);
                }
            }
        }
        
        // 2. Ép Hiện
        if (danhSachUIHienKhiChayCutscene != null)
        {
            foreach (var ui in danhSachUIHienKhiChayCutscene)
            {
                if (ui != null && !ui.activeSelf) 
                {
                    ui.SetActive(true);
                }
            }
        }

        EpTrangThaiModelLienTuc();
    }

    private void AnModelNhanVat()
    {
        if (danhSachModelNPCCanAn != null)
        {
            foreach (var model in danhSachModelNPCCanAn)
            {
                if (model != null) model.SetActive(false);
            }
        }

        // Tìm tất cả Player hiện có để ẩn model
        Player_Controller[] players = FindObjectsOfType<Player_Controller>();
        foreach (var p in players)
        {
            Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    playerRenderersDaAn.Add(r);
                }
            }
        }
    }

    private void HienModelNhanVat()
    {
        if (danhSachModelNPCCanAn != null)
        {
            foreach (var model in danhSachModelNPCCanAn)
            {
                if (model != null) model.SetActive(true);
            }
        }

        // Hiện lại tất cả model player đã ẩn
        foreach (var r in playerRenderersDaAn)
        {
            if (r != null)
            {
                r.enabled = true;
            }
        }
        playerRenderersDaAn.Clear();
    }

    private void EpTrangThaiModelLienTuc()
    {
        // Ép ẩn NPC Model liên tục
        if (danhSachModelNPCCanAn != null)
        {
            foreach (var model in danhSachModelNPCCanAn)
            {
                if (model != null && model.activeSelf)
                {
                    model.SetActive(false);
                }
            }
        }

        // Quét tìm Player mới sau mỗi 10 frame để tối ưu hiệu suất (nếu người chơi mới vào thế giới khi đang có cutscene)
        if (Time.frameCount % 10 == 0)
        {
            Player_Controller[] players = FindObjectsOfType<Player_Controller>();
            foreach (var p in players)
            {
                Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    // Nếu renderer đang bật và chưa có trong danh sách đã ẩn
                    if (r.enabled && !playerRenderersDaAn.Contains(r))
                    {
                        r.enabled = false;
                        playerRenderersDaAn.Add(r);
                    }
                }
            }
        }
    }

    // Hàm bật / ẩn Icon dấu chấm cảm !
    public void CapNhatIconNhiemVu(bool hienIcon)
    {
        if (iconDauChamCam != null)
        {
            iconDauChamCam.SetActive(hienIcon);
        }
    }
}