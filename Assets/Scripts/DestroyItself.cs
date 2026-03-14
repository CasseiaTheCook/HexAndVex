using UnityEngine;

public class DestroyItself : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 2f; // Inspector'da ayarlanabilir (saniye)

    void Start()
    {
        // Belirtilen süre sonra kendisini sil
        Destroy(gameObject, destroyDelay);
    }
}
