using UnityEngine;

public class ExplosiveObject : MonoBehaviour
{
    private Health health;
    [SerializeField] private float ExpRadius = 6f;
    [SerializeField] private float ExpForce = 14f;
    [SerializeField] private LayerMask LayerToIgnore;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = GetComponent<Health>();
        health.onDied.AddListener(OnDied);
        LayerToIgnore = LayerMask.GetMask("BreakableWall");
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDied()
    {
        Explosion.Detonate(transform.position, ExpRadius, ExpForce, LayerToIgnore);
    }
}
