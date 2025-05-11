// WaveManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Configuration")]
    public List<WaveConfig> waveConfigurations;
    public float timeBetweenWaves = 5f;

    [Header("Goat Prefabs")]
    public GameObject peacefulGoatPrefab;
    public GameObject enemyGoatPrefab;

    [Header("Camera-Edge Spawning")]
    public Camera gameCamera; // Assign your main game camera here
    public float spawnOffset = 1.5f; // How far off-screen (in world units) to spawn the goats. Adjust this!
    public float spawnDepth = 10f;   // For perspective cameras: distance from camera. For ortho: used in calculation.

    [Header("UI Dialogue Card")]
    public GameObject dialogueCardUI;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI waveNumberText;

    [Header("Audio")]
    public AudioSource musicAudioSource;
    public AudioClip peacefulMusic;

    private int currentWaveIndex = -1;
    private int peacefulGoatsSpawnedThisWave = 0;
    private int goatsherdedThisWave = 0;


    [SerializeField]
    private enum WaveState { Idle, Spawning, InProgress, Dialogue, Cooldown, AllWavesComplete }
    [SerializeField]

    private WaveState currentState;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        if (gameCamera == null)
        {
            gameCamera = Camera.main; // Attempt to find the main camera if not assigned
            if (gameCamera == null)
            {
                Debug.LogError("WaveManager: Game Camera is not assigned and Camera.main could not be found!");
                enabled = false;
                return;
            }
        }
    }

    void Start()
    {
        if (waveConfigurations == null || waveConfigurations.Count == 0)
        {
            Debug.LogError("WaveManager: No wave configurations assigned!");
            enabled = false;
            return;
        }
        if (!peacefulGoatPrefab || !enemyGoatPrefab)
        {
            Debug.LogError("WaveManager: Goat prefabs not assigned!");
            enabled = false;
            return;
        }
        // Removed spawnPoints check

        if (dialogueCardUI) dialogueCardUI.SetActive(false);

        currentState = WaveState.Idle;
        //PlayMusic(peacefulMusic);
        Debug.Log("Wave Manager Initialized. Call StartNextWaveSequence() to begin.");
        Invoke(nameof(StartNextWaveSequence), 2f); // To start automatically
    }

    public void StartNextWaveSequence()
    {
        if (currentState == WaveState.Idle || currentState == WaveState.Cooldown)
        {
            StartCoroutine(WaveSequenceCoroutine());
        }
    }

    IEnumerator WaveSequenceCoroutine()
    {
        currentWaveIndex++;

        if (currentWaveIndex >= waveConfigurations.Count)
        {
            AllWavesCompleted();
            yield break;
        }

        WaveConfig currentWave = waveConfigurations[currentWaveIndex];
        currentState = WaveState.Spawning;
        Debug.Log($"Starting Wave {currentWave.waveNumber}");

        if (waveNumberText) waveNumberText.text = "Wave: " + currentWave.waveNumber;
        PlayMusic(currentWave.waveMusic);

        peacefulGoatsSpawnedThisWave = currentWave.peacefulGoatsToSpawn;
        goatsherdedThisWave = 0;

        for (int i = 0; i < currentWave.peacefulGoatsToSpawn; i++)
        {
            SpawnGoatNearCameraEdge(peacefulGoatPrefab);
            yield return null;
        }

        for (int i = 0; i < currentWave.enemyGoatsToSpawn; i++)
        {
            SpawnGoatNearCameraEdge(enemyGoatPrefab);
            yield return null;
        }

        currentState = WaveState.InProgress;
        Debug.Log($"Wave {currentWave.waveNumber} in progress. {peacefulGoatsSpawnedThisWave} peaceful goats to watch.");
    }

    void SpawnGoatNearCameraEdge(GameObject goatPrefab)
    {
        if (gameCamera == null)
        {
            Debug.LogError("WaveManager: Game Camera not set for spawning!");
            return;
        }

        float zDepth = spawnDepth;
        if (gameCamera.orthographic)
        {
            zDepth = goatPrefab.transform.position.z - gameCamera.transform.position.z;
        }


        // Choose a random edge (0: bottom, 1: top, 2: left, 3: right)
        int edgeIndex = Random.Range(0, 4);
        Vector3 spawnViewportPosition = Vector3.zero;

        // Determine a point on the viewport edge
        switch (edgeIndex)
        {
            case 0: // Bottom edge
                spawnViewportPosition = new Vector3(Random.value, 0f, zDepth);
                break;
            case 1: // Top edge
                spawnViewportPosition = new Vector3(Random.value, 1f, zDepth);
                break;
            case 2: // Left edge
                spawnViewportPosition = new Vector3(0f, Random.value, zDepth);
                break;
            case 3: // Right edge
                spawnViewportPosition = new Vector3(1f, Random.value, zDepth);
                break;
        }

        // Convert viewport point at edge to world point
        Vector3 worldPointAtEdge = gameCamera.ViewportToWorldPoint(spawnViewportPosition);
        Vector3 spawnDirection = Vector3.zero;

        // Determine direction to offset from the edge
        // This uses the camera's orientation to ensure "off-screen" is correct
        switch (edgeIndex)
        {
            case 0: spawnDirection = -gameCamera.transform.up; break;    // Offset down
            case 1: spawnDirection = gameCamera.transform.up; break;     // Offset up
            case 2: spawnDirection = -gameCamera.transform.right; break; // Offset left
            case 3: spawnDirection = gameCamera.transform.right; break;  // Offset right
        }

        Vector3 spawnPosition = worldPointAtEdge + spawnDirection * spawnOffset;

        // If orthographic, ensure Z is correct for the 2D plane
        if (gameCamera.orthographic)
        {
            spawnPosition.z = goatPrefab.transform.position.z; // Or your desired 2D spawn Z
        }

        Instantiate(goatPrefab, spawnPosition, Quaternion.identity);
    }


    public void OnPeacefulGoatConvertedInGameManager() // This name was from thought process, let's use the actual call
    {
        // This is now handled by NotifyPeacefulGoatConverted directly
    }

    void EndWave()
    {
        if (currentState != WaveState.InProgress) return;

        WaveConfig currentWave = waveConfigurations[currentWaveIndex];
        Debug.Log($"Wave {currentWave.waveNumber} completed!");
        currentState = WaveState.Dialogue;
    
        if (dialogueCardUI && dialogueText)
        {
                dialogueCardUI.SetActive(true);
            TypeDialogueText(currentWave.endOfWaveDialogue, dialogueText, 5f); // Assuming 20 characters per second
            StartCoroutine(DialoguePhaseCoroutine());
        }
        else
        {
            StartCooldown();
        }
    }

    IEnumerator DialoguePhaseCoroutine()
    {
        yield return new WaitForSeconds(5f); // Or wait for player input
        CloseDialogueAndStartCooldown();
    }

    public void CloseDialogueAndStartCooldown()
    {
        if (dialogueCardUI) dialogueCardUI.SetActive(false);
        StartCooldown();
    }

    void StartCooldown()
    {
        currentState = WaveState.Cooldown;
        PlayMusic(peacefulMusic);

        if (currentWaveIndex >= waveConfigurations.Count - 1)
        {
            AllWavesCompleted();
        }
        else
        {
            Debug.Log($"Starting cooldown for {timeBetweenWaves} seconds before next wave.");
            StartCoroutine(CooldownCoroutine());
        }
    }

    IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(timeBetweenWaves);
        StartNextWaveSequence();
    }

    void AllWavesCompleted()
    {
        currentState = WaveState.AllWavesComplete;
        Debug.Log("All waves completed! Congratulations!");
        PlayMusic(peacefulMusic);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.WinGame("All waves survived! The goats are safe... for now!");
        }
    }

    void PlayMusic(AudioClip clip)
    {
        if (musicAudioSource && clip)
        {
            if (musicAudioSource.isPlaying && musicAudioSource.clip == clip) return;
            musicAudioSource.Stop();
            musicAudioSource.clip = clip;
            musicAudioSource.Play();
        }
        else if (musicAudioSource && clip == null)
        {
            musicAudioSource.Stop();
            musicAudioSource.clip = null;
        }
    }

    public void NotifyHerdedGoatConverted()
    {
        if (currentState == WaveState.InProgress)
        {
            
            Debug.Log($"A peaceful goat was converted to a cyborg. Converted this wave: {goatsherdedThisWave}/{peacefulGoatsSpawnedThisWave}");
            goatsherdedThisWave--;
            Debug.Log($"A peaceful goat was converted. Converted this wave: {goatsherdedThisWave}/{peacefulGoatsSpawnedThisWave}");
            if (goatsherdedThisWave >= peacefulGoatsSpawnedThisWave)
            {
                EndWave();
            }
        }
    }

    public void NotifyPeacefulGoatHerded()
    {
        if (currentState == WaveState.InProgress)
        {
            goatsherdedThisWave++;
            Debug.Log($"A peaceful goat was converted. Converted this wave: {goatsherdedThisWave}/{peacefulGoatsSpawnedThisWave}");

            if (goatsherdedThisWave >= peacefulGoatsSpawnedThisWave)
            {
                EndWave();
            }
        }
    }

    IEnumerator TypeDialogueText(string textToType, TextMeshProUGUI targetText, float charactersPerSecond)
    {
        targetText.text = "";
        float delay = 1f / charactersPerSecond;
        foreach (char letter in textToType.ToCharArray())
        {
            targetText.text += letter;
            yield return new WaitForSeconds(delay);
        }
    }

    // Example of how you might call this from DialoguePhaseCoroutine:
    // IEnumerator DialoguePhaseCoroutine()
    // {
    //     WaveConfig currentWave = waveConfigurations[currentWaveIndex];
    //     if (dialogueCardUI && dialogueText)
    //     {
    //         dialogueCardUI.SetActive(true);
    //         // Assuming you have a characters per second value, e.g., 20
    //         yield return StartCoroutine(TypeDialogueText(currentWave.endOfWaveDialogue, dialogueText, 20f)); 
    //         yield return new WaitForSeconds(2f); // Extra delay after typing finishes
    //     }
    //     CloseDialogueAndStartCooldown();
    // }
}