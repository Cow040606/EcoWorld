using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections; // BẮT BUỘC THÊM ĐỂ DÙNG HIỆU ỨNG THỜI GIAN (COROUTINE)

public class ChatSystem : NetworkBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI textMessage;
    public TMP_InputField inputFieldMessage;
    public Button buttonSend;
    public GameObject ChatSys;
    
    // Biến quản lý hiệu ứng làm mờ
    private CanvasGroup chatCanvasGroup;
    private Coroutine fadeCoroutine;

    // Biến tĩnh dùng chung toàn game
    public static bool IsChatting = false;
    
    // Bản sao CỤC BỘ của máy Bò, giúp xử lý tin nhắn từ mọi người mượt mà nhất
    public static ChatSystem LocalInstance;

    public override void Spawned()
    {
        // Yêu cầu của Bò: Bật lại kiểm tra quyền điều khiển
        //if (HasInputAuthority)
        //{
            LocalInstance = this; // Khẳng định: Đây là UI trên màn hình của mình!
            IsChatting = false;

            
            if (ChatSys != null)
            {
                ChatSys.SetActive(true); 

                // TỰ ĐỘNG THÊM CANVAS GROUP (Vũ khí làm mờ UI)
                chatCanvasGroup = ChatSys.GetComponent<CanvasGroup>();
                if (chatCanvasGroup == null) chatCanvasGroup = ChatSys.AddComponent<CanvasGroup>();
            }

            textMessage = GameObject.Find("TextMessage").GetComponent<TextMeshProUGUI>();
            inputFieldMessage = GameObject.Find("InputField Message").GetComponent<TMP_InputField>();
            buttonSend = GameObject.Find("Send").GetComponent<Button>();

            buttonSend.onClick.AddListener(SendMessageChat);

            // Bắt đầu game: Chỉ TẮT Ô NHẬP LIỆU để không che màn hình
            inputFieldMessage.gameObject.SetActive(false);
            buttonSend.gameObject.SetActive(false);
            
            // Ép tàng hình khung chat ngay từ đầu
            if (chatCanvasGroup != null) chatCanvasGroup.alpha = 0f; 

            // Yêu cầu: Tắt ChatSys hoàn toàn khi hết Spawn
            if (ChatSys != null) ChatSys.SetActive(false);
        }
    //}

    void Update()
    {
        // Yêu cầu của Bò: Bật lại kiểm tra quyền
        //if (!HasInputAuthority) return;

        // Bật chat (/)
        if (!IsChatting && Keyboard.current != null && Keyboard.current.slashKey.wasPressedThisFrame)
        {
            OpenChat();
        }
        // Gửi bằng Enter
        else if (IsChatting && Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
        {
            SendMessageChat();
        }
        // Tắt bằng Escape
        else if (IsChatting && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseChat();
        }
    }

    public void SendMessageChat()
    {
        var message = inputFieldMessage.text;
        if (!string.IsNullOrWhiteSpace(message))
        {
            // Yêu cầu: Xác định đúng tên của người chơi nhắn
            string tenHienThi = "Người chơi";
            if (Player_Controller.localPlayer != null)
            {
                Player_Data data = Player_Controller.localPlayer.GetComponent<Player_Data>();
                if (data != null) tenHienThi = data.tenTrenMang.ToString();
            }
            
            // Yêu cầu: Tên màu vàng, nội dung màu trắng
            var text = $"<color=yellow>{tenHienThi}</color>: <color=white>{message}</color>";
            RpcChat(text);
        }
        
        CloseChat();
    }

    void OpenChat()
    {
        IsChatting = true;
        
        // Yêu cầu: Bật hoàn toàn ChatSys khi Open
        if (ChatSys != null) ChatSys.SetActive(true);

        // Sáng 100% ngay lập tức
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 1f;

        // CHỈ HIỆN 2 Ô NHẬP LIỆU
        inputFieldMessage.gameObject.SetActive(true);
        buttonSend.gameObject.SetActive(true);
        inputFieldMessage.ActivateInputField();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseChat()
    {
        IsChatting = false;
        inputFieldMessage.text = ""; 

        // CHỈ ẨN 2 Ô NHẬP LIỆU
        inputFieldMessage.gameObject.SetActive(false);
        buttonSend.gameObject.SetActive(false);
        
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Kích hoạt đồng hồ đếm ngược 4s (Nó sẽ tự tắt ChatSys hoàn toàn sau khi mờ xong)
        WakeUpChatUI();
    }

    // ===============================================
    // PHẦN XỬ LÝ MẠNG VÀ HIỆU ỨNG
    // ===============================================

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RpcChat(string message)
    {
        if (LocalInstance != null && LocalInstance.textMessage != null)
        {
            LocalInstance.textMessage.text += message + "\n";
            LocalInstance.WakeUpChatUI(); // Đánh thức UI sáng lên
        }
    }

    public void WakeUpChatUI()
    {
        // Khi có tin nhắn tới, phải đảm bảo ChatSys đang bật để nhìn thấy
        if (ChatSys != null) ChatSys.SetActive(true);

        // Sáng rực rỡ
        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 1f;

        // Nếu KHÔNG BẬT KHUNG GÕ CHỮ thì mới cho phép đếm ngược mờ đi
        if (!IsChatting)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeOutRoutine());
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        // Đứng chờ 4 giây cho người ta đọc tin nhắn
        yield return new WaitForSeconds(4f);

        float duration = 1.5f; // Thời gian hiệu ứng mờ dần (1.5 giây)
        float time = 0f;

        // Vòng lặp từ từ giảm độ Alpha từ 1 về 0
        while (time < duration)
        {
            time += Time.deltaTime;
            if (chatCanvasGroup != null)
            {
                chatCanvasGroup.alpha = Mathf.Lerp(1f, 0f, time / duration);
            }
            yield return null; 
        }

        if (chatCanvasGroup != null) chatCanvasGroup.alpha = 0f;
        
        // Yêu cầu: Tắt hoàn toàn ChatSys khi Close (Thực hiện sau khi mờ xong)
        if (ChatSys != null) ChatSys.SetActive(false);
    }
}