using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static QuestManager instance;
    public GameObject khungnhiemvu;
    public bool isQuest_Open; 
    public GameObject txtBangNhiemVu;

    [Header("UI MỚI: Quest Prefab")]
    public GameObject questPrefab;
    public Transform questContent;

    void Awake()
    {
        if (instance == null) instance = this;
    }



    void Start()
    {
        if (khungnhiemvu != null) khungnhiemvu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Battatbangnhiemvu()
    {
        isQuest_Open = !isQuest_Open;
        
        if (khungnhiemvu != null) 
        {
            khungnhiemvu.SetActive(isQuest_Open);
        }
        if (isQuest_Open)
        {
            if (Player_QuestManager.localQuest != null)
            {
                Player_QuestManager.localQuest.KiemTraTienDo();
            }
        }
    }

    public void CapNhatUI_NhiemVu(System.Collections.Generic.List<NhiemVuDangLam> danhSachNhiemVu)
    {
        if (questPrefab == null || questContent == null) return;

        // 1. Xóa sạch danh sách cũ trong Content
        for (int i = questContent.childCount - 1; i >= 0; i--)
        {
            Transform child = questContent.GetChild(i);
            Destroy(child.gameObject);
            child.SetParent(null); // Quan trọng: Gỡ khỏi danh sách ngay lập tức để không bị đè UI
        }

        // 2. Tạo prefab mới cho từng nhiệm vụ
        for (int i = 0; i < danhSachNhiemVu.Count; i++)
        {
            var nv = danhSachNhiemVu[i];
            if (nv.duLieuQuest == null) continue;

            GameObject obj = Instantiate(questPrefab, questContent);
            
            // Tìm Text (TMP) để set chữ
            Transform txtObj = obj.transform.Find("Content/Text (TMP)");
            if (txtObj != null)
            {
                TMPro.TextMeshProUGUI txt = txtObj.GetComponent<TMPro.TextMeshProUGUI>();
                if (txt != null)
                {
                    string dong = $"{nv.duLieuQuest.tenNhiemVu}: {nv.soLuongHienTai}/{nv.duLieuQuest.soLuongCan}";
                    if (nv.daDatYeuCau) dong += " <color=yellow>(Đạt)</color>";
                    txt.text = dong;
                }
            }

            // Quản lý hiển thị icon Active bằng Image.enabled (Tránh bị Animator đè SetActive)
            Transform[] allChildren = obj.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allChildren)
            {
                if (t.name.Contains("SPR_Item_Active")) 
                {
                    var img = t.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) img.enabled = nv.daDatYeuCau; 
                }
            }
        }
    }
}
