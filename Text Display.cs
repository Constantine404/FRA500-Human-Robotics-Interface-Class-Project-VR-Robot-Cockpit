using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std; // UInt16Msg อยู่ใน namespace นี้
using TMPro;

public class ReadBattery : MonoBehaviour
{
    public string topicName = "/ros_robot_controller/battery";

    [Header("UI Settings (3D Text)")]
    public TextMeshPro batteryText;

    public Color healthyColor = Color.green;
    public Color criticalColor = Color.red;
    public float lowBatteryThreshold = 10.0f;

    void Start()
    {
        if (batteryText == null)
        {
            Debug.LogError($"[Battery] {gameObject.name}: ลืมลาก Text (TMP) มาใส่ในช่อง Battery Text!");
        }
        else
        {
            batteryText.text = "Wait for ROS (UInt16)...";
        }

        // เปลี่ยนตรงนี้เป็น UInt16Msg
        ROSConnection.GetOrCreateInstance().Subscribe<UInt16Msg>(topicName, UpdateBatteryStatus);

        Debug.Log($"[Battery] เริ่ม Subscribe Topic: {topicName} (Mode: UInt16)");
    }

    // เปลี่ยนพารามิเตอร์เป็น UInt16Msg
    void UpdateBatteryStatus(UInt16Msg msg)
    {
        // UInt16 มีค่าได้ตั้งแต่ 0 ถึง 65535
        // สมมติว่า ROS ส่งค่าเป็น mV (เช่น 12000 = 12V)
        float voltage = msg.data / 1000f;

        Debug.Log($"[Battery] Received UInt16: {msg.data} -> {voltage:F2}V");

        if (batteryText != null)
        {
            batteryText.text = $"Battery: {voltage:F2} V";

            if (voltage < lowBatteryThreshold)
            {
                batteryText.color = criticalColor;
            }
            else
            {
                batteryText.color = healthyColor;
            }
        }
    }
}