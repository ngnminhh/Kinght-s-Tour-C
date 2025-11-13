using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TileController : MonoBehaviour, IPointerClickHandler
{
    public int x, y;
    public Text stepText;
    private BoardManager board;

    /// <summary>
    /// Gán thông tin ô và tham chiếu tới BoardManager
    /// </summary>
    public void Setup(int x, int y, BoardManager board)
    {
        this.x = x;
        this.y = y;
        this.board = board;

        if (board == null)
        {
            Debug.LogError($"Tile [{x},{y}] setup failed: board is null!");
        }
    }

    /// <summary>
    /// Hiển thị số bước lên ô (hoặc xoá nếu = 0)
    /// </summary>
    public void SetStepNumber(int step)
    {
        if (stepText != null)
        {
            stepText.text = step == 0 ? "" : step.ToString();
        }
        else
        {
            Debug.LogWarning($"Tile [{x},{y}] stepText is null.");
        }
    }

    /// <summary>
    /// Khi click vào tile
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
         Debug.Log($"Tile clicked at ({x},{y})");
        if (board != null)
        {
            board.OnTileClicked(this);
        }
        else
        {
            Debug.LogError($"Tile [{x},{y}] was clicked but board is null!");
        }
    }
    public void Highlight(bool isOn)
    {
        Image img = GetComponent<Image>();
        if (isOn)
            img.color = Color.yellow;
        else
            img.color = (x + y) % 2 == 0 ? Color.white : new Color(0.36f, 0.22f, 0.10f);
    }

}
