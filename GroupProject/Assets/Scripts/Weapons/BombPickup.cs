using Unity.IntegerTime;
using UnityEngine;

public class BombPickup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //the trigger collider of the object

    public GameObject ghostenemy;
    public Transform playertransform;

    private void OnTriggerEnter(Collider other)
    {
        GameObject OtherGameObj = other.gameObject;
        try
        {
            BombPlacer bomplc = OtherGameObj.GetComponentInChildren<BombPlacer>();
            bomplc.enabled = true;
            GhostEnemy GhostScript = Instantiate(ghostenemy).GetComponent<GhostEnemy>();
            GhostScript.SetTarget(playertransform);
            Destroy(gameObject);
        }
        catch 
        {

        }
    }
}
