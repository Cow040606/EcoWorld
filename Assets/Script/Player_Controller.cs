using UnityEngine;
using Fusion;
using System.Collections.Generic;
using UnityEngine.InputSystem;
// public struct DuLieuInput : INetworkInput 
// {
//     public Vector2 moveInput;
//     public NetworkBool BamE;
// }
public class Player_Controller : NetworkBehaviour
{
    [SerializeField] 
    [Header("Di chuyển")]
    CharacterController character;
    private Vector2 moveInputLocal;
    public float speed = 5f;
    [Header("Nhặt vật phẩm")]
    
    public float banKinhNhat = 5f;
    private bool NutE = false;
    


    public override void Spawned()
    {
        // 1. Dòng này sẽ báo cáo xem nhân vật có được đẻ ra không và ai là chủ
        Debug.Log($"<color=cyan>Đã đẻ Player! Quyền điều khiển (Input Authority): {HasInputAuthority}</color>");

        if (HasInputAuthority == false)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.enabled = false;
            
            AudioListener listener = GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = false;
        }
    }
    public override void FixedUpdateNetwork()
    {

            Vector3 move = moveInputLocal.x * transform.right + moveInputLocal.y * transform.forward;
            character.Move(move * speed * Runner.DeltaTime);

    }



    // public void OnInput(NetworkRunner runner, NetworkInput input)
    // {   
    //     var data = new DuLieuInput();
    //     data.moveInput = moveInputLocal;
    //     data.BamE = NutE;
        
    //     input.Set(data);
    //     NutE = false; // Reset sau khi gửi
    // }
    



    public void OnMove(InputValue value)
    {
        moveInputLocal = value.Get<Vector2>();
        //Debug.Log("<color=cyan>Bàn phím đang bấm:</color> " + moveInput); 
    }




    private void Quetxungquanh()
    {
        Collider[] ketQuaQuet = Physics.OverlapSphere(transform.position, banKinhNhat);
        foreach (var Obj in ketQuaQuet)
        {
            if (Obj.CompareTag("Rac"))
            {
                Debug.Log("<color=green>Đã nhặt được rác!</color>");
                Destroy(Obj.gameObject);
            }
        }
    }

}
