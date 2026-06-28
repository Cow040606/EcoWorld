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
    public Transform cameraTransform; 

    private FarmPlot currentLookedPlot;

    private void Start()
    {
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
                hintText.transform.position = currentLookedPlot.transform.position + (Vector3.up * 1.5f);

                if (cameraTransform != null)
                {
                    hintText.transform.rotation = Quaternion.LookRotation(hintText.transform.position - cameraTransform.position);
                }

                switch (currentLookedPlot.CurrentState)
                {
                    case FarmPlot.PlotState.DatTrong: hintText.text = "[Chuột Phải] Gieo hạt"; break;
                    case FarmPlot.PlotState.CayCon:   hintText.text = "Cây đang lớn..."; break;
                    case FarmPlot.PlotState.CayLon:   hintText.text = "[E] Thu hoạch"; break;
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

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // Chỉ cần 1 Click Chuột Phải là Gieo Hạt (không cần cày cuốc nữa)
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.DatTrong)
                currentLookedPlot.RPC_GieoHat(); 
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentLookedPlot.CurrentState == FarmPlot.PlotState.CayLon)
                currentLookedPlot.RPC_ThuHoach(myPlayer.Runner.LocalPlayer);
        }
    }
}