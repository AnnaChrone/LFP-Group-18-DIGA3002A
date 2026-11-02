using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class NightSummaryManager : MonoBehaviour
{

    public static NightSummaryManager Instance { get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Night Statistics")]
    public int NoOfCompleted = 0;
    public string MostPopular = "None";
    public float orderProfits = 0f;
    public float cancellationDeductions = 0f;
    public float nightlyTax = 10f;
    public float OverallEarnings = 0f;

    private readonly System.Collections.Generic.Dictionary<string, int> dishCounts =
    new System.Collections.Generic.Dictionary<string, int>();
    private int topCount = 0;



    [Header("Summary Display")]
    public GameObject SummaryPanel;
    public TextMeshProUGUI TotalOrder;
    public TextMeshProUGUI PopularDish;
    public TextMeshProUGUI OrderProfits;
    public TextMeshProUGUI CancellationDeduc;
    public TextMeshProUGUI Tax;
    public TextMeshProUGUI OrderTotal;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SummaryPanel.SetActive(false);
    }

    public void ResetNightSummary()
    {
        NoOfCompleted = 0;
        MostPopular = "None";
        orderProfits = 0f;
        cancellationDeductions = 0f;
        OverallEarnings = 0;
        dishCounts.Clear();
        topCount = 0;
    }

    public void IncreaseOrderCount()
    {
        NoOfCompleted++;
    }

    public void RecordDishServed(string dishName)
    {
        dishCounts.TryGetValue(dishName, out int count);
        count++;
        dishCounts[dishName] = count;

        if (count > topCount)          // new leader (a tie keeps whoever got there first)
        {
            topCount = count;
            MostPopular = dishName;
        }
    }

    public void IncreaseOrderProfits(float amount)
    {
        orderProfits += amount;
    }

    public void IncreaseCancellationDeductions(float amount)
    {
        cancellationDeductions += amount;
    }


    public void OverallEarningsCalc()
    {
        OverallEarnings = orderProfits - cancellationDeductions - nightlyTax;
    }

    public void DisplaySummary()
    {
        OverallEarningsCalc();

        TotalOrder.text = "Total Orders Completed: " + NoOfCompleted;
        PopularDish.text = "Most Popular Dish: " + MostPopular;
        OrderProfits.text = "Profit from Orders: +" + orderProfits + " Gold";
        CancellationDeduc.text = "Cancellation Deduction from Active Orders: -" + cancellationDeductions + " Gold";
        Tax.text = "Nightly Tax: -" + nightlyTax + " Gold";
        OrderTotal.text = "Overall Profit: " + OverallEarnings + " Gold" ;

        SummaryPanel.SetActive(true);
    }

    public void CloseSummary()
    {
        SummaryPanel.SetActive(false);
    }
}
