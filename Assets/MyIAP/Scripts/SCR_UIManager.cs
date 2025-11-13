using System;
using UnityEngine;
using UnityEngine.UI;

public class SCR_UIManager : MonoBehaviour
{
    [SerializeField] public GameObject PFB_StorePopup;

    [SerializeField] public Button btnStore;

    public Action ON_STORE_BUTTON_CLICKED;
    public Action ON_STORE_CLOSED;
    public GameObject storeBTN;
    public GameObject button_TICKET;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PFB_StorePopup.SetActive(false);
    }

    public void ShowStore()
    {
        ON_STORE_BUTTON_CLICKED?.Invoke();
        PFB_StorePopup.transform.SetAsLastSibling();
        PFB_StorePopup.SetActive(true);
        HideStoreButton();
        storeBTN.SetActive(false);
        button_TICKET.SetActive(false);
    }

    public void HideStore()
    {
        ON_STORE_CLOSED?.Invoke();
        PFB_StorePopup.SetActive(false);
        storeBTN.SetActive(true);
        button_TICKET.SetActive(true);
    }
    
    public void ShowStoreButton()
    {
        btnStore.gameObject.SetActive(true);
    }

    public void HideStoreButton()
    {
        btnStore.gameObject.SetActive(false);
    }
}
