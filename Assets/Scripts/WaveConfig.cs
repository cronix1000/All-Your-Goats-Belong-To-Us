// Create a new C# script called WaveConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "WaveConfig", menuName = "GoatGame/Wave Configuration", order = 0)]
public class WaveConfig : ScriptableObject
{
    public int waveNumber;
    public int peacefulGoatsToSpawn;
    public int enemyGoatsToSpawn;
    [TextArea(3,5)]
    public string endOfWaveDialogue;
    public AudioClip waveMusic; // Music for during this wave
}