using UnityEngine;

public class EnemyAmmoDrop : MonoBehaviour
{
    Health health;
    public GameObject AmmoBoxPrefab;
    void Start()
    {
        health = GetComponent<Health>();
        health.onDied.AddListener(OnDied);
    }

    // Update is called once per frame
    void OnDied()
    {
        Instantiate(AmmoBoxPrefab, transform.position, transform.rotation);
    }
}
