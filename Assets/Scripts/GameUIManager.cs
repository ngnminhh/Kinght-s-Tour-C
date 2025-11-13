using UnityEngine;
using UniPay;

public class GameUIManager : MonoBehaviour
{
    public GameObject homeCanvas;
    public GameObject gameCanvas;
    public GameObject levelCanvas;
    public GameObject gameBoard;
    public GameObject boardObject;

    public string ticketCurrencyID = "ticket"; // ID loại tiền là "vé"

    public void StartGame()
    {
        homeCanvas.SetActive(false);
        gameCanvas.SetActive(false);
        levelCanvas.SetActive(true);
    }

    public void BackHome()
    {
        homeCanvas.SetActive(true);
        gameCanvas.SetActive(false);
        levelCanvas.SetActive(false);
        gameBoard.SetActive(false);
    }

    public void Size7x7()
    {
        GameConfig.boardSize = 7;
        ShowGame();
    }

    public void Size8x8()
    {
        GameConfig.boardSize = 8;
        ShowGame();
    }

    public void Size9x9()
    {
        GameConfig.boardSize = 9;
        ShowGame();
    }

    private void ShowGame()
    {
        if (!DBManager.GetInstance()) return;

        int currentTickets = DBManager.GetCurrency(ticketCurrencyID);

        if (currentTickets <= 0)
        {
            Debug.Log("Không đủ vé để chơi!");
            return;
        }

        DBManager.SetCurrency(ticketCurrencyID, currentTickets - 1);
        DBManager.Save(); // Tự động lưu và gửi sự kiện cập nhật UI

        homeCanvas.SetActive(false);
        levelCanvas.SetActive(false);
        gameCanvas.SetActive(true);
        gameBoard.SetActive(true);

        BoardManager bm = boardObject.GetComponent<BoardManager>();
        if (bm != null)
            bm.InitBoard();
        else
            Debug.LogError("BoardManager not found!");
    }
    public void ContinueGame()
    {
        if (!PlayerPrefs.HasKey("SavedGame"))
        {
            Debug.Log("❌ Không có dữ liệu game để tiếp tục.");
            return;
        }

        homeCanvas.SetActive(false);
        levelCanvas.SetActive(false);
        gameCanvas.SetActive(true);
        gameBoard.SetActive(true);

        BoardManager bm = boardObject.GetComponent<BoardManager>();
        if (bm != null)
        {
            bm.InitBoard(); // đã tự gọi LoadSavedGame()
        }
        else
        {
            Debug.LogError("BoardManager not found!");
        }
    }

}
