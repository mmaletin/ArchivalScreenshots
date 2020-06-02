
using UnityEngine;

[CreateAssetMenu(fileName = "Archival Screenshots Settings", menuName = "Archival Screenshots/Settings")]
internal class ArchivalScreenshotsSettings : ScriptableObject
{
    public bool enabled = true;
    public float delaySeconds = 15;
    public Vector2Int resolution = new Vector2Int(1920, 1080);
    public int supersampling = 1;
}