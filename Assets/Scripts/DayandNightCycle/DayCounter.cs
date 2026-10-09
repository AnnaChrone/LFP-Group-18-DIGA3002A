using UnityEngine;
using TMPro;

public class DayCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text dayLabel;

    public int DaysPlayed { get; private set; }

    private const string SaveKey = "DaysPlayed";

    private void Awake()
    {
        DaysPlayed = PlayerPrefs.GetInt(SaveKey, 0);
        UpdateLabel();
    }

    // Call this once each time the game enters the Day state
    public void OnDayStarted()
    {
        DaysPlayed++;
        PlayerPrefs.SetInt(SaveKey, DaysPlayed);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (dayLabel != null)
            dayLabel.text = $"Day {DaysPlayed}";
    }
}