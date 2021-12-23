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


    Vector3 focusPoint;
    //X角定义了它的垂直方向，0垂直于地平线，90垂直于地平线。Y角定义了水平方向，0沿着世界Z轴。在Vector2字段中跟踪这些角度，默认设置为45和0。
    Vector2 orbitAngles = new Vector2(45f, 0f);

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
        //镜头输入角度发生变化时
        if (ManualRotation())
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
            return true;
        }
        return false;
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
}
