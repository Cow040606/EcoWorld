using UnityEngine;

// Lệnh này giúp Bò click chuột phải tạo món đồ nhanh
[CreateAssetMenu(fileName = "Items", menuName = "Items/Itemcreate")]
public class Item : ScriptableObject
{
    public int itemID;
    public string itemName;
    public Sprite icon;   
    public bool stackable = true;
    public int value;
}