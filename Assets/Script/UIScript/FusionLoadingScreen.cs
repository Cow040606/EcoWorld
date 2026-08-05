using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FusionLoadingScreen : MonoBehaviour
{
    public static FusionLoadingScreen instance;

    [Header("UI References")]
    public GameObject loadingPanel;
    public Slider sliderProgress;
    public TextMeshProUGUI txtStatus;
    public TextMeshProUGUI txtPercent;
    public GameObject btnHuy;

    [Header("Cấu Hình Tốc Độ Chạy % (Smooth)")]
    [Tooltip("Tốc độ đuổi theo target progress khi chuyển giai đoạn")]
    public float baseSmoothSpeed = 0.6f;
    [Tooltip("Tốc độ tự động nhích 1% 2% 3%... liên tục trong lúc đứng chờ mạng")]
    public float minCreepSpeed = 0.08f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private float currentSpeed = 0.6f;
    private bool isErrorState = false;
    private int lastDisplayedPercent = -1;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    private void Start()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
        if (btnHuy != null) btnHuy.SetActive(false);
    }

    private void Update()
    {
        if (loadingPanel != null && loadingPanel.activeSelf && !isErrorState)
        {
            // 1. Tự động tính toán tiến độ tăng dần liên tục không bị khựng
            if (currentProgress < targetProgress)
            {
                // Đuổi theo mốc mục tiêu
                currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.deltaTime * currentSpeed);
            }
            else if (currentProgress < 0.95f)
            {
                // Nếu đã đạt mốc tạm thời nhưng chưa xong 100%, tự động nhích % từ từ (1% 2% 3%...) để người chơi không thấy kẹt
                currentProgress += Time.deltaTime * minCreepSpeed;
                currentProgress = Mathf.Clamp(currentProgress, 0f, 0.95f);
            }

            // 2. Cập nhật Slider
            if (sliderProgress != null)
            {
                sliderProgress.value = currentProgress;
            }

            // 3. Cập nhật chữ % (Chạy từng số 1% 2% 3%...)
            int displayPercent = Mathf.FloorToInt(currentProgress * 100f);
            if (displayPercent != lastDisplayedPercent)
            {
                lastDisplayedPercent = displayPercent;
                if (txtPercent != null)
                {
                    txtPercent.text = displayPercent + "%";
                }
            }
        }
    }

    // Mở màn hình Loading
    public void ShowLoading(string sessionName, string modeName)
    {
        isErrorState = false;
        currentProgress = 0f;
        targetProgress = 0.15f;
        currentSpeed = baseSmoothSpeed;
        lastDisplayedPercent = -1;

        if (sliderProgress != null) sliderProgress.value = 0f;
        if (btnHuy != null) btnHuy.SetActive(false);

        if (txtStatus != null)
        {
            txtStatus.text = $"Đang khởi tạo [{modeName}] phòng: {sessionName}...";
        }

        if (txtPercent != null) txtPercent.text = "0%";

        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    // Cập nhật trạng thái & Tiến độ (0.0 đến 1.0)
    public void UpdateStatus(string statusText, float progress)
    {
        if (isErrorState) return;

        float clamped = Mathf.Clamp01(progress);
        if (clamped > targetProgress)
        {
            targetProgress = clamped;
        }

        // Khi đạt 100% -> tăng tốc chạy nốt % còn lại cực nhanh để chuyển game
        if (clamped >= 1.0f)
        {
            currentSpeed = 2.5f;
        }

        if (txtStatus != null && !string.IsNullOrEmpty(statusText))
        {
            txtStatus.text = statusText;
        }
    }

    // Hiển thị báo lỗi khi kết nối thất bại
    public void ShowError(string errorMessage)
    {
        isErrorState = true;
        if (txtStatus != null)
        {
            txtStatus.text = $"<color=red>Lỗi kết nối:</color> {errorMessage}";
        }

        if (btnHuy != null)
        {
            btnHuy.SetActive(true);
        }
    }

    // Ẩn màn hình Loading
    public void HideLoading()
    {
        isErrorState = false;
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    // Sự kiện khi bấm nút Hủy / Quay lại
    public void BamNut_Huy()
    {
        HideLoading();
    }
}
