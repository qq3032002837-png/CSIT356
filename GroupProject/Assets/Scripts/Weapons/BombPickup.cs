using UnityEngine;

public class BombPickup : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //the trigger collider of the object
    public Collider coll;
    void Start()
    {
        coll = GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject OtherGameObj = other.gameObject;
        try
        {
            //enable the bombweapon script on the player
            Destroy(gameObject);
        }
        catch 
        {

        }
    }
}
