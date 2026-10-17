using UnityEngine;

public class ExplosiveObject : MonoBehaviour
{
    private Health health;
    [SerializeField] private float ExpRadius = 6f;
    [SerializeField] private float ExpForce = 14f;
    [SerializeField] private LayerMask LayerToBlowUp;
    [SerializeField] private float CountDownSeconds = 5f;
    private Material material;
    //private Color BoomRed = Color.red;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        material = GetComponent<MeshRenderer>().material;
        health = GetComponent<Health>();
        health.onDied.AddListener(OnDied);
        LayerToBlowUp = LayerMask.GetMask("BreakableWall");
    }

    // Update is called once per frame
    void Update()
    {
        CountDownSeconds -= Time.deltaTime;
        //material.SetColor(1, (BoomRed*(5-CountDownSeconds)));
        if (CountDownSeconds <= 0)
        {
            OnDied();
        }
    }

    void OnDied()
    {
        Explosion.Detonate(transform.position, ExpRadius, ExpForce, LayerToBlowUp);
        Destroy(gameObject);
    }
}
