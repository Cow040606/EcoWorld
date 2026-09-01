using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

public class FusionLoadingScreen : MonoBehaviour
{
    public static FusionLoadingScreen instance;

    [Header("UI References")]
    public GameObject loadingPanel;
    public Slider sliderProgress;
    public TextMeshProUGUI txtStatus;
    public TextMeshProUGUI txtPercent;
    public GameObject btnHuy;

    [Header("Video Loading")]
    public RawImage videoRawImage;
    public VideoPlayer videoPlayer;

    [Header("Cấu Hình Tốc Độ Chạy %")]
    [Tooltip("Tốc độ đuổi theo target progress")]
    public float baseSmoothSpeed = 0.6f;

    [Tooltip("Tốc độ tự động nhích % khi đang chờ")]
    public float minCreepSpeed = 0.08f;

    private float targetProgress = 0f;
    private float currentProgress = 0f;
    private float currentSpeed = 0.6f;

    private bool isErrorState = false;
    private int lastDisplayedPercent = -1;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Không Stop Video ở đây.
        // Video Player sẽ tự phát nếu Play On Awake đang bật.

        if (btnHuy != null)
        {
            btnHuy.SetActive(false);
        }

        if (videoRawImage != null)
        {
            // Video nằm phía sau Button/Text/Slider
            videoRawImage.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (loadingPanel == null || !loadingPanel.activeSelf)
            return;

        if (isErrorState)
            return;

        // ==========================================
        // TĂNG PROGRESS
        // ==========================================

        if (currentProgress < targetProgress)
        {
            currentProgress = Mathf.MoveTowards(
                currentProgress,
                targetProgress,
                Time.deltaTime * currentSpeed
            );
        }
        else if (currentProgress < 0.95f)
        {
            currentProgress += Time.deltaTime * minCreepSpeed;

            currentProgress = Mathf.Clamp(
                currentProgress,
                0f,
                0.95f
            );
        }

        // ==========================================
        // SLIDER
        // ==========================================

        if (sliderProgress != null)
        {
            sliderProgress.value = currentProgress;
        }

        // ==========================================
        // %
        // ==========================================

        int displayPercent =
            Mathf.FloorToInt(currentProgress * 100f);

        if (displayPercent != lastDisplayedPercent)
        {
            lastDisplayedPercent = displayPercent;

            if (txtPercent != null)
            {
                txtPercent.text = displayPercent + "%";
            }
        }
    }

    // =========================================================
    // HIỆN LOADING
    // =========================================================

    public void ShowLoading(string sessionName, string modeName)
    {
        isErrorState = false;

        currentProgress = 0f;
        targetProgress = 0.15f;
        currentSpeed = baseSmoothSpeed;
        lastDisplayedPercent = -1;

        // ==========================================
        // BẬT PANEL
        // ==========================================

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }

        // ==========================================
        // RESET SLIDER
        // ==========================================

        if (sliderProgress != null)
        {
            sliderProgress.value = 0f;
        }

        // ==========================================
        // RESET BUTTON
        // ==========================================

        if (btnHuy != null)
        {
            btnHuy.SetActive(false);
        }

        // ==========================================
        // STATUS
        // ==========================================

        if (txtStatus != null)
        {
            txtStatus.text =
                $"Đang khởi tạo [{modeName}] phòng: {sessionName}...";
        }

        // ==========================================
        // %
        // ==========================================

        if (txtPercent != null)
        {
            txtPercent.text = "0%";
        }

        // ==========================================
        // BẬT VIDEO
        // ==========================================

        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(true);
        }

        PlayLoadingVideo();
    }

    // =========================================================
    // PHÁT VIDEO
    // =========================================================

    private void PlayLoadingVideo()
    {
        if (videoPlayer == null)
        {
            // Debug.LogWarning(
            //    "[FusionLoadingScreen] Video Player chưa được gắn!"
            // );

            return;
        }

        // Đảm bảo dùng Render Texture
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Đảm bảo Target Texture vẫn là LoadingVideo
        // Không cần gán lại bằng code nếu Inspector đã setup đúng.

        // Phát lại từ đầu
        videoPlayer.Stop();

        videoPlayer.time = 0;

        videoPlayer.Play();

        // Debug.Log(
        //    "[FusionLoadingScreen] Loading Video đang phát."
        // );
    }

    // =========================================================
    // CẬP NHẬT STATUS + PROGRESS
    // =========================================================

    public void UpdateStatus(string statusText, float progress)
    {
        if (isErrorState)
            return;

        float clamped = Mathf.Clamp01(progress);

        if (clamped > targetProgress)
        {
            targetProgress = clamped;
        }

        // Khi đạt 100%
        if (clamped >= 1.0f)
        {
            currentSpeed = 2.5f;
        }

        if (txtStatus != null &&
            !string.IsNullOrEmpty(statusText))
        {
            txtStatus.text = statusText;
        }
    }

    // =========================================================
    // HIỂN THỊ LỖI
    // =========================================================

    public void ShowError(string errorMessage)
    {
        isErrorState = true;

        if (txtStatus != null)
        {
            txtStatus.text =
                $"<color=red>Lỗi kết nối:</color> {errorMessage}";
        }

        if (btnHuy != null)
        {
            btnHuy.SetActive(true);
        }

        // Tạm dừng video khi có lỗi
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
        }
    }

    // =========================================================
    // ẨN LOADING
    // =========================================================

    public void HideLoading()
    {
        isErrorState = false;

        // Dừng video khi Loading kết thúc
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        // Ẩn video
        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(false);
        }

        // Ẩn Loading Panel
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    // =========================================================
    // NÚT HỦY
    // =========================================================

    public void BamNut_Huy()
    {
        HideLoading();
    }
}