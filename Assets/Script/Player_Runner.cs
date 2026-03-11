using Fusion;
using UnityEngine;

public class Player_Runner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField]
    GameObject playerPrefab;
    public void PlayerJoined(PlayerRef player)
    {
        if(player == Runner.LocalPlayer)
        {

            // ĐÚNG: Phải giao sổ đỏ cho 'player'
            Runner.Spawn(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity, player);
        }
    }

}
