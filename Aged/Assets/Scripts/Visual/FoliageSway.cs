using UnityEngine;
using System.Collections;

public class FoliageSway : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("องศาการเอนสูงสุด (ยิ่งมากยิ่งเอนเยอะ)")]
    public float maxSwayAngle = 15f;
    [Tooltip("ความเร็วในการเด้งไปมา")]
    public float wobbleSpeed = 15f;
    [Tooltip("ระยะเวลาที่พุ่มไม้จะสั่นจนกว่าจะหยุดนิ่ง")]
    public float swayDuration = 0.5f;

    private bool isSwaying = false;
    private Quaternion originalRotation;

    void Start()
    {
        // จำค่าการหมุนเริ่มต้นไว้
        originalRotation = transform.rotation;
    }

    // ฟังก์ชันนี้จะทำงานเมื่อมีคนเดินเข้ามาในระยะ Collider
    void OnTriggerEnter2D(Collider2D other)
    {
        // เช็คว่าคนที่เดินชนมี Tag ว่า "Player" และพุ่มไม้ต้องไม่ได้สั่นอยู่
        if (other.CompareTag("Player") && !isSwaying)
        {
            // คำนวณทิศทาง: ถ้าผู้เล่นอยู่ซ้ายของพุ่มไม้ แปลว่าเดินมาทางขวา พุ่มไม้ต้องเอนไปทางขวา (-1)
            float hitDirection = (other.transform.position.x < transform.position.x) ? -1f : 1f;

            StartCoroutine(SwayRoutine(hitDirection));
        }
    }

    private IEnumerator SwayRoutine(float direction)
    {
        isSwaying = true;
        float elapsed = 0f;

        while (elapsed < swayDuration)
        {
            elapsed += Time.deltaTime;

            // คำนวณเปอร์เซ็นต์เวลาที่ผ่านไป (0.0 ถึง 1.0)
            float percent = elapsed / swayDuration;

            // ใช้สูตร Damped Sine Wave: สั่นไปมาและค่อยๆ เบาลงจนหยุด (1 - percent)
            float damping = 1f - percent;
            float currentAngle = maxSwayAngle * direction * damping * Mathf.Sin(elapsed * wobbleSpeed);

            // สั่งหมุนพุ่มไม้แกน Z
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, currentAngle);

            yield return null;
        }

        // คืนค่ากลับมาตั้งตรงเหมือนเดิมเมื่อสั่นเสร็จ
        transform.rotation = originalRotation;
        isSwaying = false;
    }
}