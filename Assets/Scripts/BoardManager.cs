using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject knightPrefab;
    public Transform boardParent;
    public Canvas canvas;
    public GameObject winnerText;

    public AudioClip moveSound;
    private AudioSource audioSource;

    private GameObject knight;
    private int stepCount = 0;
    private TileController[,] tiles;
    private Stack<Vector2Int> history = new Stack<Vector2Int>();

    private Dictionary<int, GameObject> boardCache = new Dictionary<int, GameObject>();
    private Dictionary<int, BoardState> boardStates = new Dictionary<int, BoardState>();

    private GameObject currentBoard;
    private int currentSize = -1;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        canvas.gameObject.SetActive(false);
    }

    public void InitBoard()
    {
        CreateBoard(GameConfig.boardSize);
    }

    void CreateBoard(int size)
    {
        if (currentBoard != null && currentSize > 0)
        {
            SaveBoardState(currentSize);
            currentBoard.SetActive(false);
        }

        if (boardCache.ContainsKey(size))
        {
            currentBoard = boardCache[size];
            currentBoard.SetActive(true);
            currentSize = size;
            tiles = GetTilesFromBoard(currentBoard, size);

            if (boardStates.ContainsKey(size))
                LoadBoardState(boardStates[size]);
            return;
        }

        GameObject boardGO = new GameObject($"Board_{size}x{size}");
        boardGO.transform.SetParent(boardParent, false);
        boardCache[size] = boardGO;
        currentBoard = boardGO;
        currentSize = size;

        float tileSize = 100f;
        float boardOffset = (tileSize * size) / 2f - tileSize / 2f;
        tiles = new TileController[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                GameObject tileGO = Instantiate(tilePrefab, boardGO.transform);
                RectTransform rt = tileGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(tileSize, tileSize);
                rt.anchoredPosition = new Vector2(x * tileSize - boardOffset, y * tileSize - boardOffset);

                TileController tile = tileGO.AddComponent<TileController>();
                tile.Setup(x, y, this);
                tiles[x, y] = tile;

                Image img = tileGO.GetComponent<Image>();
                img.color = (x + y) % 2 == 0 ? Color.white : new Color(0.36f, 0.22f, 0.10f);

                GameObject textGO = new GameObject("StepText");
                textGO.transform.SetParent(tileGO.transform, false);
                Text text = textGO.AddComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.color = Color.green;
                text.fontSize = 45;
                text.fontStyle = FontStyle.Bold;
                text.rectTransform.sizeDelta = rt.sizeDelta;

                tile.stepText = text;
            }
        }

        if (boardStates.ContainsKey(size))
            LoadBoardState(boardStates[size]);
    }

    TileController[,] GetTilesFromBoard(GameObject board, int size)
    {
        TileController[,] result = new TileController[size, size];
        TileController[] children = board.GetComponentsInChildren<TileController>();
        foreach (var tile in children)
        {
            result[tile.x, tile.y] = tile;
        }
        return result;
    }

    void SaveBoardState(int size)
    {
        BoardState state = new BoardState();
        state.stepCount = stepCount;
        state.history = new Stack<Vector2Int>(new Stack<Vector2Int>(history));
        if (history.Count > 0)
            state.currentKnightPos = history.Peek();

        state.stepNumbers = new Dictionary<Vector2Int, int>();
        int n = tiles.GetLength(0);
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                string txt = tiles[x, y].stepText?.text;
                if (!string.IsNullOrEmpty(txt) && int.TryParse(txt, out int step))
                {
                    state.stepNumbers[new Vector2Int(x, y)] = step;
                }
            }
        }
        boardStates[size] = state;
    }

    void LoadBoardState(BoardState state)
    {
        stepCount = state.stepCount;
        history = new Stack<Vector2Int>(new Stack<Vector2Int>(state.history));

        if (knight != null) Destroy(knight);
        if (history.Count > 0)
        {
            knight = Instantiate(knightPrefab, canvas.transform);
            knight.GetComponent<RectTransform>().anchoredPosition = GetTilePosition(state.currentKnightPos);
            knight.transform.SetAsLastSibling();
        }

        foreach (var kv in state.stepNumbers)
        {
            tiles[kv.Key.x, kv.Key.y].SetStepNumber(kv.Value);
        }

        if (history.Count > 0)
            HighlightValidMoves(state.currentKnightPos);
    }

    public void OnTileClicked(TileController tile)
    {
        Vector2Int pos = new Vector2Int(tile.x, tile.y);

        if (stepCount == 0)
        {
            knight = Instantiate(knightPrefab, canvas.transform);
            knight.GetComponent<RectTransform>().anchoredPosition = GetTilePosition(pos);
            knight.transform.SetAsLastSibling();

            tile.SetStepNumber(++stepCount);
            PlayMoveSound();
            history.Push(pos);
            HighlightValidMoves(pos);
        }
        else if (IsValidKnightMove(history.Peek(), pos) && tile.stepText.text == "")
        {
            knight.GetComponent<RectTransform>().anchoredPosition = GetTilePosition(pos);
            knight.transform.SetAsLastSibling();

            tile.SetStepNumber(++stepCount);
            PlayMoveSound();
            history.Push(pos);
            HighlightValidMoves(pos);
        }

        if (stepCount == tiles.Length)
        {
            Debug.Log("🎉 Winner! Completed the Knight's Tour!");
            if (winnerText != null)
            {
                winnerText.SetActive(true);
            }
        }
    }
   



    void PlayMoveSound()
    {
        if (moveSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(moveSound);
        }
    }

    bool IsValidKnightMove(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(from.x - to.x);
        int dy = Mathf.Abs(from.y - to.y);
        return (dx == 2 && dy == 1) || (dx == 1 && dy == 2);
    }

    Vector2 GetTilePosition(Vector2Int pos)
    {
        float size = 100f;
        float boardOffset = (size * tiles.GetLength(0)) / 2f - size / 2f;
        return new Vector2(pos.x * size - boardOffset, pos.y * size - boardOffset);
    }

    public void Undo()
    {
        if (history.Count <= 1) return;

        Vector2Int last = history.Pop();
        tiles[last.x, last.y].SetStepNumber(0);

        Vector2Int current = history.Peek();
        knight.GetComponent<RectTransform>().anchoredPosition = GetTilePosition(current);

        stepCount--;
        HighlightValidMoves(current);
    }

    void HighlightValidMoves(Vector2Int currentPos)
    {
        ClearHighlights();

        int[] dx = { 2, 1, -1, -2, -2, -1, 1, 2 };
        int[] dy = { 1, 2, 2, 1, -1, -2, -2, -1 };

        for (int i = 0; i < 8; i++)
        {
            int nx = currentPos.x + dx[i];
            int ny = currentPos.y + dy[i];

            int size = tiles.GetLength(0);
            if (nx >= 0 && nx < size && ny >= 0 && ny < size)
            {
                TileController tile = tiles[nx, ny];
                if (tile.stepText.text == "")
                {
                    tile.Highlight(true);
                }
            }
        }
    }

    void ClearHighlights()
    {
        foreach (TileController tile in tiles)
        {
            tile.Highlight(false);
        }
    }

    public void Restart()
    {
        if (knight != null)
        {
            Destroy(knight);
            knight = null;
        }

        stepCount = 0;
        history.Clear();

        // Xóa dữ liệu trực tiếp từ các tile trong currentBoard
        TileController[] allTiles = currentBoard.GetComponentsInChildren<TileController>();
        foreach (TileController tile in allTiles)
        {
            tile.SetStepNumber(0);
            tile.Highlight(false);
        }

        if (winnerText != null)
        {
            winnerText.SetActive(false);
        }

        // Xóa trạng thái cũ khỏi boardStates
        if (boardStates.ContainsKey(currentSize))
        {
            boardStates.Remove(currentSize);
        }
    }


}

class BoardState
{
    public int stepCount;
    public Vector2Int currentKnightPos;
    public Stack<Vector2Int> history = new Stack<Vector2Int>();
    public Dictionary<Vector2Int, int> stepNumbers = new Dictionary<Vector2Int, int>();
}