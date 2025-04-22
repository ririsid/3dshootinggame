using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;

    private void Update()
    {
        // interpolation, smoothing 기법이 들어갈 예정
        transform.position = Target.position;
    }
}
