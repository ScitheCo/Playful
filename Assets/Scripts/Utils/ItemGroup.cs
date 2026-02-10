using UnityEngine;
using UnityEngine.UI;

public class ItemGroup : MonoBehaviour
{
    public Image m_Image;
    public GameObject m_SelectionBorder;

    public void SetImageSprite(Sprite sprite)
    {
        m_Image.sprite = sprite;
    }

    public void SetSelectionBorder(bool status)
    {
        m_SelectionBorder.SetActive(status);
    }
}
