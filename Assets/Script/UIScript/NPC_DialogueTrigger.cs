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
    public int npcID; 

    [Header("Kéo thả cuộc hội thoại vào đây")]
    public NPCConversation cuocHoiThoaiCuaNPC; 

    [Header("Khoảng cách được phép chat (Mét)")]
    public float tamHoatDong = 5f; 

    [Header("Icon Dấu Chấm Cảm (!) Nhiệm Vụ")]
    public GameObject iconDauChamCam; 

    [Header("Danh Sách UI Cần Ẩn Khi Chạy Cutscene")]
    public List<GameObject> danhSachUIAnKhiChayCutscene = new List<GameObject>();

    [Header("Danh Sách UI Bắt Buộc HIỆN Khi Chạy Cutscene")]
    public List<GameObject> danhSachUIHienKhiChayCutscene = new List<GameObject>();

    [Header("Thời gian ép buộc chạy Cutscene (Nếu = 0: tự động tính)")]
    public float thoiGianCutsceneThuCong = 0f;

    [Header("Danh sách Model NPC cần ẩn khi chạy cutscene")]
    public List<GameObject> danhSachModelNPCCanAn = new List<GameObject>();

    [Header("Danh sách Model NPC cần ẨN LUÔN sau khi Cutscene")]
    public List<GameObject> danhSachModelNPCAnSauCutscene = new List<GameObject>();

    [Header("Danh sách Model NPC cần ẨN LUÔN sau Cutscene thứ 3")]
    public List<GameObject> danhSachModelNPCAnSauCutscene3 = new List<GameObject>();

    [Header("Vị trí dịch chuyển NPC (Tùy chọn)")]
    public Transform viTriDichChuyen;

    private List<Renderer> playerRenderersDaAn = new List<Renderer>();
    private bool dangNóiChuyenVoiNPCNay = false;
    
    // Biến đánh dấu riêng cho cutscene thứ 3
    private bool dangChayCutscene3 = false;

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

            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.HoanThanhNhiemVuNPC(npcID);
            }
        }
    }

    // =========================================================================
    // HÀM MỚI: DÙNG ĐỂ CHẠY RIÊNG TIMELINE THỨ 3 VÀ TỰ ĐỘNG ẨN MODEL
    // =========================================================================
    public void ChayTimelineThu3VaAnModel(PlayableDirector timeline)
    {
        dangChayCutscene3 = true; // Bật cờ đánh dấu
        ChayTimelineCuThe(timeline); // Vẫn gọi hàm chạy Timeline như cũ
    }
    
    public void ChayCutsceneThu3VaAnModel_GameObject(GameObject objCutscene)
    {
        dangChayCutscene3 = true; // Bật cờ đánh dấu
        ChayCutsceneCuThe(objCutscene);
    }

