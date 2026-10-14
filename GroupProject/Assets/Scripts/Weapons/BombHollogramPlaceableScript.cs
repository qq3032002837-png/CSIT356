using UnityEditor.Build;
using UnityEngine;

public class BombHollogramPlaceableScript : MonoBehaviour
{
    Material HollogramMaterial;
    bool IsPlaceable;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        HollogramMaterial = GetComponent<MeshRenderer>().material;
        IsPlaceable = true;
    }

    // Update is called once per frame
    void Update()
    {
        //if its placeable, make the object green. If its not then make it red
        HollogramMaterial.color = (IsPlaceable) ? Color.green:Color.red;
    }
    public bool GetPlaceable()
    {
        return IsPlaceable;
    }

    private void OnTriggerEnter(Collider other)
    {
        IsPlaceable = false;
    }
    private void OnTriggerStay(Collider other)
    {
        IsPlaceable = false;
    }
    private void OnTriggerExit(Collider other)
    {
        IsPlaceable = true;
    }
}
