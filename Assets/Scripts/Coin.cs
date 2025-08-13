using UnityEngine;
// moneda que el jugador puede recoger
// se destruye al recogerla y suma puntos al GameManager
public class Coin : MonoBehaviour
{
    [SerializeField] private int points = 1;
    [SerializeField] private AudioClip pickupSfx;
    
    //agarra la moneda y la destruye
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
