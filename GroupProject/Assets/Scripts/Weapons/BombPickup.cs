using Unity.IntegerTime;
using UnityEngine;

public class BombPickup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //the trigger collider of the object

    public GameObject ghostenemy;

    private void OnTriggerEnter(Collider other)
    {
        GameObject OtherGameObj = other.gameObject;
        try
        {
            BombPlacer bomplc = OtherGameObj.GetComponentInChildren<BombPlacer>();
            bomplc.enabled = true;
            Instantiate(ghostenemy);
            Destroy(gameObject);
        }
        catch 
        {

        }
    }
}
