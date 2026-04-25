using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class RobotStateSubscriber : MonoBehaviour
{
    public string topicName = "/joint_states";

    [Header("Robot Mapping")]
    // ใส่ชื่อ Joint ให้ตรงกับใน ROS (ปกติคือ joint1, joint2, ..., joint5)
    public string[] targetJointNames = { "joint1", "joint2", "joint3", "joint4", "joint5" };
    // ลาก ArticulationBody มาใส่ให้ลำดับตรงกับชื่อด้านบน
    public ArticulationBody[] robotJoints;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<JointStateMsg>(topicName, OnJointStatesReceived);
        Debug.Log("<color=cyan>VR Robot Subscriber: Listening...</color>");
    }

    void OnJointStatesReceived(JointStateMsg msg)
    {
        // 1. วนลูปหาชื่อ Joint ที่ได้รับมาจาก ROS
        for (int i = 0; i < msg.name.Length; i++)
        {
            // 2. เปรียบเทียบกับชื่อที่เราต้องการ (Target Names)
            for (int j = 0; j < targetJointNames.Length; j++)
            {
                if (msg.name[i] == targetJointNames[j])
                {
                    // 3. ถ้าชื่อตรงกัน และมี ArticulationBody รองรับ
                    if (j < robotJoints.Length && robotJoints[j] != null)
                    {
                        float angleInDegree = (float)(msg.position[i] * Mathf.Rad2Deg);

                        // อัปเดตตำแหน่งหุ่นในจอ VR
                        var drive = robotJoints[j].xDrive;
                        drive.target = angleInDegree;
                        robotJoints[j].xDrive = drive;
                    }
                }
            }
        }
    }
}