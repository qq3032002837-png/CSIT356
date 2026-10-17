using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;


public class Shotgun : MonoBehaviour
{
    /// <summary> the amount of ammo the player has </summary>
    [SerializeField] private int Ammo = 6; // the amount it is set to here is how much the player will start with
    /// <summary> The cooldown time, in seconds, between each shot</summary>
    [SerializeField] private float CooldownBetweenShots = 1f;
    /// <summary> the game run time since the gun was last shot</summary>
    private float TimeOfLastShot;

    /// <summary> the amount of damage one pellet should do </summary>
    [SerializeField] private float Damage = 5;
    /// <summary> the amount of projectiles the gun will shoot</summary>
    [SerializeField] private int NumberOfPellets = 12;
    /// <summary> how much spread the pellets will have when they shoot (measured by pixel distance from the centre of the screen)</summary>
    [SerializeField] private float Deviation = 40f;
    /// <summary> seconds after the shot before the pump sound plays</summary>
    [SerializeField] private float PumpSoundDelay = 0.35f;

    /// <summary> the shoot action (like mouse 1 or right trigger or whatever) </summary>
    private InputAction ShootAction;
    /// <summary> the assigned shotgun prefab, for decorative purposes only</summary>
    public GameObject ShotgunPrefab;
    /// <summary> where the gun sits relative to the camera (right, up, forward) in metres</summary>
    [SerializeField] private Vector3 GunHoldPosition = new Vector3(0.162f, -0.144f, 0.702f);
    /// <summary> the gun model in the player's hands, so the melee bash can animate it</summary>
    public Transform HeldGun { get; private set; }
    /// <summary> the assigned particle hit prefab, for decorative purposes only... and debugging</summary>
    public GameObject particleSysPrefab;
    /// <summary> the camera, should be fine</summary>
    public Camera Cam;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //set the cursor to always be in the middle of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ShootAction = InputSystem.actions.FindAction("Attack");
        SetPrefab();
        TimeOfLastShot = Time.time;
        Cam = GetComponent<Camera>();
        // melee bash on V, lives in its own script
        if (GetComponent<ShotgunBash>() == null) gameObject.AddComponent<ShotgunBash>();
    }
    private void SetPrefab()
    {
       GameObject gun = Instantiate(ShotgunPrefab, gameObject.transform); //is this how it works?
       // without this the gun keeps the template's position, which floats it 0.7m in front of the camera
       gun.transform.localPosition = GunHoldPosition;
       HeldGun = gun.transform;
    }
    // Update is called once per frame
    void Update()
    {
        float ShootActValue = ShootAction.ReadValue<float>();
        if ((ShootActValue > 0) && (Time.time - TimeOfLastShot > CooldownBetweenShots)) {
            Shoot();
            TimeOfLastShot = Time.time;
        }
    }
    private void Shoot()
    {
        if (Ammo > 0) {
            Bang();
            MinusAmmo();

        } else
        {
            EmptyShoot();
        }

    }
    private void EmptyShoot()
    {
        //use this function to make a sad, heartbreaking, and dissapointing *click* noise
        GameAudio.Play2D(GameAudio.ShotgunEmpty);
    }
    private void Bang()
    {
        // the bang, then the pump racking the next shell in a moment later
        GameAudio.Play2D(GameAudio.ShotgunFire, 0.8f);
        GameAudio.Play2D(GameAudio.ShotgunPump, 0.7f, 1f, PumpSoundDelay);

        for (int i = 0; i < NumberOfPellets; i++) {
            //random add on values
            float WidthRandom = Random.Range(-Deviation, Deviation);
            float HeightRandom = Random.Range(-Deviation, Deviation);
            // get point in the middle of the screen
            Vector3 point = new Vector3((Cam.pixelWidth / 2) + WidthRandom, (Cam.pixelHeight / 2) + HeightRandom, 0);

            // create a ray from the point in the direction of the camera
            Ray ray = Cam.ScreenPointToRay(point);


            RaycastHit hit; // stores ray intersection information

            // ray cast will obtain hit information if it intersects anything
            // we can use this for whatever we want tbh
            if (Physics.Raycast(ray, out hit))
            {
                // get the GameObject that was hit
                GameObject hitObject = hit.transform.gameObject;
                // Damage a hit enemy through its existing Health component, including child colliders.
                Health targetHealth = hit.collider.GetComponentInParent<Health>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(Damage);
                }
                StartCoroutine(GeneratePS(hit));

            }
        }
    }
    private IEnumerator GeneratePS(RaycastHit hit)
    {
        GameObject ps = Instantiate(particleSysPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        yield return new WaitForSeconds(1);
        Destroy(ps);
    }
    private void MinusAmmo()
    {
        Ammo -= 1;
        // this function could be used to also update the HUD of the player character. specifically the part indicating how much ammo is left
    }
    public void AddAmmo(int add)
    {
        Ammo += add;
        // ...what he said
    }
}
