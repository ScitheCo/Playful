using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingText : MonoBehaviour
{
    public TextMeshProUGUI damageText;
    public float moveSpeed = 25f;
    public float fadeDuration = 1f;

    public void Setup(float amount, bool isCritical, bool isHeal)
    {
        if (damageText == null) damageText = GetComponentInChildren<TextMeshProUGUI>();

        // Metin Ayarı
        damageText.text = Mathf.RoundToInt(amount).ToString();

        // Renk Ayarı
        if (isHeal)
        {
            damageText.color = Color.green;
            damageText.text = "+" + damageText.text;
        }
        else if (isCritical)
        {
            damageText.color = Color.yellow;
            damageText.fontSize *= 1.5f; // Kritikse büyük olsun
            damageText.text += "!";
        }
        else
        {
            damageText.color = Color.red; // Normal hasar
        }

        // Animasyonu Başlat
        StartCoroutine(AnimateRoutine());
    }

    IEnumerator AnimateRoutine()
    {
        float elapsed = 0f;
        Color startColor = damageText.color;
        Vector3 startPos = transform.position;

        while (elapsed < fadeDuration)
        {
            // Yukarı doğru hareket
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;

            // Opaklığı azalt (Fade Out)
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            damageText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}