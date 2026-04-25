using UnityEngine;
using UnityEngine.XR;

public class VRUIRecenterStable : MonoBehaviour
{
    public Transform head;
    public float distance = 2.0f;
    public float heightOffset = -0.2f; // ปรับให้ UI อยู่ต่ำลงจากระดับสายตานิดหน่อยจะดูสบายตาขึ้น

    private bool lastPressed = false;

    void Update()
    {
        // แนะนำให้ใช้แกนที่แน่นอน หรือเช็คเครื่องมือให้ชัวร์ (เช่น PrimaryButton คือปุ่ม X หรือ A)
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        if (leftHand.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed))
        {
            if (pressed && !lastPressed)
            {
                RecenterUI();
            }
            lastPressed = pressed;
        }
    }

    void RecenterUI()
    {
        if (head == null) return;

        // 1. หาตำแหน่งข้างหน้า (ตัดแกน Y ออกเพื่อให้ UI ไม่ลอยขึ้นฟ้าตามการเงยหน้า)
        Vector3 headForward = head.forward;
        headForward.y = 0; // บังคับให้ขนานกับพื้น
        Vector3 targetPosition = head.position + (headForward.normalized * distance);

        // 2. ปรับความสูงเล็กน้อย (ถ้าต้องการ)
        targetPosition.y += heightOffset;

        // 3. ปรับตำแหน่ง UI
        transform.position = targetPosition;

        // 4. ทำให้ UI หันหน้ามาหาผู้ใช้ตรงๆ (Look At)
        // เราต้องการให้ UI หันหลังกลับมาหาหัวเรา
        Vector3 lookPos = head.position - transform.position;
        lookPos.y = 0; // ล็อคแกน Y ไว้เพื่อไม่ให้ UI เอียงก้ม/เงย

        if (lookPos != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookPos);
        }
    }
}