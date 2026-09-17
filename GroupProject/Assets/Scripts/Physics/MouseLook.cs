using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Control Script/Mouse Look")] // add to the Unity editor's component menu
public class MouseLook : MonoBehaviour
{
    // enum to choose rotation axis in the Unity editor
    public enum RotationAxes
    {
        MouseXAndY = 0,     // yaw and pitch
        MouseX = 1,         // yaw only
        MouseY = 2          // pitch only
    }
    // rotation axis
    public RotationAxes axes = RotationAxes.MouseX;

    // rotation sensitivity
    public float ScalesensitivityHor = 9.0f;
    public float ScalesensitivityVert = 9.0f;

    private float sensitivityHor;
    private float sensitivityVert;

    // min and max pitch angles
    public float minimumVert = -45.0f;
    public float maximumVert = 45.0f;

    // for the pitch
    private float verticalRot = 0;

    //input actions
    InputAction LookAction;

    // Start is called before the first frame update
    void Start()
    {
        // make the rigid body not change rotation, if it has one
        Rigidbody body = GetComponent<Rigidbody>();
        
        if (body != null)
        {
            body.freezeRotation = true;
        }
        //find the look action in the unity input system
        LookAction = InputSystem.actions.FindAction("Look");

        //set the sensitivity from the scale(1-10) and lower the hell out of it
        sensitivityHor = ScalesensitivityHor * 0.1f;
        sensitivityVert = ScalesensitivityVert * 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 LookVector = LookAction.ReadValue<Vector2>();
        // yaw only
        if (axes == RotationAxes.MouseX)
        {
            // rotate about y-axis
            transform.Rotate(0, LookVector.x * sensitivityHor, 0);
        }
        // pitch only
        else if (axes == RotationAxes.MouseY)
        {
            // change in pitch
            verticalRot -= LookVector.y * sensitivityVert;
            verticalRot = Mathf.Clamp(verticalRot, minimumVert, maximumVert);

            // get current yaw
            float horizontalRot = transform.localEulerAngles.y;

            // set the pitch, yaw remains the same
            transform.localEulerAngles = new Vector3(verticalRot, horizontalRot, 0);
        }
        // yaw and pitch
        else
        {
            // change in pitch
            verticalRot -= LookVector.y * sensitivityVert;
            verticalRot = Mathf.Clamp(verticalRot, minimumVert, maximumVert);

            // change in yaw
            float delta = LookVector.x * sensitivityHor;
            float horizontalRot = transform.localEulerAngles.y + delta;

            // set pitch and yaw
            transform.localEulerAngles = new Vector3(verticalRot, horizontalRot, 0);
        }
    }
}
