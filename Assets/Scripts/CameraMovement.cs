using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float speed = 1f;
    public Vector3 target = new Vector3(0, 0, -10);

    void Update()
    {
        if (transform.parent != null)
        {
            // Smoothly move localPosition towards Vector3.zero (0,0,0 under parent)
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                target,
                speed * Time.deltaTime
            );
        }
    }
}