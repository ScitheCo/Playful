using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class TileController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int x;
    public int y;
    public int typeID;

    private Image myImage;
    private RectTransform rectTransform;
    private BoardManager board;

    private bool isMoving = false;
    private float moveSpeed = 0.2f; // Kayma hızı

    private float swipeThreshold = 30f; 
    private Vector2 startTouchPosition;
    private Vector2 endTouchPosition;

    private void Awake()
    {
        myImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(int _x, int _y, int _type, Sprite _sprite, BoardManager _board)
    {
        x = _x;
        y = _y;
        typeID = _type;
        myImage.sprite = _sprite;
        board = _board;
        name = $"Tile_{x}_{y}";
    }

    public void MoveToPosition(Vector2 targetPosition)
    {
        StartCoroutine(MoveCoroutine(targetPosition));
    }

    IEnumerator MoveCoroutine(Vector2 targetPosition)
    {
        isMoving = true;
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < moveSpeed)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, elapsed / moveSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        isMoving = false;
    }

    // === YENİ: PATLAMA EFEKTİ ===
    public void Explode()
    {
        StartCoroutine(ExplodeCoroutine());
    }

    IEnumerator ExplodeCoroutine()
    {
        // Taşı yavaşça küçült
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Animasyon bitince yok et
        Destroy(gameObject);
    }
    // ============================

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isMoving || board.IsBoardLocked()) return; // Board kilitliyse dokunma
        startTouchPosition = eventData.position;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isMoving || board.IsBoardLocked()) return;
        endTouchPosition = eventData.position;
        CalculateSwipe();
    }

    void CalculateSwipe()
    {
        Vector2 swipeVector = endTouchPosition - startTouchPosition;
        if (swipeVector.magnitude < swipeThreshold) return;

        swipeVector.Normalize();

        if (Mathf.Abs(swipeVector.x) > Mathf.Abs(swipeVector.y))
        {
            if (swipeVector.x > 0) board.AttemptSwap(x, y, 1, 0); 
            else board.AttemptSwap(x, y, -1, 0); 
        }
        else
        {
            if (swipeVector.y > 0) board.AttemptSwap(x, y, 0, 1); 
            else board.AttemptSwap(x, y, 0, -1); 
        }
    }
}