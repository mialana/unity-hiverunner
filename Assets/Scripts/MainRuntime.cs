using UnityEngine;

public class MainRuntime : MonoBehaviour
{
    public GameObject player;
    int score;

    [Header("Scoring Parameters")]
    // base points per second (much smaller so passive gain is slow)
    public float timeMultiplier = 0.2f; // reduced from 5f

    // how strongly height (in rows) influences scoring
    public float heightMultiplier = 3f; // stronger effect per row
    public float baselineY = 0f; // world Y considered "ground" for scoring
    public float heightExponent = 1.5f; // nonlinearity for height effect

    // height of a single row of cells (provided context: each row = 10 units)
    public float cellHeight = 10f;

    // extra points per row climbed (not per unit)
    public float upwardBonusPerRow = 5f; // reduced and per-row
    public float peakInterval = 10f; // seconds between periodic peak bonuses
    public int peakBonus = 10; // points given every peakInterval

    // internal state
    private float accumulatedScore = 0f;
    private int lastPlayerRow = 0;
    private float lastPeakTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        accumulatedScore = score;
        if (player != null)
        {
            lastPlayerRow = Mathf.FloorToInt(
                player.transform.position.y / Mathf.Max(0.0001f, cellHeight)
            );
        }
        else
        {
            lastPlayerRow = 0;
        }
        lastPeakTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        // base time-driven score (much smaller)
        float frameGain = timeMultiplier * dt;

        // if player exists, modify based on their world Y and motion
        if (player != null)
        {
            float py = player.transform.position.y;

            // Treat height in rows so climbing rows matters more than raw Y units
            float rowsAbove = Mathf.Max(0f, (py - baselineY) / Mathf.Max(0.0001f, cellHeight));
            // heightFactor roughly behaves like (rows+1)^exp - 1
            float heightFactor = Mathf.Pow(rowsAbove + 1f, Mathf.Max(0.001f, heightExponent)) - 1f;
            // Apply height influence to frameGain (rows dominate scoring)
            frameGain *= 1f + heightFactor * heightMultiplier;

            // Reward discrete row crossings instead of unit movement
            int currentRow = Mathf.FloorToInt(py / Mathf.Max(0.0001f, cellHeight));
            int deltaRows = currentRow - lastPlayerRow;
            if (deltaRows > 0)
            {
                accumulatedScore += deltaRows * upwardBonusPerRow;
            }
            lastPlayerRow = currentRow;
        }

        accumulatedScore += frameGain;

        // periodic peak bonus
        if (Time.time - lastPeakTime >= peakInterval)
        {
            // grant one or multiple peaks if game was paused/lagged
            int peaks = Mathf.FloorToInt((Time.time - lastPeakTime) / peakInterval);
            accumulatedScore += peaks * peakBonus;
            lastPeakTime += peaks * peakInterval;
        }

        // publish integer score
        int newScore = Mathf.FloorToInt(accumulatedScore);
        if (newScore != score)
            score = newScore;

        // Debug.Log(score);
    }
}
