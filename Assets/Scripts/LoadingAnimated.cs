using DG.Tweening;
using UnityEngine;

public class LoadingAnimated : MonoBehaviour
{
    [SerializeField] private RectTransform imageTransform;
    public float stepAngle = 45f;
    public float stepInterval = 0.2f;

    private Tween rotateTween;

    void OnEnable()
    {
        if (imageTransform == null)
        {
            Debug.LogError("Image Transform is not assigned!");
            return;
        }

        rotateTween = DOVirtual.DelayedCall(stepInterval, RotateStep, false)
            .SetLoops(-1, LoopType.Restart);
    }

    void OnDisable()
    {
        if (rotateTween != null && rotateTween.IsActive())
        {
            rotateTween.Kill();
        }
        rotateTween = null;
    }

    private void RotateStep()
    {
        if (imageTransform != null)
        {
            float newZ = imageTransform.eulerAngles.z + stepAngle;
            imageTransform.rotation = Quaternion.Euler(0, 0, newZ);
        }
    }
}