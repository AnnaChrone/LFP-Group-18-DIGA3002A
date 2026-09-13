using System.Collections;
using UnityEngine;

public class TimeCycleManager : MonoBehaviour
{
    public static TimeCycleManager Instance;

    [Header("Current State")]
    public TimeOfDay currentTime = TimeOfDay.Morning;
    public GameLocation currentLocation = GameLocation.Dock;

    [Header("Player & Spawn Points")]
    public GameObject player;
    public Transform dockSpawnPoint;
    public Transform fishingSpawnPoint;
    public Transform restaurantSpawnPoint;

    [Header("Environment Lighting/Visuals")]
    public SpriteRenderer backgroundSky;
    public Color morningColor = Color.white;
    public Color middayColor = new Color(0.7f, 0.9f, 1f); 
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f);  

    [Header("Transition Settings")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.5f;

    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateEnvironmentVisuals();
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }
    }

    public void ProgressTimeAndLocation()
    {
        if (isTransitioning) return; // Prevent double trigger inputs

        if (currentTime == TimeOfDay.Morning && currentLocation == GameLocation.Dock)
        {
            StartCoroutine(TransitionRoutine(TimeOfDay.Midday, GameLocation.FishingArea, fishingSpawnPoint.position));
        }
        else if (currentTime == TimeOfDay.Midday && currentLocation == GameLocation.FishingArea)
        {
            StartCoroutine(TransitionRoutine(TimeOfDay.Night, GameLocation.Dock, dockSpawnPoint.position));
        }
        else if (currentTime == TimeOfDay.Night && currentLocation == GameLocation.Dock)
        {
            StartCoroutine(TransitionRoutine(TimeOfDay.Night, GameLocation.Restaurant, restaurantSpawnPoint.position));
        }
    }

    public void EndRestaurantShift()
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(TimeOfDay.Morning, GameLocation.Dock, dockSpawnPoint.position));
    }

    private IEnumerator TransitionRoutine(TimeOfDay newTime, GameLocation newLocation, Vector3 spawnPosition)
    {
        isTransitioning = true;

        // 1. Fade to Black
        yield return StartCoroutine(Fade(1f));

        // 2. Perform the actual transition state changes while screen is black
        currentTime = newTime;
        currentLocation = newLocation;
        player.transform.position = spawnPosition;
        UpdateEnvironmentVisuals();

        // Small pause at complete black for a cleaner feel
        yield return new WaitForSeconds(0.2f);

        // 3. Fade back to gameplay
        yield return StartCoroutine(Fade(0f));

        isTransitioning = false;
    }

    private IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;

        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    private void UpdateEnvironmentVisuals()
    {
        if (backgroundSky == null) return;

        switch (currentTime)
        {
            case TimeOfDay.Morning:
                backgroundSky.color = morningColor;
                break;
            case TimeOfDay.Midday:
                backgroundSky.color = middayColor;
                break;
            case TimeOfDay.Night:
                backgroundSky.color = nightColor;
                break;
        }
    }
}
