using UnityEngine;
using Fusion;
public class XuLyItem : NetworkBehaviour
{
    public Item thongTinDoVat;
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Destroy(gameObject); 
    }
}