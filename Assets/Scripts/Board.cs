using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public GameObject tilePrefab;
    public Transform boardParent;

    private BoardManager boardManager;

    void Start()
    {
        boardManager = GetComponent<BoardManager>();
        CreateBoard(GameConfig.boardSize);
    }

    void CreateBoard(int boardSize)
    {
        float tileSize = 80f;

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                GameObject tileObj = Instantiate(tilePrefab, boardParent);
                RectTransform rt = tileObj.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(tileSize, tileSize);
                rt.anchoredPosition = new Vector2(
                    (x - (boardSize - 1) / 2f) * tileSize,
                    (y - (boardSize - 1) / 2f) * tileSize
                );

                Image img = tileObj.GetComponent<Image>();
                img.color = (x + y) % 2 == 0 ? Color.white : new Color(0.36f, 0.22f, 0.10f);

                TileController tile = tileObj.GetComponent<TileController>();
                tile.Setup(x, y, boardManager);
            }
        }
    }
}
