using UnityEngine;

public class GarbageController : MonoBehaviour
{
    bool IsProtectedRoot(Transform t)
    {
        if (!t) return true;
        var root = t.root;
        if (root.CompareTag("Player")) return true;
        if (root.CompareTag("InitialPlatform")) return true; // pon este tag a tu plataforma fija
        if (root.GetComponent<GameManager>()) return true;
        if (root.GetComponent<PlatformGeneratorSmart2D>()) return true;
        return false;
    }

    bool ShouldDespawn(Transform t)
    {
        var root = t.root;

        // Tags que pediste
        if (root.CompareTag("Coin")) return true;
        if (root.CompareTag("Enemy")) return true;

        // Extras útiles
        if (root.GetComponent<Bullet>()) return true;

        // Plataformas generadas
        if (root.name.Contains("(Clone)") &&
            root.gameObject.layer == LayerMask.NameToLayer("Ground") &&
            root.GetComponentInChildren<BoxCollider2D>() != null) return true;

        return false;
    }

    void OnTriggerEnter2D(Collider2D c)
    {
        var target = c.attachedRigidbody ? c.attachedRigidbody.transform : c.transform;
        if (IsProtectedRoot(target)) return;
        if (!ShouldDespawn(target)) return;

        Destroy(target.root.gameObject);
    }
}


