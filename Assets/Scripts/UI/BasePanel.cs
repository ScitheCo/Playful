using UnityEngine;

// Abstract class: Tek başına kullanılamaz, miras alınması gerekir.
public abstract class BasePanel : MonoBehaviour
{
    public bool startActive = false; // Oyun başlayınca açık mı olsun?

    public virtual void Init()
    {
        // Başlangıç ayarları (Gerekirse override edilebilir)
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}