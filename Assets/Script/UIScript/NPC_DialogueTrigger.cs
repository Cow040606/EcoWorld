using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DialogueEditor; 
using UnityEngine.InputSystem; 
using UnityEngine.Playables; 
using UnityEngine.AI;

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
    // CODE: Danh sách Model sẽ bị ẨN LUÔN sau khi Cutscene (chung) kết thúc
    // =========================================================================
    [Header("Danh sách Model NPC cần ẨN LUÔN sau khi Cutscene")]
    public List<GameObject> danhSachModelNPCAnSauCutscene = new List<GameObject>();

    // =========================================================================
    // CODE MỚI THÊM: Danh sách Model ẨN LUÔN dành riêng cho Cutscene thứ 3
    // =========================================================================
    [Header("Danh sách Model NPC cần ẨN LUÔN sau Cutscene thứ 3")]
    public List<GameObject> danhSachModelNPCAnSauCutscene3 = new List<GameObject>();

    [Header("Vị trí dịch chuyển NPC (Tùy chọn)")]
    public Transform viTriDichChuyen;

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

                        // Tự động cập nhật thông số cho tất cả nhiệm vụ khi bắt đầu đối thoại
                        NPC_QuestBridge bridge = FindFirstObjectByType<NPC_QuestBridge>();
                        if (bridge != null)
                        {
                            bridge.CapNhatTatCaThongSoQuest();
                        }
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
            // Debug.Log($"<color=green>[NPC Cutscene]:</color> Bắt đầu phát Cutscene: {objCutscene.name}");
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
            // Debug.Log($"<color=green>[NPC Cutscene]:</color> Đã phát Timeline Cutscene: {timeline.name}");
        }
    }

    // 3. Ẩn/Tắt 1 GameObject Cutscene CỤ THỂ bất kỳ thủ công
    public void TatCutsceneCuThe(GameObject objCutscene)
    {
        if (objCutscene != null)
        {
            objCutscene.SetActive(false);
            HienDanhSachUI();
            
            // Ẩn các model sau khi tắt thủ công
            XuLyAnModelSauCutscene();
            
            // Debug.Log($"<color=yellow>[NPC Cutscene]:</color> Đã tắt Cutscene GameObject: {objCutscene.name}");
        }
    }

    // =========================================================================
    // CÁC HÀM DỊCH CHUYỂN NPC ĐẾN VỊ TRÍ MỤC TIÊU
    // =========================================================================

    // 4. Dịch chuyển NPC này đến 1 GameObject vị trí cụ thể
    public void DiChuyenNPCDenViTri(GameObject targetObj)
    {
        if (targetObj != null)
        {
            ThucHienDichChuyen(transform, targetObj.transform.position, targetObj.transform.rotation);
        }
    }

    // 5. Dịch chuyển NPC này đến 1 Transform vị trí cụ thể
    public void DiChuyenNPCDenTransform(Transform targetTransform)
    {
        if (targetTransform != null)
        {
            ThucHienDichChuyen(transform, targetTransform.position, targetTransform.rotation);
        }
    }

    // 6. Dịch chuyển NPC này đến vị trí đã kéo sẵn trong Inspector (viTriDichChuyen)
    public void DiChuyenNPCDenViTriDaChon()
    {
        if (viTriDichChuyen != null)
        {
            ThucHienDichChuyen(transform, viTriDichChuyen.position, viTriDichChuyen.rotation);
        }
    }

    // 7. Dịch chuyển một đối tượng bất kỳ đến vị trí của một GameObject khác
    public void DiChuyenDoiTuong(GameObject doiTuongCanChuyen, GameObject viTriMoi)
    {
        if (doiTuongCanChuyen != null && viTriMoi != null)
        {
            ThucHienDichChuyen(doiTuongCanChuyen.transform, viTriMoi.transform.position, viTriMoi.transform.rotation);
        }
    }

    // =========================================================================
    // CODE MỚI THÊM: HÀM GỌI ĐỂ ẨN MODEL CHO CUTSCENE 3 (GẮN VÀO EVENT DIALOGUE)
    // =========================================================================
    public void AnModelSauCutsceneThu3()
    {
        if (danhSachModelNPCAnSauCutscene3 != null && danhSachModelNPCAnSauCutscene3.Count > 0)
        {
            foreach (var model in danhSachModelNPCAnSauCutscene3)
            {
                if (model != null)
                {
                    model.SetActive(false);
                }
            }
        }
    }

    // Hàm phụ trợ thực hiện di chuyển: Hỗ trợ an toàn cho NavMeshAgent, CharacterController và Rigidbody
    private void ThucHienDichChuyen(Transform doiTuong, Vector3 viTriMoi, Quaternion gocXoayMoi)
    {
        if (doiTuong == null) return;

        NavMeshAgent agent = doiTuong.GetComponent<NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.Warp(viTriMoi);
        }
        else
        {
            CharacterController cc = doiTuong.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                doiTuong.position = viTriMoi;
                cc.enabled = true;
            }
            else
            {
                doiTuong.position = viTriMoi;
            }
        }

        doiTuong.rotation = gocXoayMoi;

        Rigidbody rb = doiTuong.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
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

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator TienTrinhTheoDoiCutscene(GameObject objCutscene, PlayableDirector director)
    {
        yield return null; 

        AnDanhSachUI();

        if (thoiGianCutsceneThuCong > 0f)
        {
            float timer = 0f;
            while (objCutscene != null && objCutscene.activeSelf && timer < thoiGianCutsceneThuCong)
            {
                timer += Time.unscaledDeltaTime;
                EpTrangThaiUILienTuc();
                isCutsceneActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                yield return null;
            }
        }
        else if (director != null)
        {
            while (director != null && objCutscene != null && objCutscene.activeSelf && director.state == PlayState.Playing)
            {
                EpTrangThaiUILienTuc();
                isCutsceneActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                yield return null;
            }
        }
        else
        {
            float timer = 0f;
            while (objCutscene != null && objCutscene.activeSelf && timer < 5f)
            {
                timer += Time.unscaledDeltaTime;
                EpTrangThaiUILienTuc();
                isCutsceneActive = true;
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                yield return null;
            }
        }

        if (objCutscene != null)
        {
            objCutscene.SetActive(false);
        }
        
        DialogueUI dialogueUI = FindFirstObjectByType<DialogueUI>();
        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
        }

        HienDanhSachUI();
        XuLyAnModelSauCutscene();
    }

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

        if (Time.frameCount % 10 == 0)
        {
            Player_Controller[] players = FindObjectsOfType<Player_Controller>();
            foreach (var p in players)
            {
                Renderer[] renderers = p.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    if (r.enabled && !playerRenderersDaAn.Contains(r))
                    {
                        r.enabled = false;
                        playerRenderersDaAn.Add(r);
                    }
                }
            }
        }
    }

    public void CapNhatIconNhiemVu(bool hienIcon)
    {
        if (iconDauChamCam != null)
        {
            iconDauChamCam.SetActive(hienIcon);
        }
    }
}