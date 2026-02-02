using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // List karıştırmak için gerekli

public class BoardManager : MonoBehaviour
{
    [Header("Grid Ayarları")]
    public int width = 7;
    public int height = 8;
    public float spacing = 10f; 
    public float padding = 20f; 
    
    [Header("UI Referansları")]
    public RectTransform boardPanel; 
    public GameObject tilePrefab;    
    public Texture2D[] tileTextures; 
    
    private Sprite[] cachedSprites; 
    public int[] gridData; 
    private List<int> tileBag = new List<int>(); 
    private GameObject[,] tileObjects; 
    private float tileSize;
    
    private bool isProcessingMove = false;
    public bool IsBoardLocked() { return isProcessingMove; }

    private int currentElo = 1200;

    void Start()
    {
        GenerateSpritesFromTextures();
        gridData = new int[width * height];
        tileObjects = new GameObject[width, height];

        CalculateTileSize();
        InitializeBoardLogic(currentElo); 
        DrawBoardUI();
    }

    public void AttemptSwap(int x1, int y1, int dirX, int dirY)
    {
        if (isProcessingMove) return;

        int x2 = x1 + dirX;
        int y2 = y1 + dirY;

        if (x2 < 0 || x2 >= width || y2 < 0 || y2 >= height) return;

        StartCoroutine(ProcessSwap(x1, y1, x2, y2));
    }

    IEnumerator ProcessSwap(int x1, int y1, int x2, int y2)
    {
        isProcessingMove = true;

        DoSwap(x1, y1, x2, y2);
        yield return new WaitForSeconds(0.25f);

        List<GameObject> matchedTiles = FindMatchesSimple();

        if (matchedTiles.Count > 0)
        {
            StartCoroutine(ProcessGameLoop(matchedTiles));
        }
        else
        {
            DoSwap(x1, y1, x2, y2);
            yield return new WaitForSeconds(0.25f);
            isProcessingMove = false;
        }
    }

    // === ANA OYUN DÖNGÜSÜ (GÜNCELLENMİŞ VERSİYON) ===
    IEnumerator ProcessGameLoop(List<GameObject> initialMatches)
    {
        List<GameObject> currentMatches = initialMatches;

        // Eşleşme olduğu sürece dönmeye devam et
        while (currentMatches.Count > 0)
        {
            // --- PATLATMA ÖNCESİ SAYAÇLAR ---
            int redCount = 0;   // Hasar
            int blueCount = 0;  // Mana
            bool greenFound = false; // Kalkan (Yeşil)

            // 1. Eşleşenleri Yok Et (Veriden ve Görselden)
            foreach (GameObject tile in currentMatches)
            {
                if (tile != null)
                {
                    TileController tc = tile.GetComponent<TileController>();
                    
                    // RENGİNE GÖRE İŞLEM YAP (ID'lerin Inspector sırasına göre)
                    if (tc.typeID == 1) redCount++;          // Kırmızı
                    else if (tc.typeID == 2) blueCount++;    // Mavi
                    else if (tc.typeID == 3) greenFound = true; // Yeşil
                    
                    // Standart yok etme işlemleri
                    gridData[tc.x + tc.y * width] = 0; // Datayı boşalt
                    tileObjects[tc.x, tc.y] = null;    // Referansı sil
                    tc.Explode();                      // Görseli patlat
                }
            }

            // --- BATTLE MANAGER'A GÖNDER ---
            if (BattleManager.Instance != null)
            {
                // Kırmızı varsa Rakibe Hasar vur (Taş başı 10 hasar)
                if (redCount > 0)
                {
                    BattleManager.Instance.TakeDamage(false, redCount * 10f);
                }

                // Mavi varsa Mana ekle (Taş başı 5 mana)
                if (blueCount > 0)
                {
                    BattleManager.Instance.AddMana(blueCount * 5f);
                }

                // Yeşil varsa Kalkanı Aktif Et (5 saniye)
                if (greenFound)
                {
                    BattleManager.Instance.ActivatePlayerShield(5f);
                }
            }
            // --------------------------------

            yield return new WaitForSeconds(0.25f); // Patlama animasyonu süresi

            // 2. Yerçekimi (Collapse) - Taşları aşağı kaydır
            ApplyGravity();

            // 3. Boşlukları Doldur (Refill) - Yeni taşlar üret
            RefillBoard();

            yield return new WaitForSeconds(0.25f); // Düşme animasyonu süresi

            // 4. Yeni Eşleşme Var mı?
            currentMatches = FindMatchesSimple();
            
            // Eğer yeni eşleşme varsa döngü başa döner, yoksa biter.
        }

        // --- DEADLOCK KONTROLÜ ---
        if (!HasPossibleMoves())
        {
            Debug.Log("HAMLE KALMADI! Tahta Karıştırılıyor...");
            yield return StartCoroutine(ShuffleBoard());
        }
        else
        {
            // Kilit açıldı, oyuncu oynayabilir
            // Private değişkene dışarıdan erişilemediği için fonksiyonla açabiliriz 
            // veya değişkeni protected/public yapabilirsin ama şimdilik burada false yapıyoruz.
            // (Not: isProcessingMove değişkenin private ise class içinde en üstte tanımlı olmalı)
            // isProcessingMove = false; 
            // BoardManager içindeki değişkenini private tanımladığımız için:
             GetType().GetField("isProcessingMove", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, false);
        }
    }

