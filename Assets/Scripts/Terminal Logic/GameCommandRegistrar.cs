using System.Collections;
using UnityEngine;

/// <summary>
/// Example of registering commands with the <see cref="CodeGameController"/>.
/// This component holds references to gameplay services such as a
/// <see cref="GridMover"/> and <see cref="BulletManager"/> and converts
/// interpreter arguments into those services’ APIs.  By isolating command
/// implementations here you avoid hard‑coding any behaviour in the
/// interpreter itself.  Add your own commands by registering additional
/// coroutines that delegate to other systems (audio, animation, etc.).
/// </summary>
public class GameCommandRegistrar : MonoBehaviour
{
    [Tooltip("Interpreter that will call the registered commands.")]
    public CodeGameController controller;
    [Tooltip("Component responsible for moving the player on the grid.")]
    public GridMover gridMover;
    [Tooltip("Component responsible for firing bullets.")]
    public BulletManager bulletManager;
    [Tooltip("Transform of the player.  Used by Fire() if BulletManager has no origin.")]
    public Transform player;

    private void Awake()
    {
        if (controller == null) return;
        // Register Move.  It parses the first argument into a Direction and
        // delegates to GridMover.  Because parsing occurs here, the
        // interpreter remains agnostic of movement details.
        controller.RegisterCommand("Move", MoveCommand);
        // Register Fire.  It spawns a bullet via BulletManager.  You can
        // register additional commands following the same pattern.
        controller.RegisterCommand("Fire", FireCommand);
    }

    /// <summary>
    /// Handles the Move command.  Expects one argument corresponding to a
    /// <see cref="GridMover.Direction"/> name (e.g. "Up", "Left").  The
    /// parsing is case‑sensitive; unknown directions produce a feedback
    /// message.  The coroutine delegates to <see cref="GridMover.Move"/>.
    /// </summary>
    private IEnumerator MoveCommand(string[] args)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            controller?.AddFeedback("Move() ignored: game finished.");
            yield break;
        }

        if (gridMover == null)
        {
            controller.AddFeedback("Move(): no GridMover assigned.");
            yield break;
        }
        if (args.Length < 1)
        {
            controller.AddFeedback("Move() requires a direction argument.");
            yield break;
        }
        // Attempt to parse the argument into the Direction enum
        if (!System.Enum.TryParse<GridMover.Direction>(args[0], out var dir))
        {
            controller.AddFeedback($"Unknown direction '{args[0]}' for Move().");
            yield break;
        }
        // Delegate to grid mover; pass controller for feedback on invalid cells
        IEnumerator co = gridMover.Move(dir, controller);
        while (co != null && co.MoveNext())
        {
            yield return co.Current;
        }
    }

    /// <summary>
    /// Handles the Fire command.  Accepts no arguments.  Delegates to
    /// <see cref="BulletManager.Fire"/>.  If no BulletManager is assigned
    /// the command reports a feedback message.
    /// </summary>
    private IEnumerator FireCommand(string[] args)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            controller?.AddFeedback("Fire() ignored: game finished.");
            yield break;
        }

        if (bulletManager == null)
        {
            controller.AddFeedback("Fire(): no BulletManager assigned.");
            yield break;
        }
        // Use player's position as origin if BulletManager has no origin
        Vector3? origin = player != null ? player.position : (Vector3?)null;
        IEnumerator co = bulletManager.Fire(origin, controller);
        while (co != null && co.MoveNext())
        {
            yield return co.Current;
        }
    }
}
