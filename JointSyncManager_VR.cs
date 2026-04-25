using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.ServoController;

public class JointSyncManager : MonoBehaviour
{
    ROSConnection ros;
    public string pubTopic = "/servo_controller";
    public string subTopic = "/joint_states";

    [Header("UI & Logic")]
    public Slider[] jointSliders;
    public string[] jointNames = { "joint1", "joint2", "joint3", "joint4", "joint5" };
    public int[] servoIDs = { 1, 2, 3, 4, 5 };
    public bool[] isInverted;

    [Header("3D Model")]
    public ArticulationBody[] robotJoints;

    [Header("XR Control")]
    private InputDevice rightHandDevice;
    public float joystickSpeed = 300f; // ปรับความเร็วได้ที่นี่
    public float publishRate = 0.05f;  // ส่งคำสั่ง 20 ครั้งต่อวินาทีให้หุ่นขยับเนียนๆ

    // 🔥 ตัวแปรเก็บค่าเป้าหมายภายใน (แก้ปัญหา Slider ปัดเศษทศนิยม)
    private float[] targetPulses = new float[5] { 500f, 500f, 150f, 500f, 500f };
    private float nextPublishTime = 0f;
    private bool isUpdatingFromROS = false;
    private bool wasAPressed = false;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ServosPositionMsg>(pubTopic);
        ros.Subscribe<JointStateMsg>(subTopic, OnJointStatesReceived);

        // ดึงค่าเริ่มต้นให้ตรงกับ Slider 
        for (int i = 0; i < jointSliders.Length; i++)
        {
            int index = i;
            if (index < targetPulses.Length) targetPulses[index] = jointSliders[index].value;

            // ฟังคำสั่งกรณีใช้เมาส์ลาก Slider ในจอ
            jointSliders[i].onValueChanged.AddListener((val) => {
                if (!isUpdatingFromROS)
                {
                    targetPulses[index] = val;
                    SendServoCommand(index, (int)val);
                }
            });
        }

        // เชื่อมต่อจอยขวาตอนเริ่ม
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) rightHandDevice = devices[0];
    }

    void Update()
    {
        // Reconnect ถ้าจอยหลุด
        if (!rightHandDevice.isValid)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
            if (devices.Count > 0) rightHandDevice = devices[0];
        }

        bool hasVRInput = false;

        // ==========================================
        // 1. ปุ่ม A: รีเซ็ตแขนกลับท่ามาตรฐาน (Home)
        // ==========================================
        if (rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool isAPressed))
        {
            if (isAPressed && !wasAPressed)
            {
                targetPulses[0] = 500f;
                targetPulses[1] = 500f;
                targetPulses[2] = 150f;
                targetPulses[3] = 500f;
                targetPulses[4] = 500f;

                hasVRInput = true;
                wasAPressed = true;
                Debug.Log("[XR] Button A Pressed -> Resetting Arm to Home");
            }
            else if (!isAPressed)
            {
                wasAPressed = false;
            }
        }

        // ==========================================
        // 2. Joystick X-Axis: หมุนซ้าย-ขวา เฉพาะ Joint 1
        // ==========================================
        if (rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystickInput))
        {
            float h = -joystickInput.x;
            if (Mathf.Abs(h) > 0.1f) // Deadzone กันจอยดริฟท์
            {
                targetPulses[0] += h * joystickSpeed * Time.deltaTime;
                targetPulses[0] = Mathf.Clamp(targetPulses[0], 250f, 750f); // ล็อกระยะ 250-750
                hasVRInput = true;
            }
        }

        // ==========================================
        // 3. อัปเดต UI และยิงคำสั่งไปหา ROS
        // ==========================================
        if (hasVRInput)
        {
            // ขยับ Slider ตามค่าที่คำนวณได้โดยไม่ต้องกระตุ้น onValueChanged
            isUpdatingFromROS = true;
            for (int i = 0; i < jointSliders.Length; i++)
            {
                if (i < targetPulses.Length) jointSliders[i].value = targetPulses[i];
            }
            isUpdatingFromROS = false;

            // ยิงคำสั่งไปหาหุ่น (Rate Limit กัน Network ค้าง)
            if (Time.time > nextPublishTime)
            {
                SendAllServosCommand();
                nextPublishTime = Time.time + publishRate;
            }
        }
    }

    void OnJointStatesReceived(JointStateMsg msg)
    {
        isUpdatingFromROS = true;
        for (int i = 0; i < msg.name.Length; i++)
        {
            for (int j = 0; j < jointNames.Length; j++)
            {
                if (msg.name[i] == jointNames[j])
                {
                    // 1. ซิงก์หุ่น 3D ในจอ
                    if (j < robotJoints.Length && robotJoints[j] != null)
                    {
                        float degrees = (float)(msg.position[i] * Mathf.Rad2Deg);
                        if (j < isInverted.Length && isInverted[j]) degrees *= -1;

                        var drive = robotJoints[j].xDrive;
                        drive.target = degrees;
                        robotJoints[j].xDrive = drive;
                    }

                    // 2. ซิงก์ค่ากลับมาที่ Slider เพื่อให้ตรงกับโลกจริง
                    if (j < jointSliders.Length)
                    {
                        int pulseValue = RadianToPulse(msg.position[i], j);
                        jointSliders[j].value = pulseValue;
                        targetPulses[j] = pulseValue; // ป้องกันแขนกระตุกกลับ
                    }
                }
            }
        }
        isUpdatingFromROS = false;
    }

    // ฟังก์ชันยิงคำสั่งรวดเดียว 5 ตัว (ไหลลื่นกว่าแยกยิงทีละตัว)
    void SendAllServosCommand()
    {
        var msg = new ServosPositionMsg();
        msg.duration = 0.1f; // สั่งให้หุ่นขยับเสร็จใน 0.1 วิ (สมูทขึ้น)
        msg.position_unit = "pulse";

        var servoList = new List<ServoPositionMsg>();
        for (int i = 0; i < servoIDs.Length; i++)
        {
            var servo = new ServoPositionMsg();
            servo.id = (byte)servoIDs[i];
            servo.position = (ushort)targetPulses[i];
            servoList.Add(servo);
        }

        msg.position = servoList.ToArray();
        ros.Publish(pubTopic, msg);
    }

    void SendServoCommand(int index, int value)
    {
        var msg = new ServosPositionMsg();
        msg.duration = 0.1f;
        msg.position_unit = "pulse";

        var servo = new ServoPositionMsg();
        servo.id = (byte)servoIDs[index];
        servo.position = (ushort)value;

        msg.position = new ServoPositionMsg[] { servo };
        ros.Publish(pubTopic, msg);
    }

    int RadianToPulse(double rad, int index)
    {
        if (index < isInverted.Length && isInverted[index]) rad *= -1;
        double pulse = (rad * 500.0 / 2.094395) + 500.0;
        return (int)Mathf.Clamp((float)pulse, 0f, 1000f);
    }
}