public void ChayTimeline4()
    {
        GameObject tl4 = null;
        // 1. Tìm qua PlayableDirector trong Scene (quét cả các object đang bị Inactive/Ẩn)
        PlayableDirector[] directors = FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var d in directors)
        {
            if (d.gameObject.name.Contains("TIMELINE_04") || d.gameObject.name.Contains("TIME") ||
                (d.transform.parent != null && d.transform.parent.name.Contains("TIMELINE_04")))
            {
                tl4 = (d.transform.parent != null && d.transform.parent.name.Contains("TIMELINE_04")) ? d.transform.parent.gameObject : d.gameObject;
                break;
            }
        }
        // 2. Dự phòng: Tìm qua Root GameObjects của Scene (quét cả Inactive)
        if (tl4 == null)
        {
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in activeScene.GetRootGameObjects())
            {
                if (root.name.Contains("TIMELINE_04") || root.name.Contains("TIME"))
                {
                    tl4 = root;
                    break;
                }
                Transform child = root.transform.Find("TIMELINE_04");
                if (child != null)
                {
                    tl4 = child.gameObject;
                    break;
                }
            }
        }
        // 3. Tiến hành phát cutscene Timeline 4
        if (tl4 != null)
        {
            ChayCutsceneCuThe(tl4);
        }
        else
        {
            Debug.LogError("[NPC_DialogueTrigger] Không tìm thấy đối tượng TIMELINE_04 trong Scene (kể cả trong các object bị ẩn)!");
        }
    }
    // Hàm nhận tham số nếu bạn muốn kéo thả thủ công:
    public void ChayCutsceneThu4_GameObject(GameObject objCutscene)
    {
        ChayCutsceneCuThe(objCutscene);
    }
    public void ChayTimelineThu4(PlayableDirector timeline)
    {
        ChayTimelineCuThe(timeline);
    }
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
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
                director.time = 0;
                director.Play();
            }

            StopAllCoroutines();
            StartCoroutine(TienTrinhTheoDoiCutscene(objCutscene, director));
        }
    }

    public void ChayTimelineCuThe(PlayableDirector timeline)
    {
        if (timeline != null)
        {
            AnDanhSachUI();
            timeline.gameObject.SetActive(true);
            
            timeline.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            timeline.time = 0;
            timeline.Play();

            StopAllCoroutines();
            StartCoroutine(TienTrinhTheoDoiCutscene(timeline.gameObject, timeline));
        }
    }

    public void TatCutsceneCuThe(GameObject objCutscene)
    {
        if (objCutscene != null)
        {
            objCutscene.SetActive(false);
            HienDanhSachUI();
            XuLyAnModelSauCutscene();
        }
    }

    public void DiChuyenNPCDenViTri(GameObject targetObj)
    {
        if (targetObj != null) ThucHienDichChuyen(transform, targetObj.transform.position, targetObj.transform.rotation);
    }

    public void DiChuyenNPCDenTransform(Transform targetTransform)
    {
        if (targetTransform != null) ThucHienDichChuyen(transform, targetTransform.position, targetTransform.rotation);
    }

    public void DiChuyenNPCDenViTriDaChon()
    {
        if (viTriDichChuyen != null) ThucHienDichChuyen(transform, viTriDichChuyen.position, viTriDichChuyen.rotation);
    }

    public void DiChuyenDoiTuong(GameObject doiTuongCanChuyen, GameObject viTriMoi)
    {
        if (doiTuongCanChuyen != null && viTriMoi != null) ThucHienDichChuyen(doiTuongCanChuyen.transform, viTriMoi.transform.position, viTriMoi.transform.rotation);
    }

    public void AnModelSauCutsceneThu3()
    {
        if (danhSachModelNPCAnSauCutscene3 != null && danhSachModelNPCAnSauCutscene3.Count > 0)
        {
            foreach (var model in danhSachModelNPCAnSauCutscene3)
            {
                // LƯU Ý QUAN TRỌNG CHO BẠN: model ở đây phải là MESH/MODEL con, chứ không phải GameObject chứa script này.
                if (model != null)
                {
                    model.SetActive(false);
                }
            }
        }
    }

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

        // =====================================================================
        // KIỂM TRA: NẾU ĐÂY LÀ CUTSCENE 3, ẨN MODEL XONG RỒI TẮT CỜ ĐI
        // =====================================================================
        if (dangChayCutscene3)
        {
            AnModelSauCutsceneThu3();
            dangChayCutscene3 = false;
        }
    }

    private void XuLyAnModelSauCutscene()
    {
        if (danhSachModelNPCAnSauCutscene != null && danhSachModelNPCAnSauCutscene.Count > 0)
        {
            foreach (var model in danhSachModelNPCAnSauCutscene)
            {
                if (model != null) model.SetActive(false);
            }
        }
    }

    private void EpTrangThaiUILienTuc()
    {
        if (danhSachUIAnKhiChayCutscene != null)
        {
            foreach (var ui in danhSachUIAnKhiChayCutscene)
            {
                if (ui != null && ui.activeSelf) ui.SetActive(false);
            }
        }
        
        if (danhSachUIHienKhiChayCutscene != null)
        {
            foreach (var ui in danhSachUIHienKhiChayCutscene)
            {
                if (ui != null && !ui.activeSelf) ui.SetActive(true);
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
            if (r != null) r.enabled = true;
        }
        playerRenderersDaAn.Clear();
    }

    private void EpTrangThaiModelLienTuc()
    {
        if (danhSachModelNPCCanAn != null)
        {
            foreach (var model in danhSachModelNPCCanAn)
            {
                if (model != null && model.activeSelf) model.SetActive(false);
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

        ObjectiveTarget objTarget = GetComponent<ObjectiveTarget>();
        if (objTarget != null)
        {
            if (hienIcon) objTarget.ShowMarker();
            else objTarget.HideMarker();
        }
    }
}