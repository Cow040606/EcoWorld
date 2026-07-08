using UnityEngine;
using UnityEngine.EventSystems;

// Cần 3 cái Interface này để nhận diện thao tác chuột
public class KeoThaItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Thông tin lúc kéo")]
    public Transform canvasGoc; // Để item bay lơ lửng trên cùng
    private Transform chaCu;    // Nhớ vị trí cũ để lỡ kéo hụt thì quay về
    private CanvasGroup canvasGroup;
    
    [HideInInspector] 
    public int idMonDoDangKeo;  // Sẽ được bơm ID vào lúc mở balo

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        chaCu = transform.parent;
        canvasGoc = GetComponentInParent<Canvas>().rootCanvas.transform;
        transform.SetParent(canvasGoc);
        transform.SetAsLastSibling(); 

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f; 

        // ==========================================
        // DÙNG LIST ĐỂ QUÉT CỰC NHANH
        // ==========================================
        Item thongTin = GetComponent<ItemHover>().thongTinMonDo;
        
        // Chỉ cần chạy vòng lặp qua cuốn sổ điểm danh
        foreach (LoCamDo lo in LoCamDo.danhSachTatCaCacLo)
        {
            if (lo != null) // Check an toàn lỡ ô bị xóa đột ngột
            {
                lo.KiemTraHopLeVaBatSang(thongTin); 
            }
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        // Hình chạy theo chuột
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (transform.parent == canvasGoc)
        {
            transform.SetParent(chaCu);
        }

        // ==========================================
        // THẢ CHUỘT RA THÌ TẮT HẾT ĐÈN TRONG LIST
        // ==========================================
        foreach (LoCamDo lo in LoCamDo.danhSachTatCaCacLo)
        {
            if (lo != null) lo.TatSang();
        }
    }
}