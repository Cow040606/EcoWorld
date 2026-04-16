using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class FarmInteraction : MonoBehaviour
{
    [Header("--- References ---")]
    public Player_Controller myPlayer;
    public LayerMask farmlandLayer;   
    public float interactRange = 4f; 

    [Header("--- UI 3D Lơ Lửng ---")]
    public TextMeshProUGUI hintText;  
    [Tooltip("Kéo Main Camera vào đây để chữ luôn xoay mặt về phía người chơi")]
    public Transform cameraTransform; 

    private FarmPlot currentLookedPlot;

    private void Start()
    {
        // Ẩn chữ đi khi mới vào game
        if (hintText != null) hintText.text = ""; 
    }

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

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, farmlandLayer))
        {
            currentLookedPlot = hit.collider.GetComponentInParent<FarmPlot>();
            
            if (currentLookedPlot != null && hintText != null)
            {
                // 1. DỜI CHỮ ĐẾN CỤC ĐẤT (Cách mặt đất 1.5 unit)
                hintText.transform.position = currentLookedPlot.transform.position + (Vector3.up * 1.5f);

                // 2. XOAY CHỮ VỀ PHÍA CAMERA (Để chữ không bị ngược)
                if (cameraTransform != null)
                {
                    hintText.transform.rotation = Quaternion.LookRotation(hintText.transform.position - cameraTransform.position);
                }

                // 3. HIỂN THỊ NỘI DUNG TƯƠNG ỨNG
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

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Normal)
                currentLookedPlot.RPC_CayDat();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Tilled)
                currentLookedPlot.RPC_GieoHat(); 
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.Grown)
                currentLookedPlot.RPC_ThuHoach(myPlayer.Runner.LocalPlayer);
        }
    }
}