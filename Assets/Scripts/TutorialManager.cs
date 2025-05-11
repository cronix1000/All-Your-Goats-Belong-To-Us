// TutorialSequenceManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // For TextMeshPro UI

public class TutorialSequenceManager : MonoBehaviour
{
    public static TutorialSequenceManager Instance { get; private set; }

    [Header("Tutorial Step Configuration")]
    public List<WaveConfig> tutorialSteps;
    public float timeBetweenSteps = 3f; // Time after completing a step before starting next instructions

    [Header("Character Prefabs (if applicable)")]
    public GameObject peacefulGoatPrefab; // Example: if tutorial involves these
    public GameObject enemyGoatPrefab;   // Example: if tutorial involves these

    [Header("Spawning (if applicable)")]
    public Camera gameCamera;
    public float spawnOffset = 1.5f;
    public float spawnDepth = 10f;
    public float autoProceedTime = 3f; // Time to wait before auto-proceeding to next step

    [Header("UI Elements")]
    public GameObject instructionCardUI; // Renamed from dialogueCardUI
    public TextMeshProUGUI instructionTextDisplay; // Renamed from dialogueText
    public TextMeshProUGUI stepNumberDisplay; // Optional: Renamed from waveNumberText
    // public Button continueButton; // Assign if requirePlayerContinue is true for any step

    [Header("Audio")]
    public AudioSource musicAudioSource;
    public AudioClip defaultTutorialMusic; // Fallback music

    private int currentStepIndex = -1;
    private Coroutine typingCoroutine;
    private Coroutine currentStepCoroutine;

    // --- Fields related to example goat herding objectives ---
    // These should be adapted or removed if your tutorial has different objectives
    private int peacefulGoatsToSpawnThisStep = 0;
    private int goatsHerdedThisStep = 0;
    private bool waitingForPlayerToProceed = false;
    // --- End goat herding objective fields ---


