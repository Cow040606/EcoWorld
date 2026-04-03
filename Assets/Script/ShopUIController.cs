using Fusion;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;
using Fusion.Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class ShopUIController : MonoBehaviour
{
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
    public void Sell_Button()
    {
        Click_BanVatPham(2, 10);
    }
}