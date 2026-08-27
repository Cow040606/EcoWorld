using UnityEngine;

public class Bow_StringController : MonoBehaviour
{
    [Header("--- CÁC ĐIỂM MÓC DÂY CUNG ---")]
    [Tooltip("Điểm mấu trên của cánh cung")]
    public Transform topPoint;

    [Tooltip("Điểm mấu dưới của cánh cung")]
    public Transform bottomPoint;

    [Tooltip("Điểm giữa dây cung (nơi tay phải kéo dây)")]
    public Transform stringPoint;

    [Header("--- THÀNH PHẦN VẼ DÂY (LINE RENDERER) ---")]
    public LineRenderer lineRenderer;
    public float doDayCung = 0.012f;
    public Color mauDayCung = new Color(0.9f, 0.9f, 0.85f, 1f);

    [Header("--- CẤU HÌNH ĐỘ CĂNG LOCAL ---")]
    [Tooltip("Khoảng cách dây bị kéo lùi về sau theo trục Local của cung khi không dùng Transform tay")]
    public Vector3 huongKeoDayLocal = new Vector3(0f, 0f, -0.4f);

    private Vector3 viTriDayNghiLocal;
    private bool daKhoiTao = false;

    private void Awake()
    {
        KhoiTaoLineRenderer();
    }

    private void KhoiTaoLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null) lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.positionCount = 3;
        lineRenderer.startWidth = doDayCung;
        lineRenderer.endWidth = doDayCung;
        lineRenderer.useWorldSpace = true;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Tạo material mặc định nếu chưa có
        if (lineRenderer.sharedMaterial == null)
        {
            Shader defaultShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (defaultShader == null) defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null)
            {
                lineRenderer.material = new Material(defaultShader);
                lineRenderer.material.color = mauDayCung;
            }
        }

        if (stringPoint != null)
        {
            viTriDayNghiLocal = stringPoint.localPosition;
            daKhoiTao = true;
        }
    }

    private void LateUpdate()
    {
        if (topPoint == null || bottomPoint == null) return;

        if (lineRenderer == null) KhoiTaoLineRenderer();

        Vector3 pTop = topPoint.position;
        Vector3 pBottom = bottomPoint.position;
        Vector3 pMiddle = (stringPoint != null) ? stringPoint.position : ((pTop + pBottom) * 0.5f);

        lineRenderer.SetPosition(0, pTop);
        lineRenderer.SetPosition(1, pMiddle);
        lineRenderer.SetPosition(2, pBottom);
    }

    /// <summary>
    /// Điều khiển độ căng dây cung (tension từ 0 đến 1)
    /// </summary>
    /// <param name="tension">0 = trạng thái nghỉ, 1 = kéo căng hết cỡ</param>
    /// <param name="tayPhaiTransform">Vị trí tay phải kéo dây (nếu có)</param>
    public void SetStringTension(float tension, Transform tayPhaiTransform = null)
    {
        if (stringPoint == null) return;
        if (!daKhoiTao)
        {
            viTriDayNghiLocal = stringPoint.localPosition;
            daKhoiTao = true;
        }

        if (tension > 0.05f && tayPhaiTransform != null)
        {
            // Điểm giữa dây cung bám theo bàn tay phải
            stringPoint.position = tayPhaiTransform.position;
        }
        else
        {
            // Dây cung dịch chuyển lùi dần theo trục local
            stringPoint.localPosition = Vector3.Lerp(viTriDayNghiLocal, viTriDayNghiLocal + huongKeoDayLocal, tension);
        }
    }
}
