using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldSpaceMarkerUI : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public Image iconImage;
    public TextMeshProUGUI distanceText;

    private Vector3 offset;
    private Camera mainCamera;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        mainCamera = Camera.main;
    }

    public void Setup(Transform _target, Sprite _icon, Color _color, Vector3 _offset)
    {
        target = _target;
        offset = _offset;

        if (distanceText == null) 
        {
            Transform distObj = transform.Find("Distance");
            if (distObj != null) distanceText = distObj.GetComponent<TextMeshProUGUI>(); 
        }

        if (iconImage == null) 
        {
            Transform iconObj = transform.Find("Icon");
            if (iconObj != null) iconImage = iconObj.GetComponent<Image>();
        }

        if (iconImage != null)
        {
            if (_icon != null) iconImage.sprite = _icon;
            iconImage.color = _color; // Áp dụng màu sắc
            iconImage.preserveAspect = true; // Chống méo hình
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.position + offset;
        float distance = Vector3.Distance(mainCamera.transform.position, targetPos);
        
        if (distanceText != null)
        {
            distanceText.text = Mathf.RoundToInt(distance) + "m";
        }

        Vector3 screenPos = mainCamera.WorldToScreenPoint(targetPos);

        if (screenPos.z > 0)
        {
            if (iconImage != null && !iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(true);
            if (distanceText != null && !distanceText.gameObject.activeSelf) distanceText.gameObject.SetActive(true);

            rectTransform.position = screenPos;
        }
        else
        {
            if (iconImage != null && iconImage.gameObject.activeSelf) iconImage.gameObject.SetActive(false);
            if (distanceText != null && distanceText.gameObject.activeSelf) distanceText.gameObject.SetActive(false);
        }
    }
}