    // === DEADLOCK ÇÖZÜMÜ: SHUFFLE ===
    IEnumerator ShuffleBoard()
    {
        yield return new WaitForSeconds(0.5f); // Oyuncu durumu fark etsin diye bekle

        // 1. Mevcut taşları topla (Boşlukları alma)
        List<int> currentTiles = new List<int>();
        for (int i = 0; i < gridData.Length; i++)
        {
            if (gridData[i] != 0) currentTiles.Add(gridData[i]);
        }

        // 2. Taşları karıştır (Fisher-Yates Shuffle)
        // Eğer Shuffle sonucunda yine hamle yoksa tekrarla (Garantili Çözüm)
        bool playableBoardFound = false;
        int maxRetry = 10; // Sonsuz döngü koruması

        while (!playableBoardFound && maxRetry > 0)
        {
            // Listeyi karıştır
            for (int i = 0; i < currentTiles.Count; i++) {
                int temp = currentTiles[i];
                int randomIndex = Random.Range(i, currentTiles.Count);
                currentTiles[i] = currentTiles[randomIndex];
                currentTiles[randomIndex] = temp;
            }

            // Gride yerleştir (Sanal olarak)
            for (int x = 0; x < width; x++) {
                for (int y = 0; y < height; y++) {
                    gridData[x + y * width] = currentTiles[x + y * width]; // Basitçe sırayla doldur
                }
            }

            // Patlamaya hazır taş var mı? (İstemeyiz, temiz başlasın)
            if (FindMatchesSimple().Count > 0) 
            {
                maxRetry--;
                continue; 
            }

            // Hamle var mı?
            if (HasPossibleMoves()) 
            {
                playableBoardFound = true;
            }
            else
            {
                maxRetry--;
            }
        }

        // 3. Görsel Objeleri Veriye Göre Güncelle
        // Mevcut objelerin sadece resmini ve ID'sini değiştirmek daha performanslıdır,
        // ama animasyon için yerlerini değiştirmek daha havalı durur.
        // Basit ve güvenli yöntem: Resimlerini (Sprite) güncellemek.

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int type = gridData[x + y * width];
                GameObject tileObj = tileObjects[x, y];
                
                // Veriyi güncelle
                TileController tc = tileObj.GetComponent<TileController>();
                tc.typeID = type;
                
                // Görseli güncelle
                Image img = tileObj.GetComponent<Image>();
                img.sprite = cachedSprites.Length > type ? cachedSprites[type] : cachedSprites[0];
                
                // Ufak bir "Hopla" animasyonu yapabiliriz belli olsun diye
                // Şimdilik sadece güncelledik.
            }
        }
        
        Debug.Log("Tahta Karıştırıldı ve Hamleler Açıldı.");
        isProcessingMove = false;
    }

    // === HAMLE KONTROL ALGORİTMASI ===
    // Bu fonksiyon tahtayı bozmadan sanal olarak "Şunu şuraya çeksem olur muydu?" diye dener.
    bool HasPossibleMoves()
    {
        // Tahtanın kopyasını al (Orijinal veriyi bozmamak için)
        int[] tempGrid = (int[])gridData.Clone();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // SAĞA TAKAS DENEMESİ
                if (x < width - 1)
                {
                    if (CheckSwapSimulation(x, y, x + 1, y, tempGrid)) return true;
                }

                // YUKARI TAKAS DENEMESİ
                if (y < height - 1)
                {
                    if (CheckSwapSimulation(x, y, x, y + 1, tempGrid)) return true;
                }
            }
        }
        return false; // Hiçbir hamle işe yaramadı
    }

    bool CheckSwapSimulation(int x1, int y1, int x2, int y2, int[] tempGrid)
    {
        // 1. Sanal Takas Yap
        int index1 = x1 + y1 * width;
        int index2 = x2 + y2 * width;
        int t1 = tempGrid[index1];
        int t2 = tempGrid[index2];

        tempGrid[index1] = t2;
        tempGrid[index2] = t1;

        // 2. Eşleşme Var mı Kontrol Et
        bool hasMatch = false;
        
        // Sadece etkilenen bölgeleri kontrol etsek yeter ama güvenli olsun diye basit kontrol:
        // Takaslanan 1. taş için kontrol
        if (CheckMatchAt(x1, y1, tempGrid) || CheckMatchAt(x2, y2, tempGrid)) hasMatch = true;

        // 3. Takası Geri Al (Temiz bırakmak için)
        tempGrid[index1] = t1;
        tempGrid[index2] = t2;

        return hasMatch;
    }

    bool CheckMatchAt(int x, int y, int[] grid)
    {
        int type = grid[x + y * width];
        if (type == 0) return false;

        // Yatay (Merkez, Sol, Sağ)
        if (x >= 2 && grid[(x - 1) + y * width] == type && grid[(x - 2) + y * width] == type) return true;
        if (x >= 1 && x < width - 1 && grid[(x - 1) + y * width] == type && grid[(x + 1) + y * width] == type) return true;
        if (x < width - 2 && grid[(x + 1) + y * width] == type && grid[(x + 2) + y * width] == type) return true;

        // Dikey
        if (y >= 2 && grid[x + (y - 1) * width] == type && grid[x + (y - 2) * width] == type) return true;
        if (y >= 1 && y < height - 1 && grid[x + (y - 1) * width] == type && grid[x + (y + 1) * width] == type) return true;
        if (y < height - 2 && grid[x + (y + 1) * width] == type && grid[x + (y + 2) * width] == type) return true;

        return false;
    }

    // === GRAVITY & REFILL & OTHER UTILS (AYNEN KALDI) ===
    void ApplyGravity()
    {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (gridData[x + y * width] == 0) {
                    for (int k = y + 1; k < height; k++) {
                        if (gridData[x + k * width] != 0) {
                            int tileType = gridData[x + k * width];
                            GameObject tileObj = tileObjects[x, k];
                            gridData[x + y * width] = tileType; gridData[x + k * width] = 0;
                            tileObjects[x, y] = tileObj; tileObjects[x, k] = null;
                            TileController tc = tileObj.GetComponent<TileController>();
                            tc.x = x; tc.y = y; tc.name = $"Tile_{x}_{y}";
                            tc.MoveToPosition(GetTilePosition(x, y));
                            break;
                        }
                    }
                }
            }
        }
    }

    void RefillBoard()
    {
        int activeColors = currentElo > 1200 ? 5 : 4;
        float spawnOffsetY = boardPanel.rect.height / 2f + tileSize;
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (gridData[x + y * width] == 0) {
                    int newType = GetNextTileFromBag(activeColors);
                    gridData[x + y * width] = newType;
                    Vector2 finalPos = GetTilePosition(x, y);
                    Vector2 startPos = new Vector2(finalPos.x, spawnOffsetY);
                    GameObject newTile = Instantiate(tilePrefab, boardPanel);
                    RectTransform rect = newTile.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(tileSize, tileSize);
                    rect.anchoredPosition = startPos;
                    Sprite spriteToUse = cachedSprites.Length > newType ? cachedSprites[newType] : cachedSprites[0];
                    newTile.GetComponent<TileController>().Initialize(x, y, newType, spriteToUse, this);
                    newTile.GetComponent<TileController>().MoveToPosition(finalPos);
                    tileObjects[x, y] = newTile;
                }
            }
        }
    }

    List<GameObject> FindMatchesSimple()
    {
        HashSet<GameObject> matchedSet = new HashSet<GameObject>();
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int currentType = gridData[x + y * width];
                if (currentType == 0) continue;
                int matchCount = 1;
                for (int k = x + 1; k < width; k++) {
                    if (gridData[k + y * width] == currentType) matchCount++; else break;
                }
                if (matchCount >= 3) for (int k = 0; k < matchCount; k++) matchedSet.Add(tileObjects[x + k, y]);
            }
        }
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int currentType = gridData[x + y * width];
                if (currentType == 0) continue;
                int matchCount = 1;
                for (int k = y + 1; k < height; k++) {
                    if (gridData[x + k * width] == currentType) matchCount++; else break;
                }
                if (matchCount >= 3) for (int k = 0; k < matchCount; k++) matchedSet.Add(tileObjects[x, y + k]);
            }
        }
        return matchedSet.ToList();
    }

    void DoSwap(int x1, int y1, int x2, int y2)
    {
        int index1 = x1 + y1 * width; int index2 = x2 + y2 * width;
        int tempType = gridData[index1]; gridData[index1] = gridData[index2]; gridData[index2] = tempType;
        GameObject tempObj = tileObjects[x1, y1];
        tileObjects[x1, y1] = tileObjects[x2, y2]; tileObjects[x2, y2] = tempObj;
        TileController t1 = tileObjects[x1, y1].GetComponent<TileController>(); t1.x = x1; t1.y = y1; t1.name = $"Tile_{x1}_{y1}";
        TileController t2 = tileObjects[x2, y2].GetComponent<TileController>(); t2.x = x2; t2.y = y2; t2.name = $"Tile_{x2}_{y2}";
        UpdateTilePosition(tileObjects[x1, y1], x1, y1); UpdateTilePosition(tileObjects[x2, y2], x2, y2);
    }

    void UpdateTilePosition(GameObject tile, int x, int y) { tile.GetComponent<TileController>().MoveToPosition(GetTilePosition(x, y)); }

    Vector2 GetTilePosition(int x, int y) {
        float totalGridWidth = (width * tileSize) + ((width - 1) * spacing);
        float totalGridHeight = (height * tileSize) + ((height - 1) * spacing);
        float startX = -totalGridWidth / 2f + (tileSize / 2f);
        float startY = -totalGridHeight / 2f + (tileSize / 2f);
        return new Vector2(startX + (x * (tileSize + spacing)), startY + (y * (tileSize + spacing)));
    }

    void GenerateSpritesFromTextures() {
        cachedSprites = new Sprite[tileTextures.Length];
        for (int i = 0; i < tileTextures.Length; i++) {
            if (tileTextures[i] != null) {
                Texture2D tex = tileTextures[i];
                cachedSprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
    void CalculateTileSize() {
        float usableWidth = boardPanel.rect.width - (spacing * (width - 1)) - (padding * 2);
        float usableHeight = boardPanel.rect.height - (spacing * (height - 1)) - (padding * 2);
        tileSize = Mathf.Min(usableWidth / width, usableHeight / height);
    }
    void InitializeBoardLogic(int elo) {
        int activeColorCount = elo > 1200 ? 5 : 4; RefillBag(activeColorCount);
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int tile = -1; int maxIterations = 0;
                while (true) {
                    tile = Random.Range(1, activeColorCount + 1);
                    bool isMatch = false;
                    if (x >= 2 && gridData[(x - 1) + y * width] == tile && gridData[(x - 2) + y * width] == tile) isMatch = true;
                    if (y >= 2 && gridData[x + (y - 1) * width] == tile && gridData[x + (y - 2) * width] == tile) isMatch = true;
                    if (!isMatch) break;
                    maxIterations++; if (maxIterations > 100) break;
                }
                gridData[x + y * width] = tile;
            }
        }
    }
    void DrawBoardUI() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int tileType = gridData[x + y * width];
                if (tileType > 0) {
                    Vector2 pos = GetTilePosition(x, y);
                    GameObject newTile = Instantiate(tilePrefab, boardPanel);
                    RectTransform rect = newTile.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(tileSize, tileSize);
                    rect.anchoredPosition = pos;
                    Sprite spriteToUse = cachedSprites.Length > tileType ? cachedSprites[tileType] : cachedSprites[0];
                    newTile.GetComponent<TileController>().Initialize(x, y, tileType, spriteToUse, this);
                    tileObjects[x, y] = newTile;
                }
            }
        }
    }
    void RefillBag(int maxColors) { tileBag.Clear(); for (int i = 1; i <= maxColors; i++) for (int j = 0; j < 10; j++) tileBag.Add(i); }
    public int GetNextTileFromBag(int maxColors) {
        if (tileBag.Count == 0) RefillBag(maxColors);
        int index = Random.Range(0, tileBag.Count);
        int selectedTile = tileBag[index]; tileBag.RemoveAt(index); return selectedTile;
    }
}