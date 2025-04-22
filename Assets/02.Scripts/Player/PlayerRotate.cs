using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    public float RotationSpeed = 100f; // 카메라와 회전 속도가 같아야 한다.

    private float _rotationX = 0f;

    private void Update()
    {
        // 1. 마우스 입력을 받는다.(마우스 커서의 움직임 방향)
        float mouseX = Input.GetAxis("Mouse X");

        // 2. 회전한 양만큼 누적시켜 나간다.
        _rotationX += mouseX * RotationSpeed * Time.deltaTime;

        // 3. 회전 방향으로 회전시킨다.
        transform.eulerAngles = new Vector3(0f, _rotationX, 0f);
    }
}