    private enum TutorialState
    {
        Idle,
        ShowingInstructions,
        SpawningEntities,
        StepInProgress,
        StepCompleteTransition, // Cooldown or waiting for next step
        AllStepsComplete
    }
    [SerializeField]
    private TutorialState currentState;

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
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                Debug.LogError("TutorialSequenceManager: Game Camera is not assigned and Camera.main could not be found!");
                enabled = false;
            }
        }
    }

    void Start()
    {
        if (tutorialSteps == null || tutorialSteps.Count == 0)
        {
            Debug.LogError("TutorialSequenceManager: No tutorial steps assigned!");
            enabled = false;
            return;
        }
        // Optional: Check for goat prefabs if your tutorial universally needs them
        // if (!peacefulGoatPrefab || !enemyGoatPrefab)
        // {
        //     Debug.LogError("TutorialSequenceManager: Goat prefabs not assigned (if needed for tutorial steps)!");
        //      enabled = false;
        //      return;
        // }

        if (instructionCardUI) instructionCardUI.SetActive(false);
        // if (continueButton) continueButton.gameObject.SetActive(false); // Hide continue button initially

        currentState = TutorialState.Idle;
        PlayMusic(defaultTutorialMusic); // Play some ambient music
        Debug.Log("Tutorial Sequence Manager Initialized. Call StartNextStepSequence() to begin.");
        // Auto-start the first step after a delay, or call this from a "Start Tutorial" button
        Invoke(nameof(StartNextStepSequence), 2f);
    }

    public void StartNextStepSequence()
    {
        if (currentState == TutorialState.Idle || currentState == TutorialState.StepCompleteTransition)
        {
            if (currentStepCoroutine != null)
            {
                StopCoroutine(currentStepCoroutine);
            }
            currentStepCoroutine = StartCoroutine(TutorialStepCoroutine());
        }
    }

    IEnumerator TutorialStepCoroutine()
    {
        currentStepIndex++;

        if (currentStepIndex >= tutorialSteps.Count)
        {
            AllTutorialStepsCompleted();
            yield break;
        }

        WaveConfig currentStep = tutorialSteps[currentStepIndex];
        currentState = TutorialState.ShowingInstructions;
        Debug.Log($"Starting Tutorial Step {currentStep.waveNumber}: {currentStep.name}");

        if (stepNumberDisplay) stepNumberDisplay.text = "Step: " + currentStep.waveNumber;
        PlayMusic(currentStep.waveMusic);

        // --- Display Instructions ---
        if (instructionCardUI && instructionTextDisplay)
        {
            instructionTextDisplay.text = ""; // Clear previous text
            instructionCardUI.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeWriterEffect(currentStep.endOfWaveDialogue, instructionTextDisplay, 30f)); // Adjust chars/sec

            yield return new WaitForSeconds(autoProceedTime); // Wait for auto-proceed time
            
            instructionCardUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("TutorialSequenceManager: Instruction UI not assigned. Skipping instruction display.");
        }
        // --- End Display Instructions ---


        // --- Setup and Spawn Entities for the Step (Example: Goats) ---
        // This section should be highly customized based on currentStep.actionToComplete or similar
        currentState = TutorialState.SpawningEntities;
        Debug.Log($"Spawning entities for step {currentStep.waveNumber}.");

        peacefulGoatsToSpawnThisStep = currentStep.peacefulGoatsToSpawn; // From TutorialStepConfig
        goatsHerdedThisStep = 0;                                         // Reset for the new step

        for (int i = 0; i < currentStep.peacefulGoatsToSpawn; i++)
        {
            if (peacefulGoatPrefab) SpawnEntityNearCameraEdge(peacefulGoatPrefab);
            yield return null; // Small delay between spawns if needed
        }

        for (int i = 0; i < currentStep.enemyGoatsToSpawn; i++)
        {
            if (enemyGoatPrefab) SpawnEntityNearCameraEdge(enemyGoatPrefab);
            yield return null;
        }
        // --- End Spawning ---

        currentState = TutorialState.StepInProgress;
        Debug.Log($"Tutorial Step {currentStep.waveNumber} in progress. Objectives: Spawned {peacefulGoatsToSpawnThisStep} peaceful goats.");
        // If the step has no spawn/herding objectives, you might immediately call EndCurrentStep()
        // or wait for a different completion condition.
        if (peacefulGoatsToSpawnThisStep == 0 && currentStep.enemyGoatsToSpawn == 0) // Example: if it's just an info step
        {
            // If this step has no active gameplay objective that's automatically tracked,
            // you might want a different way to complete it (e.g. player performs a specific action that calls EndCurrentStep)
            // For now, if no goats to spawn, consider it "complete" for demonstration.
            // This is a placeholder for more complex objective tracking.
            Debug.Log($"Step {currentStep.waveNumber} has no goat objectives, proceeding to transition.");
            EndCurrentStep();
        }
    }

    // Call this method from a UI Button if requirePlayerContinue is true for the current step
    public void PlayerProceededFromInstructions()
    {
        if (currentState == TutorialState.ShowingInstructions && waitingForPlayerToProceed)
        {
            waitingForPlayerToProceed = false;
            // if (continueButton) continueButton.gameObject.SetActive(false);
            Debug.Log("Player proceeded from instructions.");
        }
    }

    void SpawnEntityNearCameraEdge(GameObject entityPrefab)
    {
        if (gameCamera == null || entityPrefab == null)
        {
            Debug.LogError("TutorialSequenceManager: Camera or Prefab not set for spawning!");
            return;
        }

        float zDepth = spawnDepth;
        if (gameCamera.orthographic)
        {
            zDepth = entityPrefab.transform.position.z - gameCamera.transform.position.z;
        }

        int edgeIndex = Random.Range(0, 4); // 0:bottom, 1:top, 2:left, 3:right
        Vector3 spawnViewportPosition = Vector3.zero;

        switch (edgeIndex)
        {
            case 0: spawnViewportPosition = new Vector3(Random.value, 0f, zDepth); break; // Bottom
            case 1: spawnViewportPosition = new Vector3(Random.value, 1f, zDepth); break; // Top
            case 2: spawnViewportPosition = new Vector3(0f, Random.value, zDepth); break; // Left
            case 3: spawnViewportPosition = new Vector3(1f, Random.value, zDepth); break; // Right
        }

        Vector3 worldPointAtEdge = gameCamera.ViewportToWorldPoint(spawnViewportPosition);
        Vector3 spawnDirection = Vector3.zero;

        switch (edgeIndex)
        {
            case 0: spawnDirection = -gameCamera.transform.up; break;
            case 1: spawnDirection = gameCamera.transform.up; break;
            case 2: spawnDirection = -gameCamera.transform.right; break;
            case 3: spawnDirection = gameCamera.transform.right; break;
        }

        Vector3 spawnPosition = worldPointAtEdge + spawnDirection * spawnOffset;
        if (gameCamera.orthographic)
        {
            spawnPosition.z = entityPrefab.transform.position.z; // Maintain original Z for 2D
        }
        Instantiate(entityPrefab, spawnPosition, Quaternion.identity);
    }

    // This function should be called when the objective of the current step is met
    public void EndCurrentStep()
    {
        if (currentState != TutorialState.StepInProgress && currentState != TutorialState.SpawningEntities) // Allow ending even if spawning if objectives met early
        {
            // If already transitioning or completed, do nothing.
            // This check prevents issues if EndCurrentStep is called multiple times.
            if (currentState == TutorialState.StepCompleteTransition || currentState == TutorialState.AllStepsComplete)
                return;

            Debug.LogWarning($"EndCurrentStep called while not in progress. Current state: {currentState}");
            // return; // Decide if you want to allow ending from other states.
            // For some tutorials, a step might be "complete" as soon as instructions are read.
        }


        WaveConfig currentStep = tutorialSteps[currentStepIndex];
        Debug.Log($"Tutorial Step {currentStep.waveNumber} completed!");
        currentState = TutorialState.StepCompleteTransition;
        PlayMusic(defaultTutorialMusic); // Or a specific "step complete" sound/music

        if (currentStepIndex >= tutorialSteps.Count - 1)
        {
            AllTutorialStepsCompleted();
        }
        else
        {
            Debug.Log($"Starting transition for {timeBetweenSteps} seconds before next step's instructions.");
            StartCoroutine(StepTransitionCoroutine());
        }
    }

    IEnumerator StepTransitionCoroutine()
    {
        yield return new WaitForSeconds(timeBetweenSteps);
        StartNextStepSequence(); // This will then show instructions for the next step
    }

    void AllTutorialStepsCompleted()
    {
        currentState = TutorialState.AllStepsComplete;
        Debug.Log("All tutorial steps completed! Congratulations!");
        PlayMusic(defaultTutorialMusic); // Or a "tutorial complete" celebratory music
        // Optionally, trigger a game event or UI indication
        if (instructionCardUI && instructionTextDisplay)
        {
            instructionCardUI.SetActive(true);
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            typingCoroutine = StartCoroutine(TypeWriterEffect("Tutorial Complete!", instructionTextDisplay, 30f));
            // Maybe disable the continue button or leave it for the player to close
        }

        // Example: Notify GameManager
        // if (GameManager.Instance != null)
        // {
        //     GameManager.Instance.OnTutorialCompleted();
        // }
    }

    void PlayMusic(AudioClip clip)
    {
        if (musicAudioSource && clip)
        {
            if (musicAudioSource.isPlaying && musicAudioSource.clip == clip) return; // Already playing this clip
            musicAudioSource.Stop();
            musicAudioSource.clip = clip;
            musicAudioSource.Play();
        }
        else if (musicAudioSource && clip == null) // Stop music if clip is null
        {
            musicAudioSource.Stop();
            musicAudioSource.clip = null;
        }
    }

    // --- Example Objective Tracking for Goat Herding ---
    // These methods would be called by your Goat scripts or GameManager
    public void NotifyPeacefulGoatHerded() // Example: Call this when a peaceful goat reaches a goal
    {
        if (currentState == TutorialState.StepInProgress)
        {
            goatsHerdedThisStep++;
            WaveConfig currentStep = tutorialSteps[currentStepIndex];
            Debug.Log($"Tutorial: Peaceful goat herded. Herded this step: {goatsHerdedThisStep}/{peacefulGoatsToSpawnThisStep}");

            // Check if the objective for the current step is met
            if (peacefulGoatsToSpawnThisStep > 0 && goatsHerdedThisStep >= peacefulGoatsToSpawnThisStep)
            {
                EndCurrentStep();
            }
        }
    }

    public void NotifyHerdedGoatConverted() // Example: If a herded goat gets "un-herded"
    {
         if (currentState == TutorialState.StepInProgress)
         {
            goatsHerdedThisStep--;
            // Ensure it doesn't go below zero, though logically it might if conversion happens before any are herded.
            goatsHerdedThisStep = Mathf.Max(0, goatsHerdedThisStep);
            WaveConfig currentStep = tutorialSteps[currentStepIndex];
            Debug.Log($"Tutorial: Herded goat was converted. Herded this step: {goatsHerdedThisStep}/{peacefulGoatsToSpawnThisStep}");
            // No EndCurrentStep check here, as losing a goat means the objective is further away
         }
    }
    // --- End Example Objective Tracking ---

    IEnumerator TypeWriterEffect(string textToType, TextMeshProUGUI targetText, float charactersPerSecond)
    {
        targetText.text = "";
        if (charactersPerSecond <= 0) charactersPerSecond = 30f; // Default speed
        float delay = 1f / charactersPerSecond;
        foreach (char letter in textToType.ToCharArray())
        {
            targetText.text += letter;
            yield return new WaitForSeconds(delay);
        }
        typingCoroutine = null;
    }

    // Optional: If you want to allow skipping the typewriter effect for instructions
    public void SkipTyping()
    {
        if (typingCoroutine != null && currentState == TutorialState.ShowingInstructions)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
            WaveConfig currentStep = tutorialSteps[currentStepIndex];
            instructionTextDisplay.text = currentStep.endOfWaveDialogue; // Show full text immediately
        }
    }
}