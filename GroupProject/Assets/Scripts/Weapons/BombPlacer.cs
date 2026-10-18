using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class BombPlacer : MonoBehaviour
{
    //the input action that shows the bomb and where to place it, also disables the shotgun script so you dont accidently shoot while trying to place the bomb
    InputAction ShowBombAction;
    InputAction CancelPreviewAction;
    //the camera this script is attached to
    GameObject ThisCam;
    // the bomb prefab and the bomb hollogram prefab
    public GameObject Bomb;
    public GameObject BombHollogram;
    [SerializeField]GameObject HollogramInstance;
    [SerializeField] bool Preview;
    Shotgun shotgun;
    void Start()
    {
        Preview = false;
        ShowBombAction = InputSystem.actions.FindAction("Interact");
        CancelPreviewAction = InputSystem.actions.FindAction("Attack");
        ThisCam = gameObject;
        shotgun = GetComponent<Shotgun>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Preview)
        {
            shotgun.enabled = false;
        } else
        {
            shotgun.enabled = true;
        }
        bool IsPlaceable = true;
        try
        {
            BombHollogramPlaceableScript BHPS = HollogramInstance.GetComponent<BombHollogramPlaceableScript>();
            IsPlaceable = BHPS.GetPlaceable();
        }
        catch { } //do nothing LOL


        //if the player presses E again while looking at the preview AND it is placeable, then place it!
        if ((ShowBombAction.WasPressedThisFrame()) && (IsPlaceable) && Preview)
        {
            CreateBomb(HollogramInstance.transform, HollogramInstance);
        }

        //if the hollogram isnt up and the player presses E
        else if ((ShowBombAction.WasPressedThisFrame()) && !Preview)
        {
            HollogramInstance = CreateBombHollogram(); 
        } 
        
        //if the hollogram is up and the player presses mouse 1 (attack)
        else if ((CancelPreviewAction.WasPressedThisFrame()) && Preview) {
            CancelBombHollogram(HollogramInstance);
        }

    }

    GameObject CreateBombHollogram()
    {
        GameObject RealBombHollogram = Instantiate(BombHollogram, gameObject.transform);
        Preview = true;
        return RealBombHollogram;
    }

    void CancelBombHollogram(GameObject BombHollogram)
    {
        Destroy(BombHollogram);
        Preview = false;

    }
    void CreateBomb(Transform Trans, GameObject BombHollogram)
    {
        Vector3 PosVec = Trans.position;
        GameObject obj = Instantiate(Bomb, PosVec, new Quaternion(0f, 0f, 0f, 0f));
        CancelBombHollogram(BombHollogram);
    }
    
}
