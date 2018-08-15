using UnityEngine;
using System.Collections;

public class lookat : MonoBehaviour
{
    //视野转动速度
    float speedX = 10f;
    float speedY = 10f;
    //上下观察范围
    float minY = -60;
    float maxY = 60;
    //观察变化量
    float rotationX;
    float rotationY;
    // Use this for initialization
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        rotationX +=  Input.GetAxis("Mouse X") * speedX;
        rotationY += -1* Input.GetAxis("Mouse Y") * speedY;
        if (rotationX < 0)
        {
            rotationX += 360;
        }
        if (rotationX > 360)
        {
            rotationX -= 360;
        }
        rotationY = Mathf.Clamp(rotationY, minY, maxY);
        transform.localEulerAngles = new Vector3(-rotationY, rotationX, 0);
    }
}