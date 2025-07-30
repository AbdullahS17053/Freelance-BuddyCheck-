using DG.Tweening;
using UnityEngine;

public class LoadingAnimated : MonoBehaviour
{
    public RectTransform imageTransform;
    public float stepAngle = 45f;
    public float stepInterval = 0.2f;

    private Tween rotateTween;

    void OnEnable()
    {
        rotateTween = DOVirtual.DelayedCall(stepInterval, RotateStep, false)
            .SetLoops(-1, LoopType.Restart);
    }

    void OnDisable()
    {
        if (rotateTween != null && rotateTween.IsActive())
        {
            rotateTween.Kill();
        }
    }

    private void RotateStep()
    {
        float newZ = imageTransform.eulerAngles.z + stepAngle;
        imageTransform.rotation = Quaternion.Euler(0, 0, newZ);
    }
}
