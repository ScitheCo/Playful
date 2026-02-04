using UnityEngine;
using System.Collections;

public class ObjectShake : MonoBehaviour
{
    // Sarsıntıyı başlatmak için dışarıdan bu fonksiyonu çağıracağız
    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines(); // Varsa eski sarsıntıyı durdur ki üst üste binmesin
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Rastgele bir noktaya titret
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            transform.localPosition = originalPos + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos; // Yerine geri koy
    }
}