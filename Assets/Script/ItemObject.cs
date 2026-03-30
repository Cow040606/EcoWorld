using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [Header("C?u hình v?t ph?m")]
    public int itemID = 1; // ID c?a G? (kh?p v?i file Wood trong Assets)
    public int soLuong = 1;

    // V? m?t vòng tròn xanh trong Editor ?? b?n d? th?y t?m nh?t
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 2f);
    }
}