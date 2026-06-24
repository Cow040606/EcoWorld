using UnityEngine;

[CreateAssetMenu(fileName = "Items", menuName = "Items/Itemcreate")]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    public string Tag;
    public Sprite icon;   
    public bool stackable = true;
    public int value;

    [Header("Mô tả chi tiết")]
    [TextArea(3, 10)] 
    public string description;

    [Header("Mô hình 3D khi cầm trên tay")]
    public GameObject model3DPrefab;

    [Header("Tùy chỉnh Kích thước (X, Y, Z)")]
    // Vector3.one mặc định là (1, 1, 1) để model không bị teo nhỏ
    public Vector3 scaleTrenTay = Vector3.one; 
}