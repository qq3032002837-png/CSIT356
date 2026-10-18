using UnityEngine;

public class AmmoPickup : MonoBehaviour
{

    //this script simple adds ammo to the players gun when they pickup the gameobject, also deleting it in the process
    private void OnTriggerEnter(Collider other)
    {
        GameObject OtherGameObj = other.gameObject;
        try
        {
            Shotgun shotgun = OtherGameObj.GetComponentInChildren<Shotgun>();
            shotgun.AddAmmo(3);
            Destroy(gameObject);
        }
        catch
        {

        }
    }
}
