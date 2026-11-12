using UnityEngine;
using UnityEngine.UI;

public class Book : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject BookUI;

    [Header("Tabs")]
    public Sprite fishSelected;
    public Sprite fishUnselected;
    public Sprite sushiSelected;
    public Sprite sushiUnselected;
    public Sprite WinSelected;
    public Sprite WinUnselected;

    public Image Fish;
    public Image Sushi;
    public Image Win;

    [Header("Pages")]
    public GameObject fishPages;
    public GameObject sushiPages;
    public GameObject WinPages;
    public GameObject storedPage;

    [Header("Fish Page Content")]
    public Image fishIcon;

    [Header("Sushi Page Content")]
    public Image sushiIcon;

    [Header("Win Page Content")]
    public Image Fill;
    public Button PayoutButton;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void toggleBook()
    {
        if (BookUI.activeInHierarchy)
        {
            BookUI.SetActive(false);
        } else
        {
            BookUI.SetActive(true);
        }
    }

    public void openFishPages()
    {
        storedPage.SetActive(false);
        storedPage = fishPages;
        storedPage.SetActive(true);


        Fish.sprite = fishSelected;
        Sushi.sprite = sushiUnselected;
        Win.sprite = WinUnselected;
    }

    public void openSushiPages()
    {
        storedPage.SetActive(false);
        storedPage = sushiPages;
        storedPage.SetActive(true);

        Sushi.sprite = sushiSelected;
        Win.sprite = WinUnselected;
        Fish.sprite = fishUnselected;

    }

    public void openWinPages()
    {
        storedPage.SetActive(false);
        storedPage = WinPages;
        storedPage.SetActive(true);


        Win.sprite = WinSelected;
        Sushi.sprite = sushiUnselected;
        Fish.sprite = fishUnselected;



    }
}
