using UnityEngine;
using Fusion;

public class ShopUIController : MonoBehaviour
{
    // PH?I CÓ CH? PUBLIC ? ?ÂY
    public void Click_BanVatPham(int id, int gia)
    {
        NetworkRunner runner = NetworkRunner.Instances[0];
        if (runner != null)
        {
            NetworkObject localPlayerObj = runner.GetPlayerObject(runner.LocalPlayer);
            if (localPlayerObj != null)
            {
                Player_Controller playerScript = localPlayerObj.GetComponent<Player_Controller>();
                if (playerScript != null)
                {
                    playerScript.RPC_BanVatPham(id, gia);
                }
            }
        }
    }
}