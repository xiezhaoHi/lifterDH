using UnityEngine;
using System.Collections;

public class move_controll : MonoBehaviour
{

    float speed = 5f;　　//移动速度
    public GameObject camer; //Capsule下面那个摄像机
    private CharacterController characterController;
    // Use this for initialization
    void Start()
    {
        characterController = this.GetComponent<CharacterController>();


    }

    // Update is called once per frame
    void Update()
    {
        Vector3 forward = camer.transform.TransformDirection(Vector3.forward);//前后移动
        float curSpeed = speed * Input.GetAxis("Vertical");
        characterController.SimpleMove(forward * curSpeed);
        Vector3 v = camer.transform.TransformDirection(Vector3.right);//左右移动
        float vSpeed = speed * Input.GetAxis("Horizontal");
        characterController.SimpleMove(v * vSpeed);

    }
}