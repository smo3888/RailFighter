using UnityEngine;

public class RailStomperSpawn : MonoBehaviour
{
    // Tracks if this rail currently has a stomper
    public bool IsOccupied { get; private set; }

    public void MarkOccupied()
    {
        IsOccupied = true;
    }

    public void MarkAvailable()
    {
        IsOccupied = false;
    }
}