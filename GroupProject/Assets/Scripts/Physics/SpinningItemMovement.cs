using UnityEngine;

public class SpinningItemMovement : MonoBehaviour
{
    //this script simply makes the object bob up and down while spinning around so it is communicated to the player that this object is an item they can pick up

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(0f, (Mathf.Cos(Time.time * 1.5f) * 0.0004f), 0f);
        transform.Rotate(0f, 45f * Time.deltaTime, 0f);
    }
}
