using UnityEngine;

public enum ItemType { Normal, Medium, Health }

public class ItemObject : MonoBehaviour
{
    [Header("C?u hình v?t ph?m")]
    public ItemType loaiItem; // Ch?n lo?i trong Inspector
    public int itemID = 1;
    public int soLuong = 1;
    public string tenVatPham;

    // T? ??ng c?p nh?t Tag d?a trên lo?i ?ã ch?n ?? tránh sai sót th? công
    private void OnValidate()
    {
        switch (loaiItem)
        {
            case ItemType.Normal:
                gameObject.tag = "Normal_Item";
                break;
            case ItemType.Medium:
                gameObject.tag = "Medium_Item";
                break;
            case ItemType.Health:
                gameObject.tag = "Health_Item";
                break;
        }
    }

    // V? vòng tròn xanh ?? d? quan sát trong Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}