using UnityEngine;

public class muvemanttag : MonoBehaviour
{
    // --- НАСТРОЙКИ ---
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float mouseSensitivity = 3f;
    public Transform cameraTransform;

    // --- КОМПОНЕНТЫ ---
    private Rigidbody rb;
    private float cameraRotationX = 0f;

    void Start()
    {
        // Получаем компонент Rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true; // Физика не вращает игрока
        }

        // Блокируем и скрываем курсор
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        MovePlayer();
        
        // Камера вращается всегда (если нужно заморозить и её, помести RotateCamera() внутрь if)
        RotateCamera();
}
    void MovePlayer()
    {
        // Получаем ввод с клавиатуры
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Определяем скорость (бег или ходьба)
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

        // Вычисляем вектор движения относительно направления взгляда игрока
        Vector3 movement = transform.right * moveX + transform.forward * moveZ;

        // Перемещаем физическое тело игрока
        rb.MovePosition(rb.position + movement * currentSpeed * Time.deltaTime);
    }

    void RotateCamera()
    {
        // Получаем движение мыши
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Вращаем тело игрока по горизонтали (Y)
        transform.Rotate(Vector3.up * mouseX);

        // Вращаем камеру по вертикали (X) с ограничением угла
        cameraRotationX -= mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
        
        cameraTransform.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);
    }

}
