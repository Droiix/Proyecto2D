using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] private int points = 1;
    [SerializeField] private AudioClip pickupSfx;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.AddCoin(points);
            if (pickupSfx) AudioSource.PlayClipAtPoint(pickupSfx, transform.position);
            Destroy(gameObject);
        }
    }
}
