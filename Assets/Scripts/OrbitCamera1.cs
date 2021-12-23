using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera1 : MonoBehaviour
{
    //焦点
    [SerializeField]
    Transform focus = default;
    //相机距离焦点的距离
    [SerializeField, Range(1f, 20f)]
    float distance =  5f;
    //焦点半径 来控制焦点移动多大后相机才会动
    [SerializeField, Min(0f)]
    float focusRadius = 1f;
    //让相机一直移动，直到焦点回到视图的中心
    [SerializeField, Range(0f, 1f)]
    float focusCentering = 0.5f;
    //转速配置选项，以每秒度数表示，用来手动控制轨道
    [SerializeField, Range(1f, 360f)]
    float rotationSpeed = 90f;
    //限制相机垂直旋转角度 防止相机翻转
    [SerializeField, Range(-89f, 89f)]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;
    //可配置的对齐延迟，默认设置为5秒。如果你根本不想要自动对准，那么你可以简单地设置一个非常高的延迟
    [SerializeField]
    float alignDelay = 5;
    //使相机对齐旋转速度与当前角度和所需角度之差成比例，通过对齐平滑范围配置选项（0-90范围，默认值为45°）来配置此角度
    [SerializeField, Range(0f, 90f)]
    float alignSmoothRange = 45f;


    Vector3 focusPoint, previousFocusPoint;
    //X角定义了它的垂直方向，0垂直于地平线，90垂直于地平线。Y角定义了水平方向，0沿着世界Z轴。在Vector2字段中跟踪这些角度，默认设置为45和0。
    Vector2 orbitAngles = new Vector2(45f, 0f);
    //上一次计算镜头旋转的时间
    float lastManualRotationTime;

    private void OnValidate()
    {
        if(maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    private void Awake()
    {
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        UpdateFocusPoint();
        //构造一个四元数来定义相机的外观旋转，并向其传递轨道角度
        Quaternion lookRotation;
        //镜头输入角度发生变化时或者对齐延迟时间到了
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);//将向量隐式转换为Vector3，并且Z旋转设置为零。
        }
        else
        {
            lookRotation = transform.localRotation;
        }
        
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    //确定镜头焦点
    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
        if(focusRadius > 0)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t= 1f;
            if(distance > 0.01f  && focusCentering > 0)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
                //focusPoint = Vector3.Lerp(targetPoint, focusPoint, focusRadius / distance);
            }
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }

    //此方法用来检索输入向量，确定镜头旋转角度
    bool ManualRotation()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Vertical Camera"),
            Input.GetAxis("Horizontal Camera"));
        const float e = 0.001f;
        if(input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    bool AutomaticRotation()
    {
        if(Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }

        //计算当前帧的运动矢量，运动向量的平方大小小于一个较小的阈值（如0.0001），那么运动不多的情况下就不旋转了
        //movement为物体的运动矢量（移动的方向向量）
        Vector2 movement = new Vector2(focusPoint.x - previousFocusPoint.x,
                                       focusPoint.z - previousFocusPoint.z);
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.000001f)
        {
            return false;
        }
        //计算出相机要在物体身后，需要旋转的角度
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        //通过将当前角度和所需角度传递给Mathf.DeltaAngle并取其绝对值来找到AutomaticRotation中的角度增量
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        //将旋转速度缩放为时间增量和平方运动增量中的最小值来进一步抑制微小角度的旋转（看似有bug）
        //float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        float rotationChange = rotationSpeed * Time.unscaledDeltaTime;
        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if(180f - deltaAbs < alignSmoothRange) //当焦点移向相机时，防止摄像机全速旋转，每次航向越过180°边界时都会改变方向
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        //orbitAngles.y = headingAngle;
        //平滑对齐
        orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    //限制镜头的旋转角度
    void ConstrainAngles()
    {
        //垂直轨道角度钳位到配置的范围
        orbitAngles.x = Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);
        //水平轨道没有限制，但请确保角度保持在0–360范围内
        if(orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        }
        else if(orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

    //将2D方向转换为角度，方向的Y分量是我们所需角度的余弦，因此将其通过Mathf.Acos放置，然后从弧度转换为度
    static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        //如果X为负，则它为逆时针方向，我们需要从360°中减去该角度
        return direction.x < 0 ? 360f - angle : angle;
    }
}
