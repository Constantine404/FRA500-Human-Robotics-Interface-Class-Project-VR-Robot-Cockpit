using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor; // สำหรับ CompressedImageMsg

public class CameraStreamer : MonoBehaviour
{
    public RawImage displayImage; // ลาก RawImage ที่สร้างไว้มาใส่ที่นี่
    public string topicName = "/depth_cam/rgb/image_raw/compressed";

    private Texture2D texture;
    private byte[] imageData;
    private bool isMessageReceived = false;

    void Start()
    {
        // สร้าง Texture ว่างๆ รอไว้
        texture = new Texture2D(2, 2);

        // เริ่มต้นการดึงข้อมูลภาพ
        ROSConnection.GetOrCreateInstance().Subscribe<CompressedImageMsg>(topicName, ReceiveImage);
    }

    void ReceiveImage(CompressedImageMsg msg)
    {
        // เก็บข้อมูลภาพไว้ใน Byte Array
        imageData = msg.data;
        isMessageReceived = true;
    }

    void Update()
    {
        if (isMessageReceived)
        {
            // แปลงข้อมูลจาก ROS เป็น Texture ใน Unity
            texture.LoadImage(imageData);

            // แสดงภาพบน UI
            displayImage.texture = texture;

            isMessageReceived = false;
        }
    }
}