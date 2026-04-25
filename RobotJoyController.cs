using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class RobotControllerXR : MonoBehaviour
{
    ROSConnection ros;
    public string topicName = "/cmd_vel";

    private InputDevice leftHandDevice;

    public float linearSpeed = 1.0f;
    public float angularSpeed = 1.0f;

    void Start()
    {
        // ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<TwistMsg>(topicName);

        // 🔥 ใช้ list เดียวพอ
        var devices = new List<InputDevice>();

        // เอาแบบรวมทุก device (debug ง่ายสุด)
        InputDevices.GetDevices(devices);

        foreach (var d in devices)
        {
            Debug.Log($"Device found: {d.name}, role: {d.characteristics}");
        }

        // แล้วค่อยหา Left Hand
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);

        if (devices.Count > 0)
        {
            leftHandDevice = devices[0];
            Debug.Log("Left controller assigned");
        }
        else
        {
            Debug.Log("No Left controller found");
        }
    }

    void Update()
    {
        if (!leftHandDevice.isValid)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);

            if (devices.Count > 0)
            {
                leftHandDevice = devices[0];
            }
        }

        Vector2 joystickInput;
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out joystickInput))
        {
            float h = joystickInput.x;
            float v = joystickInput.y;

            TwistMsg twist = new TwistMsg();

            twist.linear.x = v * linearSpeed;
            twist.angular.z = -h * angularSpeed;

            // 🔥 DEBUG ตรงนี้
            Debug.Log($"Joystick: h={h:F2}, v={v:F2}");
            Debug.Log($"Publish: linear.x={twist.linear.x:F2}, angular.z={twist.angular.z:F2}");

            ros.Publish(topicName, twist);
        }
    }
}