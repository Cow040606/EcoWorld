
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed =5f;
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate ()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(x , 0, z).normalized;
    
        //di chuyển
       rb.linearVelocity=new Vector3(move.x * speed ,rb.linearVelocity.y,move.z*speed );
        // xoay theo hướng di chuyển 
        if (move != Vector3.zero)
        {
            transform.forward = move;
        }
    }
    
}