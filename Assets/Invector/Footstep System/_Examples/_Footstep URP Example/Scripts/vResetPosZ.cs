using UnityEngine;
using System.Collections;

public class vResetPosZ : MonoBehaviour
{
    /// <summary>
    /// This is a simple script o reset the Player position.z to the reference startPos transform
    /// It was created specific for the Footstep Examples demo scene of the Invector Footstep System package
    /// Feel free to delete it if you don't need it
    /// </summary>

    public Transform startPos;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var posZ = new Vector3(other.transform.position.x, other.transform.position.y, startPos.position.z);
            other.gameObject.transform.position = posZ;
        }
    }
}