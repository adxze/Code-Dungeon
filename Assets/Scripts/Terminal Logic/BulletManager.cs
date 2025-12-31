using System.Collections;
using UnityEngine;

// Temporary Firing code, this thing does not regard a grid movement, has physic ect
public class BulletManager : MonoBehaviour
{
    [Tooltip("Prefab instantiated when Fire() is invoked.")]
    public GameObject bulletPrefab;
    [Tooltip("Speed at which bullets travel (units per second).")]
    public float bulletSpeed = 5f;
    [Tooltip("Optional transform used as the spawn location.  If null the player's position should be passed when calling Fire().")]
    public Transform fireOrigin;

    public IEnumerator Fire(Vector3? origin = null, CodeGameController controller = null)
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            controller?.AddFeedback("Fire() ignored: game finished.");
            yield break;
        }

        if (bulletPrefab == null)
        {
            controller?.AddFeedback("Fire() ignored: no bullet prefab assigned.");
            yield break;
        }
        Vector3 spawnPos = origin ?? fireOrigin?.position ?? Vector3.zero;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        Rigidbody2D rb2d = bullet.GetComponent<Rigidbody2D>();
        if (rb2d != null)
        {
            if(PlayerManager.Instance.GetDirection() == (GridMover.Direction.Up))
                rb2d.linearVelocity = Vector2.up * bulletSpeed;
            else if(PlayerManager.Instance.GetDirection() == (GridMover.Direction.Left))
                rb2d.linearVelocity = Vector2.left * bulletSpeed;
            else if(PlayerManager.Instance.GetDirection() == (GridMover.Direction.Right))
                rb2d.linearVelocity = Vector2.right * bulletSpeed;
            else if(PlayerManager.Instance.GetDirection() == (GridMover.Direction.Down))
                rb2d.linearVelocity = Vector2.down * bulletSpeed;
        }
  
        yield break;
    }
}
