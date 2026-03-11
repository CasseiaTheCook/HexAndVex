using UnityEngine;
using System.Collections;

public class CoinDropVFX : MonoBehaviour
{
    public static CoinDropVFX instance;

    [Header("Coin Sprite")]
    public Sprite coinSprite;

    [Header("Ayarlar")]
    public float spawnRadius = 0.3f;
    public float riseHeight = 0.6f;
    public float riseDuration = 0.25f;
    public float floatDuration = 0.15f;
    public float fadeDuration = 0.3f;
    public float coinSize = 0.25f;

    public int activeCoinCount { get; private set; }

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void SpawnCoins(Vector3 worldPos, int count)
    {
        if (AudioManager.instance != null) AudioManager.instance.PlayCoin();
        count = Mathf.Clamp(count, 1, 12);
        for (int i = 0; i < count; i++)
        {
            activeCoinCount++;
            float delay = i * 0.05f;
            StartCoroutine(AnimateCoin(worldPos, delay));
        }
    }

    private IEnumerator AnimateCoin(Vector3 origin, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        GameObject coin = new GameObject("CoinVFX");
        SpriteRenderer sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = coinSprite;
        sr.sortingOrder = 100;
        coin.transform.localScale = Vector3.one * coinSize;

        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        Vector3 startPos = origin + new Vector3(randomOffset.x, randomOffset.y, 0f);
        coin.transform.position = startPos;

        // Rise
        Vector3 peakPos = startPos + Vector3.up * riseHeight;
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseDuration;
            t = 1f - (1f - t) * (1f - t); // ease-out
            coin.transform.position = Vector3.Lerp(startPos, peakPos, t);
            yield return null;
        }

        // Float
        yield return new WaitForSeconds(floatDuration);

        // Fade out
        elapsed = 0f;
        Color c = sr.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            c.a = Mathf.Lerp(1f, 0f, t);
            sr.color = c;
            coin.transform.position += Vector3.up * Time.deltaTime * 0.3f;
            yield return null;
        }

        Destroy(coin);
        activeCoinCount--;
    }
}
