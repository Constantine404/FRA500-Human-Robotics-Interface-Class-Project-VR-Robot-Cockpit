using UnityEngine;
using UnityEngine.UI;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;

public class CircularRadarHud : MonoBehaviour
{
    public string topicName = "/scan";
    public RawImage displayImage; // ลาก RawImage ( RadarDisplay ) มาใส่

    [Header("Radar Settings")]
    public int textureResolution = 256; // ความละเอียดภาพเรดาร์ (พิกเซล)
    public float maxRadarDistance = 5.0f; // ระยะทางจริงที่ไกลที่สุดที่จะแสดงบนจอ (เมตร)
    public Color backgroundColor = new Color(0, 0.1f, 0, 0.2f); // สีพื้นหลังเรดาร์ (เขียวใส)

    [Header("Point Settings")]
    public int pointSize = 2; // ขนาดของจุดสี (พิกเซล)
    public Gradient distanceGradient; // สีไล่ระดับตามระยะ
    public bool invertX = false; // กลับซ้าย-ขวา
    [Range(0, 360)] public float rotationOffset = 0; // หมุนหน้าเรดาร์

    private Texture2D radarTexture;
    private Color[] clearPixels; // ไว้สำหรับล้างจอ
    private float center;
    private float radius;
    private LaserScanMsg lastMsg;
    private bool isNewData = false;

    void Start()
    {
        // 1. สร้าง Texture ว่าง และเตรียม Center/Radius
        center = textureResolution / 2f;
        radius = center - pointSize; // เผื่อขอบนิดหน่อย
        radarTexture = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        radarTexture.filterMode = FilterMode.Point; // ให้จุดคมชัด ไม่เบลอ
        displayImage.texture = radarTexture;

        // เตรียม Array สำหรับล้างจอ (ประสิทธิภาพสูงกว่า SetPixel ในลูป)
        clearPixels = new Color[textureResolution * textureResolution];
        for (int i = 0; i < clearPixels.Length; i++) clearPixels[i] = backgroundColor;

        // 2. Subscribe ROS
        ROSConnection.GetOrCreateInstance().Subscribe<LaserScanMsg>(topicName, (msg) => {
            lastMsg = msg;
            isNewData = true;
        });

        // 3. ตั้งค่า Gradient เริ่มต้นถ้าไม่มี
        if (distanceGradient == null) distanceGradient = DefaultGradient();
    }

    void Update()
    {
        if (isNewData && lastMsg != null)
        {
            DrawLidarRadar();
            isNewData = false;
        }
    }

    void DrawLidarRadar()
    {
        // 1. ล้างหน้าจอด้วยสีพื้นหลัง
        radarTexture.SetPixels(clearPixels);

        // 2. วาดจุด LiDAR
        for (int i = 0; i < lastMsg.ranges.Length; i++)
        {
            float range = lastMsg.ranges[i];

            // กรองระยะที่อ่านได้
            if (range > lastMsg.range_min && range < lastMsg.range_max)
            {
                // แปลงมุม (Polar -> Cartesian)
                float angle = lastMsg.angle_min + (i * lastMsg.angle_increment) + (rotationOffset * Mathf.Deg2Rad);
                float sinAngle = Mathf.Sin(angle);
                if (invertX) sinAngle *= -1; // กลับซ้าย-ขวา

                // คำนวณตำแหน่งพิกเซล (X, Y) บน Texture
                // สเกล: ระยะทางจริง 0 ถึง maxRadarDistance จะถูก map ลงบน Center ถึง Radius พิกเซล
                float distancePixel = (range / maxRadarDistance) * radius;

                int px = (int)(center + (distancePixel * sinAngle));
                int py = (int)(center + (distancePixel * Mathf.Cos(angle)));

                // 3. คำนวณสีตามระยะ
                float t = Mathf.InverseLerp(lastMsg.range_min, maxRadarDistance, range);
                Color pointColor = distanceGradient.Evaluate(t);

                // 4. ระบายสีจุดลงบน Texture (ระบายหลายพิกเซลตาม pointSize)
                DrawPoint(px, py, pointColor);
            }
        }

        // 5. บังคับอัปเดต Texture ลง GPU (ขั้นตอนนี้กินไฟสุด ต้องระวัง)
        radarTexture.Apply();
    }

    void DrawPoint(int x, int y, Color color)
    {
        // ระบายสีแบบสี่เหลี่ยมตามขนาด pointSize
        for (int dx = -pointSize / 2; dx <= pointSize / 2; dx++)
        {
            for (int dy = -pointSize / 2; dy <= pointSize / 2; dy++)
            {
                int px = x + dx;
                int py = y + dy;
                if (px >= 0 && px < textureResolution && py >= 0 && py < textureResolution)
                {
                    radarTexture.SetPixel(px, py, color);
                }
            }
        }
    }

    Gradient DefaultGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.red, 0.0f), new GradientColorKey(Color.green, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f) }
        );
        return g;
    }
}