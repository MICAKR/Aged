using UnityEngine;
// 1. ต้องเพิ่ม namespace ของ Input System ตัวใหม่เข้ามาก่อน
using UnityEngine.InputSystem;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;       // ความเร็วเดินปกติ
    public float sprintSpeed = 8f;     // ความเร็วตอนกด Shift วิ่ง
    private float currentSpeed;        // ความเร็วที่จะนำไปใช้จริงในเฟรมนั้นๆ

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isSprinting = false;  // ตัวแปรเช็คว่ากำลังวิ่งอยู่ไหม (เผื่อเอาไปใช้กับอนิเมชั่น)

    [Header("Jump Settings (Visual Only for Top-Down)")]
    public Transform spriteTransform;
    public float jumpDuration = 0.4f;
    public float jumpHeight = 1.2f;

    private bool isJumping = false;
    private float jumpTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentSpeed = walkSpeed; // เริ่มเกมมาให้ใช้ความเร็วเดินปกติก่อน

        if (spriteTransform == null && transform.childCount > 0)
        {
            spriteTransform = transform.GetChild(0);
        }
    }

    void Update()
    {
        HandleInput();
        HandleJumpLogic();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void HandleInput()
    {
        // 2. เปลี่ยนวิธีกด WASD มาใช้ Keyboard.current ของระบบใหม่แทน
        Vector2 moveVector = Vector2.zero;

        if (Keyboard.current != null)
        {
            // เช็คปุ่มแนวตั้ง (W / S)
            if (Keyboard.current.wKey.isPressed) moveVector.y = 1f;
            else if (Keyboard.current.sKey.isPressed) moveVector.y = -1f;

            // เช็คปุ่มแนวนอน (A / D)
            if (Keyboard.current.dKey.isPressed) moveVector.x = 1f;
            else if (Keyboard.current.aKey.isPressed) moveVector.x = -1f;

            // [เพิ่มเข้ามา] เช็คการกดปุ่ม Shift (ใช้ได้ทั้ง Left Shift และ Right Shift)
            isSprinting = Keyboard.current.shiftKey.isPressed;

            // 3. เช็คการกด Spacebar เพื่อกระโดด (wasPressedThisFrame ทำงานเหมือน GetKeyDown)
            if (Keyboard.current.spaceKey.wasPressedThisFrame && !isJumping)
            {
                StartJump();
            }
        }

        moveInput = moveVector.normalized;

        // คำนวณความเร็วปัจจุบันตามสถานะการกด Shift
        UpdateMovementSpeed();

        UpdateAnimationState();
    }

    private void UpdateMovementSpeed()
    {
        // ถ้ากดเดินอยู่และกด Shift ด้วย ให้ใช้ความเร็ววิ่งเร็ว แต่ถ้าไม่ได้กด Shift ก็เดินปกติ
        // (แถมเงื่อนไข: ถ้าความเร็วเป็น 0 หรืออยู่กลางอากาศ อาจจะเลือกให้วิ่งไม่ได้ตามใจชอบครับ)
        if (isSprinting && moveInput.sqrMagnitude > 0)
        {
            currentSpeed = sprintSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
    }

    private void MovePlayer()
    {
        // เปลี่ยนมาใช้ currentSpeed ที่สลับระว่างความเร็วเดิน/วิ่งแล้ว
        rb.MovePosition(rb.position + moveInput * currentSpeed * Time.fixedDeltaTime);
    }

    private void StartJump()
    {
        isJumping = true;
        jumpTimer = 0f;
        Debug.Log("Player Jumped!");
    }

    private void HandleJumpLogic()
    {
        if (!isJumping) return;

        jumpTimer += Time.deltaTime;
        float progress = jumpTimer / jumpDuration;

        if (progress <= 1f)
        {
            float currentHeight = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            spriteTransform.localPosition = new Vector3(0, currentHeight, 0);
        }
        else
        {
            EndJump();
        }
    }

    private void EndJump()
    {
        isJumping = false;
        spriteTransform.localPosition = Vector3.zero;
        Debug.Log("Player Landed!");
    }

    private void UpdateAnimationState()
    {
        // --- [พื้นที่ว่าง] สำหรับส่งค่าความเร็วและสถานะไปให้ Animator ---
        // คุณสามารถใช้ตัวแปร isSprinting ไปเช็คเพื่อเปลี่ยนท่าจากเดิน (Walk) เป็นวิ่ง (Run) ใน Animator ได้เลยครับ
        // เช่น animator.SetBool("IsSprinting", isSprinting);
    }
}