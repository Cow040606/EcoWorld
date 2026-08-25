using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CompassController : MonoBehaviour
{
    public static CompassController Instance;

    [Header("References")]
    public Transform playerTransform; 
    public RectTransform compassContent;

    [Header("Settings")]
    public float compassUnit = 14.666f; 
    public float northOffset = 3160f; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            if (Camera.main != null) playerTransform = Camera.main.transform;
            else return; 
        }

        float playerAngle = playerTransform.eulerAngles.y;

        // BƯỚC NGOẶT QUAN TRỌNG: 
        // Trong Unity, eulerAngles.y chạy từ 0 đến 360.
        // Nhưng dải UI của bạn được sắp xếp: SOUTH -> WEST -> NORTH -> EAST
        // Nghĩa là WEST nằm bên TRÁI North (góc âm). 
        // Nên ta phải đổi playerAngle thành từ -180 đến 180!
        if (playerAngle > 180f)
        {
            playerAngle -= 360f;
        }

        if (compassContent != null)
        {
            compassContent.anchoredPosition = new Vector2(-playerAngle * compassUnit - northOffset, compassContent.anchoredPosition.y);
        }
    }

    public float GetCompassPositionX(Vector3 targetWorldPosition)
    {
        if (playerTransform == null) return 0f;

        Vector3 dirToTarget = targetWorldPosition - playerTransform.position;
        dirToTarget.y = 0;

        // Lấy góc tuyệt đối (-180 đến 180)
        float targetWorldAngle = Vector3.SignedAngle(Vector3.forward, dirToTarget, Vector3.up);

        // Đặt Icon thẳng vào tọa độ trên dải UI
        return targetWorldAngle * compassUnit + northOffset;
    }
}
