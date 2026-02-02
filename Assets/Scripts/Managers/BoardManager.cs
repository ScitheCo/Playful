using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    [Header("Seviye Tasarımı")]
    public LevelData currentLevel; // Inspector'dan LevelData dosyasını buraya sürükle!

    [Header("Grid Ayarları")]
    [HideInInspector] public int width;  // LevelData'dan otomatik alınacak
    [HideInInspector] public int height; // LevelData'dan otomatik alınacak
    public float spacing = 10f; 
    public float padding = 20f; 
    
    [Header("UI Referansları")]
    public RectTransform boardPanel; 
    public GameObject tilePrefab;    
    
    // SIRALAMA ÖNEMLİ: 0:?, 1:Kırmızı(Hasar), 2:Mavi(Mana), 3:Yeşil(Kalkan)
    public Texture2D[] tileTextures; 
    
    // === ÖNBELLEK VE DATA ===
    private Sprite[] cachedSprites; 
    public int[] gridData; // 0:Boş, -1:Duvar/Ölü, >0:Taş ID
    private List<int> tileBag = new List<int>(); 
    private GameObject[,] tileObjects; 
    private float tileSize;

    // === KONTROL ===
    private bool isProcessingMove = false;
    public bool IsBoardLocked() { return isProcessingMove; }
    
    // Elo (İleride dinamik olacak)
    private int currentElo = 1200;

    void Start()
    {
        // 1. Level Data Kontrolü
        if (currentLevel == null)
        {
            Debug.LogError("HATA: BoardManager üzerinde 'Current Level' boş! Lütfen oluşturduğun LevelData'yı ata.");
            return;
        }

        // 2. Boyutları LevelData'dan al
        width = currentLevel.width;
        height = currentLevel.height;

        // 3. Hazırlıklar
        GenerateSpritesFromTextures();
        gridData = new int[width * height];
        tileObjects = new GameObject[width, height];

        // 4. Duvarları ve Grid'i Kur
        SetupWallsFromLevelData();
        CalculateTileSize();
        InitializeBoardLogic(currentElo); 
        DrawBoardUI();
    }

    // === GİRİŞ (INPUT) ===
    public void AttemptSwap(int x1, int y1, int dirX, int dirY)
    {
        if (isProcessingMove) return;

        int x2 = x1 + dirX;
        int y2 = y1 + dirY;

        // Grid sınırları kontrolü
        if (x2 < 0 || x2 >= width || y2 < 0 || y2 >= height) return;

        // Duvar kontrolü (Duvarla swap yapılamaz)
        if (gridData[x1 + y1 * width] == -1 || gridData[x2 + y2 * width] == -1) return;

        StartCoroutine(ProcessSwap(x1, y1, x2, y2));
    }

    // === SWAP VE OYUN AKIŞI ===
    IEnumerator ProcessSwap(int x1, int y1, int x2, int y2)
    {
        isProcessingMove = true;

        // 1. Swap Yap (Animasyonlu)
        DoSwap(x1, y1, x2, y2);
        yield return new WaitForSeconds(0.25f);

        // 2. Eşleşme Kontrolü
        List<GameObject> matchedTiles = FindMatchesSimple();

        if (matchedTiles.Count > 0)
        {
            // Eşleşme var -> Oyunu Akıt
            yield return StartCoroutine(ProcessGameLoop(matchedTiles));
        }
        else
        {
            // Eşleşme yok -> Geri al
            DoSwap(x1, y1, x2, y2);
            yield return new WaitForSeconds(0.25f);
            isProcessingMove = false;
        }
    }

    IEnumerator ProcessGameLoop(List<GameObject> initialMatches)
    {
        List<GameObject> currentMatches = initialMatches;

        while (currentMatches.Count > 0)
        {
            // --- PATLATMA VE SAVAŞ MANTIĞI ---
            int redCount = 0;   // Hasar
            int blueCount = 0;  // Mana
            bool greenFound = false; // Kalkan

            foreach (GameObject tile in currentMatches)
            {
                if (tile != null)
                {
                    TileController tc = tile.GetComponent<TileController>();
                    
                    // Renk Sayımı (BattleManager için)
                    if (tc.typeID == 1) redCount++;
                    else if (tc.typeID == 2) blueCount++;
                    else if (tc.typeID == 3) greenFound = true;
                    
                    // Yok Etme
                    gridData[tc.x + tc.y * width] = 0; 
                    tileObjects[tc.x, tc.y] = null;    
                    tc.Explode();                      
                }
            }

            // BattleManager'a Veri Gönder
            if (BattleManager.Instance != null)
            {
                if (redCount > 0) BattleManager.Instance.TakeDamage(false, redCount * 10f);
                if (blueCount > 0) BattleManager.Instance.AddMana(blueCount * 5f);
                if (greenFound) BattleManager.Instance.ActivatePlayerShield(5f);
            }

            yield return new WaitForSeconds(0.25f);

            // 2. Yerçekimi (Duvarlara dikkat ederek)
            ApplyGravity();

            // 3. Doldur (Sadece geçerli alanlara)
            RefillBoard();

            yield return new WaitForSeconds(0.25f);

            // 4. Tekrar Kontrol
            currentMatches = FindMatchesSimple();
        }

        // --- DEADLOCK KONTROLÜ (HAMLE KALDI MI?) ---
        if (!HasPossibleMoves())
        {
            Debug.Log("HAMLE KALMADI! Tahta Karıştırılıyor...");
            yield return StartCoroutine(ShuffleBoard());
        }
        else
        {
            isProcessingMove = false;
        }
    }

    // === GRAVITY (DUVAR DESTEKLİ) ===
    void ApplyGravity()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Burası boşsa ve DUVAR DEĞİLSE
                if (gridData[x + y * width] == 0)
                {
                    // Yukarı doğru tara
                    for (int k = y + 1; k < height; k++)
                    {
                        // Eğer yukarıda DUVAR varsa, bu sütunun akışı kesilir. Taş duvarın içinden geçemez.
                        if (gridData[x + k * width] == -1) break;

                        // Eğer taş bulursak aşağı çek
                        if (gridData[x + k * width] > 0)
                        {
                            int tileType = gridData[x + k * width];
                            GameObject tileObj = tileObjects[x, k];

                            gridData[x + y * width] = tileType;
                            gridData[x + k * width] = 0;

                            tileObjects[x, y] = tileObj;
                            tileObjects[x, k] = null;

                            TileController tc = tileObj.GetComponent<TileController>();
                            tc.x = x;
                            tc.y = y;
                            tc.name = $"Tile_{x}_{y}";
                            tc.MoveToPosition(GetTilePosition(x, y));
                            break; 
                        }
                    }
                }
            }
        }
    }

    // === REFILL (DUVAR DESTEKLİ) ===
    void RefillBoard()
    {
        int activeColors = currentElo > 1200 ? 5 : 4;
        float spawnOffsetY = boardPanel.rect.height / 2f + tileSize;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Boşsa ve DUVAR DEĞİLSE doldur
                if (gridData[x + y * width] == 0)
                {
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

    // === BAŞLANGIÇ KURULUMLARI ===
    void SetupWallsFromLevelData()
    {
        // LevelData'daki bool dizisini gridData'ya çevir
        // False = Duvar (-1), True = Boş (0)
        for (int i = 0; i < currentLevel.activeSlots.Length; i++)
        {
            if (i < gridData.Length)
            {
                if (currentLevel.activeSlots[i] == false) gridData[i] = -1; // Duvar
                else gridData[i] = 0; // Açık Alan
            }
        }
    }

    void InitializeBoardLogic(int elo)
    {
        int activeColorCount = elo > 1200 ? 5 : 4; 
        RefillBag(activeColorCount);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Duvarları atla
                if (gridData[x + y * width] == -1) continue;

                int tile = -1; 
                int maxIterations = 0;
                while (true)
                {
                    tile = Random.Range(1, activeColorCount + 1);
                    bool isMatch = false;

                    // Sol Kontrol (Duvar değilse)
                    if (x >= 2) {
                        int t1 = gridData[(x - 1) + y * width];
                        int t2 = gridData[(x - 2) + y * width];
                        if (t1 != -1 && t2 != -1 && t1 == tile && t2 == tile) isMatch = true;
                    }
                    // Aşağı Kontrol (Duvar değilse)
                    if (y >= 2) {
                        int t1 = gridData[x + (y - 1) * width];
                        int t2 = gridData[x + (y - 2) * width];
                        if (t1 != -1 && t2 != -1 && t1 == tile && t2 == tile) isMatch = true;
                    }

                    if (!isMatch) break;
                    maxIterations++;
                    if (maxIterations > 100) break;
                }
                gridData[x + y * width] = tile;
            }
        }
    }

    void DrawBoardUI()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int tileType = gridData[x + y * width];
                
                // Sadece geçerli taşları çiz (Duvarlar ve boşluklar çizilmez)
                if (tileType > 0)
                {
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

    // === DEADLOCK VE SHUFFLE ===
    IEnumerator ShuffleBoard()
    {
        yield return new WaitForSeconds(0.5f);

        // Mevcut taşları topla (Duvarları alma)
        List<int> currentTiles = new List<int>();
        for (int i = 0; i < gridData.Length; i++)
        {
            if (gridData[i] > 0) currentTiles.Add(gridData[i]);
        }

        int maxRetry = 10;
        bool playableBoardFound = false;

        while (!playableBoardFound && maxRetry > 0)
        {
            // Listeyi karıştır
            for (int i = 0; i < currentTiles.Count; i++) {
                int temp = currentTiles[i];
                int r = Random.Range(i, currentTiles.Count);
                currentTiles[i] = currentTiles[r];
                currentTiles[r] = temp;
            }

            // Sanal Yerleştirme
            int listIndex = 0;
            for (int i = 0; i < gridData.Length; i++)
            {
                if (gridData[i] != -1) // Sadece açık alanlara
                {
                    gridData[i] = currentTiles[listIndex];
                    listIndex++;
                }
            }

            if (FindMatchesSimple().Count == 0 && HasPossibleMoves()) playableBoardFound = true;
            else maxRetry--;
        }

        // Görselleri güncelle
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (gridData[x + y * width] > 0)
                {
                    int type = gridData[x + y * width];
                    GameObject tileObj = tileObjects[x, y];
                    TileController tc = tileObj.GetComponent<TileController>();
                    tc.typeID = type;
                    Image img = tileObj.GetComponent<Image>();
                    img.sprite = cachedSprites.Length > type ? cachedSprites[type] : cachedSprites[0];
                }
            }
        }
        
        isProcessingMove = false;
    }

    bool HasPossibleMoves()
    {
        int[] tempGrid = (int[])gridData.Clone();
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                if (tempGrid[x + y * width] == -1) continue; // Duvarı atla

                // Sağa Swap Dene
                if (x < width - 1 && tempGrid[(x + 1) + y * width] != -1) {
                    if (CheckSwapSimulation(x, y, x + 1, y, tempGrid)) return true;
                }
                // Yukarı Swap Dene
                if (y < height - 1 && tempGrid[x + (y + 1) * width] != -1) {
                    if (CheckSwapSimulation(x, y, x, y + 1, tempGrid)) return true;
                }
            }
        }
        return false;
    }

    bool CheckSwapSimulation(int x1, int y1, int x2, int y2, int[] tempGrid)
    {
        int i1 = x1 + y1 * width; int i2 = x2 + y2 * width;
        int t1 = tempGrid[i1]; int t2 = tempGrid[i2];
        
        tempGrid[i1] = t2; tempGrid[i2] = t1;
        
        bool hasMatch = CheckMatchAt(x1, y1, tempGrid) || CheckMatchAt(x2, y2, tempGrid);
        
        tempGrid[i1] = t1; tempGrid[i2] = t2;
        return hasMatch;
    }

    bool CheckMatchAt(int x, int y, int[] grid)
    {
        int type = grid[x + y * width];
        if (type <= 0) return false;

        // Yatay
        if (x >= 2 && grid[(x-1)+y*width] == type && grid[(x-2)+y*width] == type) return true;
        if (x >= 1 && x < width-1 && grid[(x-1)+y*width] == type && grid[(x+1)+y*width] == type) return true;
        if (x < width-2 && grid[(x+1)+y*width] == type && grid[(x+2)+y*width] == type) return true;

        // Dikey
        if (y >= 2 && grid[x+(y-1)*width] == type && grid[x+(y-2)*width] == type) return true;
        if (y >= 1 && y < height-1 && grid[x+(y-1)*width] == type && grid[x+(y+1)*width] == type) return true;
        if (y < height-2 && grid[x+(y+1)*width] == type && grid[x+(y+2)*width] == type) return true;

        return false;
    }

    // === YARDIMCILAR ===
    List<GameObject> FindMatchesSimple()
    {
        HashSet<GameObject> matchedSet = new HashSet<GameObject>();
        // Yatay
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                int currentType = gridData[x + y * width];
                if (currentType <= 0) continue; // 0 veya -1 ise geç
                int matchCount = 1;
                for (int k = x + 1; k < width; k++) {
                    if (gridData[k + y * width] == currentType) matchCount++; else break;
                }
                if (matchCount >= 3) for (int k = 0; k < matchCount; k++) matchedSet.Add(tileObjects[x + k, y]);
            }
        }
        // Dikey
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                int currentType = gridData[x + y * width];
                if (currentType <= 0) continue;
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

    void RefillBag(int maxColors) {
        tileBag.Clear(); for (int i = 1; i <= maxColors; i++) for (int j = 0; j < 10; j++) tileBag.Add(i);
    }

    public int GetNextTileFromBag(int maxColors) {
        if (tileBag.Count == 0) RefillBag(maxColors);
        int index = Random.Range(0, tileBag.Count);
        int selectedTile = tileBag[index]; tileBag.RemoveAt(index); return selectedTile;
    }
}