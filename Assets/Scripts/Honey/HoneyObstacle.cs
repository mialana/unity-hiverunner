using UnityEngine;

public class HoneyObstacle : MonoBehaviour
{
    public GameObject player;
    public float honeyAmount = 10f;

    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject == player)
        {
            Destroy(gameObject);
        }
    }
}
