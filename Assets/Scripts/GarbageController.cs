using UnityEngine;

public class GarbageController : MonoBehaviour
{
    bool IsProtectedRoot(Transform t)
    {
        if (!t) return true;
        var root = t.root;
        if (root.CompareTag("Player")) return true;
        if (root.CompareTag("InitialPlatform")) return true; // Plataforma inicial
        if (root.GetComponent<GameManager>()) return true; 
        if (root.GetComponent<PlatformGeneratorSmart2D>()) return true;
        return false;
    }

    bool ShouldDespawn(Transform t)
    {
        var root = t.root;

        // Tags qur comparan con los objetos que no se deben destruir
        if (root.CompareTag("Coin")) return true;
        if (root.CompareTag("Enemy")) return true;

        
        if (root.GetComponent<Bullet>()) return true;

        // Plataformas para evitar que se destruyan
        if (root.name.Contains("(Clone)") &&
            root.gameObject.layer == LayerMask.NameToLayer("Ground") &&
            root.GetComponentInChildren<BoxCollider2D>() != null) return true;

        return false;
    }

    // Detecta colisiones con objetos que no deben ser destruidos
    void OnTriggerEnter2D(Collider2D c)
    {
        var target = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
        if (IsProtectedRoot(target)) return;
        if (!ShouldDespawn(target)) return;

        Destroy(target.root.gameObject);
    }
}


