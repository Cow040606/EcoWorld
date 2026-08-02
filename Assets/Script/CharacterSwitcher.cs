using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterSwitcher : MonoBehaviour
{
    [Header("Cài đặt Model Nhân Vật")]
    public GameObject nhanVatNam;
    public GameObject nhanVatNu;

    [Header("Cài đặt Animator & Avatar")]
    public Animator rootAnimator; // Kéo object Player_Character vào đây
    public Avatar avatarNam;      // Kéo BaseMaleAvatar vào đây
    public Avatar avatarNu;       // Kéo Avatar của nhân vật nữ vào đây

    private bool dangLaNam = true;

    void Start()
    {
        dangLaNam = true;
        CapNhatHienThiNhanVat();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        {
            dangLaNam = !dangLaNam;
            CapNhatHienThiNhanVat();
        }
    }

    private void CapNhatHienThiNhanVat()
    {
        // 1. Bật/Tắt Model
        if (nhanVatNam != null) nhanVatNam.SetActive(dangLaNam);
        if (nhanVatNu != null) nhanVatNu.SetActive(!dangLaNam);

        // 2. Thay đổi Avatar cho khớp với model
        if (rootAnimator != null)
        {
            rootAnimator.avatar = dangLaNam ? avatarNam : avatarNu;
            
            // Rebind() cực kỳ quan trọng: Ép Animator quét lại xương của model mới hiện lên!
            rootAnimator.Rebind(); 
        }
    }
}