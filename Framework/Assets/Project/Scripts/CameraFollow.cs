using UnityEngine;

/*
    This camera smoothes out rotation around the y-axis and height.
    Horizontal Distance to the target is always fixed.


    There are many different ways to smooth the rotation but doing it this way gives you a lot of control over how the camera behaves.


    For every of those smoothed values we calculate the wanted value and the current value.
    Then we smooth it using the Lerp function.
    Then we apply the smoothed values to the transform's position.
*/

// Place the script in the Camera-Control group in the component menu
[AddComponentMenu("Camera-Control/Smooth Follow")]
public class CameraFollow : MonoBehaviour
{
    // The target we are following
    public Transform target;

    // The distance in the x-z plane to the target
    [SerializeField]
    float distance = 18.0f;
    [SerializeField]
    float minDistance = 10.0f;
    [SerializeField]
    float maxDistance = 20.0f;

    float addDistance = 0.0f;
    [SerializeField]
    float distanceDamping = 0.1f;
    bool _isZoomControl = false;
    public bool isZoomControl { get { return _isZoomControl; } }

    float zoomRatio = 0.0f;

    [SerializeField]
    Vector3 rotate = new Vector3(41f, -32f, 0f);

    void LateUpdate()
    {
        // Early out if we don't have a target
        if (!target)
        {
            return;
        }

        float d = distance;

        if (zoomRatio > 0.0f)
        {
            addDistance = Mathf.Lerp(0.0f, maxDistance - distance, zoomRatio);
            d += addDistance;

            if (!isZoomControl)
            {
                zoomRatio -= distanceDamping;
                zoomRatio = Mathf.Clamp(zoomRatio, 0.0f, 1.0f);
            }
        }
        else if (zoomRatio < 0.0f)
        {
            addDistance = Mathf.Lerp(maxDistance - distance, 0.0f, zoomRatio * -1f);
            d += addDistance;

            if (!isZoomControl)
            {
                zoomRatio += distanceDamping;
                zoomRatio = Mathf.Clamp(zoomRatio, 0.0f, 1.0f);
            }
        }

        Quaternion currentRotation = Quaternion.Euler(rotate);

        /* calculate "camera to target" direction */
        Matrix4x4 mat = Matrix4x4.identity;
        mat.SetTRS(Vector3.zero, currentRotation, Vector3.one);
        Vector3 dir = Vector3.back;
        dir = mat.MultiplyVector(dir);
        dir.Normalize();

        Vector3 targetPosition = new Vector3(target.position.x, target.position.y, target.position.z);
        transform.SetPositionAndRotation(targetPosition + dir * d, currentRotation);
        transform.LookAt(target);

        _isZoomControl = false;

        UpdateShakeCtrl(target);
    }

    public void ZoomOutWithDash()
    {
        zoomRatio += distanceDamping;
        zoomRatio = Mathf.Clamp(zoomRatio, 0.0f, 1.0f);

        addDistance = Mathf.Lerp(0.0f, maxDistance - distance, zoomRatio);
        _isZoomControl = true;
    }

    /// <summary>
    /// カメラの振動管理構造体
    /// </summary>
    struct ShakeCtrl
    {
        public bool isActive;
        public float sec;
        public float power;
        public bool isResume;
    }
    ShakeCtrl shakeCtrl;

    /// <summary>
    /// カメラを振動させます。
    /// </summary>
    /// <param name="sec">振動させる時間（秒）</param>
    /// <param name="power">振動幅</param>
    /// <param name="resume">元の位置に戻すか</param>
    public void SetShake(float sec = 0.2f, float power = 0.08f, bool resume = false)
    {
        shakeCtrl.isActive = true;
        shakeCtrl.sec = sec;
        shakeCtrl.power = power;
        shakeCtrl.isResume = resume;
    }

    /// <summary>
    /// カメラの振動実行処理
    /// </summary>
    /// <param name="target"></param>
    void UpdateShakeCtrl(Transform target)
    {
        if (shakeCtrl.isActive)
        {
            shakeCtrl.sec -= Time.deltaTime;
            if (shakeCtrl.sec <= 0.0f)
            {
                shakeCtrl.isActive = false;
                if (shakeCtrl.isResume)
                {
                    transform.LookAt(target);
                }
            }
            else
            {
                Vector3 add = Vector3.one;
                add.x *= Random.Range(-shakeCtrl.power, shakeCtrl.power);
                add.y *= Random.Range(-shakeCtrl.power, shakeCtrl.power);
                add.z *= Random.Range(-shakeCtrl.power, shakeCtrl.power);
                transform.LookAt(target.position + add);
            }
        }
    }

    public void AddDistance(float value)
    {
        distance += value;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }
    public float GetDistance()
    {
        return distance;
    }
}
