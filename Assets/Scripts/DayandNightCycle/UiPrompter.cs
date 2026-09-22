using System.Collections;
using TMPro;
using UnityEditor;
using UnityEngine;

public class UiPrompter : MonoBehaviour
{
    public static UiPrompter Instance;


    [SerializeField] private Sushi.Inventory.Inventory inventory;

    public GameObject FullBagText;

    public CanvasGroup OrdercompleteFade;
    public TextMeshProUGUI OrdercompleteText;

    public TextMeshProUGUI noingredientsText;
    public CanvasGroup noIngredientsFade;

    private Coroutine orderMessageCoroutine;
    private Coroutine ingredientMessageCoroutine;
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        FullBagText.SetActive(false);

        OrdercompleteFade.alpha = 0f;
        noIngredientsFade.alpha = 0f;
    }

    void Update()
    {
        if (inventory.IsFull)
        {
            FullBagText.SetActive(true);
        }
        else
        {
            FullBagText.SetActive(false);
        }
    }

    public void IncorrectOrder()
    {
        OrdercompleteText.text = "Order failed";

        if (orderMessageCoroutine != null)
        {
            StopCoroutine(orderMessageCoroutine);
        }

        orderMessageCoroutine = StartCoroutine(FadeMessage(OrdercompleteFade));
    }

    public void CorrectOrder()
    {
        OrdercompleteText.text = "Order completed";

        if (orderMessageCoroutine != null)
        {
            StopCoroutine(orderMessageCoroutine);
        }

        orderMessageCoroutine = StartCoroutine(FadeMessage(OrdercompleteFade));
    }

    public void noIngredients()
    {
        noingredientsText.text = "Not enough ingredients";

        if (ingredientMessageCoroutine != null)
        {
            StopCoroutine(ingredientMessageCoroutine);
        }

        ingredientMessageCoroutine = StartCoroutine(FadeMessage(noIngredientsFade));
    }

    private IEnumerator FadeMessage(CanvasGroup canvasGroup)
    {
        // Fade in
        canvasGroup.alpha = 0f;

        float fadeTime = 0.3f;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        // Stay visible
        yield return new WaitForSeconds(1.4f);

        // Fade out
        timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }


    public void Exit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

