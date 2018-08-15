using UnityEngine;
using System.Collections;
///<summary>
/// 通过鼠标左键可以使摄像机绕物体旋转
/// 鼠标右键可以是摄像机自身旋转
/// 鼠标滚轮可以对摄像机进行缩放
///</summary>
public class ControlByMouse : MonoBehaviour {

    //键盘移动相机 速度
    public float sensitivetyKeyBoard = 0.5f;

    /// <summary>
    /// 与y轴最大夹角
    /// </summary>
    public float yMaxAngle = 170;

    /// <summary>
    /// 与y轴最小夹角
    /// </summary>
    public float yMinAngle = 90;

    /// <summary>
    /// 与y轴最小夹角
    /// </summary>
    public float yAxisMinAngle = 60;

    /// <summary>
    /// 与y轴最大夹角
    /// </summary>
    public float yAxisMaxAngle = 330;
    //自身旋转的速度
    public float rotateSpeed = 10;
    //缩放速度
    public float fieldOfViewSpeed = 10;
    //绕物体旋转的速度
    public float rotateLookForwardSpeed = 10;
    //声明水平、垂直位移
    float horizontal, vertical;
    //声明摄像机旋转的对象
    public Transform targetTF;
    //声明摄像机
    private Camera camera;

    //左右上下移动 的范围
    private float moveXBegin = -400f;
    private float moveXEnd = 400f;

    private float moveYBegin = 0f;
    private float moveYEnd = 500f;
   
    void Start()
    {
        //初始化摄像机
        camera = this.transform.GetComponent<Camera>();
       // LookAtTarget();
        
    }
    void Update()
    {
        KeyBControl();
        ChangeFieldOfView();
        RotateAndLookForward();
    }

    //键盘控制 相机 上下 左右位置
    void KeyBControl()
    {
        return;
        //键盘按钮←/a和→/d实现视角水平移动，
        if (Input.GetAxis("Horizontal") == -1) 
        {
            if (transform.position.x< moveXEnd)
            {
                transform.Translate(Input.GetAxis("Horizontal") * sensitivetyKeyBoard, 0, 0);
            }
        }
        if (Input.GetAxis("Horizontal") == 1)
        {
            if (transform.position.x > moveXBegin)
            {
                transform.Translate(Input.GetAxis("Horizontal") * sensitivetyKeyBoard, 0, 0);
            }
        }
        //键盘按钮↑/w和↓/s实现视角水平旋转  
        if (Input.GetAxis("Vertical") == -1)
        {
            
            float resF = Input.GetAxis("Vertical") * sensitivetyKeyBoard;
            //             Vector3 length = targetTF.GetComponent<MeshFilter>().mesh.bounds.size;
            //             float xlength = length.x * targetTF.lossyScale.x;
            //             float ylength = length.y * targetTF.lossyScale.y;s
            if (transform.position.y > moveYBegin )
                transform.Translate(0, resF, 0);
        }
        if (Input.GetAxis("Vertical") == 1)
        {

            float resF = Input.GetAxis("Vertical") * sensitivetyKeyBoard;
            //             Vector3 length = targetTF.GetComponent<MeshFilter>().mesh.bounds.size;
            //             float xlength = length.x * targetTF.lossyScale.x;
            //             float ylength = length.y * targetTF.lossyScale.y;s
            if ( transform.position.y < moveYEnd)
                transform.Translate(0, resF, 0);
        }
    }


    //获取水平以及垂直位移
    private void GetMouseXY()
    {
        horizontal = Input.GetAxis("Mouse X");
        vertical = Input.GetAxis("Mouse Y");
    }
    //缩放方法
    private void ChangeFieldOfView()
    {
        //滚轮实现镜头缩进和拉远  
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            if (Input.GetAxis("Mouse ScrollWheel") < 0)
            {
                //Debug.Log(this.GetComponent<Camera>().fieldOfView);
               // Debug.Log(this.GetComponent<Camera>().orthographicSize);
                if (this.GetComponent<Camera>().fieldOfView <= 70)
                    this.GetComponent<Camera>().fieldOfView++;
                // 摄像机的正交投影
                if (this.GetComponent<Camera>().orthographicSize <= 20)
                    this.GetComponent<Camera>().orthographicSize += 0.5f;
            }
            if (Input.GetAxis("Mouse ScrollWheel") > 0)
            {
                if (this.GetComponent<Camera>().fieldOfView > 1)
                    this.GetComponent<Camera>().fieldOfView--;
                if (this.GetComponent<Camera>().orthographicSize >= 1)
                    this.GetComponent<Camera>().orthographicSize -= 0.5f;
            }
        }
    }
    //按下鼠标左键，摄像机围绕目标旋转
    private void RotateView(float x, float y)
    {
        x *= rotateSpeed;
        y *= rotateSpeed;

        //大于330     小于60
        if (this.transform.eulerAngles.x - y > yAxisMaxAngle || this.transform.eulerAngles.x - y < yAxisMinAngle)
        {
            this.transform.Rotate(0, x, 0, Space.World);
            this.transform.Rotate(-y, 0, 0);
        }
    }
    //按下鼠标右键，自身旋转方法
    private void RotateByMouse()
    {
        if (Input.GetMouseButton(1))
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            if (Input.GetMouseButton(1) && (x != 0 || y != 0))
            {
                RotateView(x, y);
            }
        }
    }
    private void LookAtTarget()
    {
        this.transform.LookAt(targetTF);
    }
    private void LimitAxisY(ref float y)
    {
        y *= rotateSpeed;
        //计算当前摄像机Z轴与世界Y轴夹角
        float angle = Vector3.Angle(this.transform.forward, Vector3.up);
        //print(angle);
        if (angle < yMinAngle && y > 0 || angle > yMaxAngle && y < 0)
            y = 0;
    }
    private void RotateAround(float x, float y)
    {
        this.transform.RotateAround
           (targetTF.position, Vector3.up, x * rotateSpeed);

        LimitAxisY(ref y);

        this.transform.RotateAround
            (targetTF.position, this.transform.right, -y);
    }
    //
    private void RotateAndLookForward()
    {
        if (Input.GetMouseButton(0))
        {
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            if (Input.GetMouseButton(0) && (x != 0 || y != 0))
                RotateAround(x, y);
        }
    }

}
