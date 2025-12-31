using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Handles movement for a character on a tilemap grid.  It exposes a
/// coroutine that moves the attached player transform one cell in a
/// specified direction and validates that the destination cell exists.  The
/// actual interpretation of direction names lives in the <see cref="Direction"/>
/// enum and a lookup table.  Nothing in this script cares about the
/// interpreter or how commands are parsed; it simply offers a movement API.
/// </summary>
public class GridMover : MonoBehaviour
{
    /// <summary>Enum representing valid movement directions.  Add more
    /// directions here </summary>
    public enum Direction { Up, Down, Left, Right }

    [Header("References")]
    [Tooltip("The tilemap used for collision detection and grid positioning.")]
    public Tilemap tilemap;
    [Tooltip("Transform of the character to move.")]
    public Transform player;

    [Header("Movement settings")]
    [Tooltip("Duration of movement between adjacent tiles in seconds.")]
    public float moveDuration = 0.2f;

    /// <summary>
    /// Mapping from Direction values to tile offsets.  You can extend this
    /// dictionary to support diagonal or custom directions without touching
    /// the command registration logic.  Note: directions must remain
    /// consistent with the Direction enum.
    /// </summary>
    private static readonly Dictionary<Direction, Vector3Int> DirectionOffsets = new()
    {
        { Direction.Up, new Vector3Int(0, 1, 0) },
        { Direction.Down, new Vector3Int(0, -1, 0) },
        { Direction.Left, new Vector3Int(-1, 0, 0) },
        { Direction.Right, new Vector3Int(1, 0, 0) }
    };

    /// <summary>
    /// Moves the player one cell in the given direction if a tile exists at
    /// the destination.  If no tile exists the movement is cancelled and a
    /// message is sent to the supplied controller.  This method can be used
    /// by any command registration script; it does not parse strings or
    /// interact with user input.
    /// </summary>
    /// <param name="direction">Direction to move.</param>
    /// <param name="controller">Interpreter that collects feedback.  Pass
    /// null if you don’t want feedback messages.</param>
    public IEnumerator Move(Direction direction, CodeGameController controller = null)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            controller?.AddFeedback("Movement blocked: game finished.");
            yield break;
        }

        if (tilemap == null || player == null)
        {
            controller?.AddFeedback("GridMover: missing tilemap or player reference.");
            yield break;
        }
        if (!DirectionOffsets.TryGetValue(direction, out var delta))
        {
            controller?.AddFeedback($"GridMover: no offset defined for {direction}.");
            yield break;
        }
        Vector3Int currentCell = tilemap.WorldToCell(player.position);
        Vector3Int targetCell = currentCell + delta;
        if (!tilemap.HasTile(targetCell))
        {
            controller?.AddFeedback($"Cannot move to {targetCell}: no tile exists.");
            yield break;
        }
        Vector3 startPos = player.position;
        Vector3 targetPos = tilemap.GetCellCenterWorld(targetCell);
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            player.position = Vector3.Lerp(startPos, targetPos, elapsed / moveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        player.position = targetPos;

        // Update player direction in PlayerManager
        if(PlayerManager.Instance != null)
        {
            PlayerManager.Instance.playerDirection = direction;  
            Debug.Log($"Player direction updated to: {direction}"); 
        }
        // Movement complete
    }
}
