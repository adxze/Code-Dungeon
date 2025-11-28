using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [Header("Player Settings")]
    public string playerName = "Hero";
    
    [Header("Player Direction")][Tooltip("Default direction is UP")]
    public GridMover.Direction playerDirection = GridMover.Direction.Up;
    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("Player Manager initialized");
    }

    public GridMover.Direction GetDirection()
    {
        return playerDirection;
    }

}