using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FarmInteraction : MonoBehaviour
{
    [Header("--- References ---")]
    public Player_Controller myPlayer;
    public LayerMask farmlandLayer;   
    public float interactRange = 3f;

    [Header("--- UI ---")]
    public TextMeshProUGUI hintText;  

    private FarmPlot currentLookedPlot;

    private void Update()
    {
        if (myPlayer == null || myPlayer.Object == null || !myPlayer.Object.HasInputAuthority) 
            return;

        CheckRaycast();
        HandleInput();
    }

    private void CheckRaycast()
    {
        Vector3 diemBatDau = myPlayer.transform.position + Vector3.up * 1.0f; 
        Vector3 huongNhin = myPlayer.transform.forward + (Vector3.down * 0.7f); 

        Ray ray = new Ray(diemBatDau, huongNhin.normalized);
        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, farmlandLayer))
        {
            currentLookedPlot = hit.collider.GetComponentInParent<FarmPlot>();
            
            if (currentLookedPlot != null && hintText != null)
            {
                switch (currentLookedPlot.CurrentState)
                {
                    case FarmPlot.PlotState.Normal: hintText.text = "[F] Cày đất"; break;
                    case FarmPlot.PlotState.Tilled: hintText.text = "[Chuột Phải] Gieo hạt"; break;
                    case FarmPlot.PlotState.Seeded: hintText.text = "Cây đang lớn..."; break;
                    case FarmPlot.PlotState.Grown:  hintText.text = "[E] Thu hoạch"; break;
                }
            }
        }
        else
        {
            currentLookedPlot = null;
            if (hintText != null) hintText.text = ""; 
        }
    }

    private void HandleInput()
    {
        if (currentLookedPlot == null) return;

        // [F] CÀY ĐẤT
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Normal)
                currentLookedPlot.RPC_CayDat();
        }

        // =========================================================
        // [CHUỘT PHẢI] GIEO HẠT - HỆ THỐNG DEBUG
        // =========================================================
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Debug.Log("<color=cyan>>>> [CLIENT - BƯỚC 1]: Bạn vừa bấm CHUỘT PHẢI!</color>");
            
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Tilled)
            {
                Debug.Log("<color=cyan>>>> [CLIENT - BƯỚC 2]: Đất hợp lệ. Đang bắn lệnh RPC_GieoHat lên Server...</color>");
                currentLookedPlot.RPC_GieoHat(); 
            }
            else
            {
                Debug.LogWarning($">>> [CLIENT - LỖI BƯỚC 2]: Không bắn lệnh được! Vì Client thấy trạng thái đất đang là: {currentLookedPlot.CurrentState}");
            }
        }

        // [E] THU HOẠCH
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Grown)
                currentLookedPlot.RPC_ThuHoach(myPlayer.Runner.LocalPlayer);
        }
    }
}