using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    [Header("Objects")]
    [SerializeField] private Transform[] objects = new Transform[6];

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 60f;

    [Header("Scale")]
    [SerializeField] private float scaleSpeed = 1f;

    private Transform selectedObject;

    // Update is called once per frame
    private void Update()
    {
        SelectObject();

        if (selectedObject == null)
            return;

        MoveObject();
        RotateObject();
        ScaleObject();
    }

    private void SelectObject()
    {
        for (int i = 0; i < objects.Length; i++)
        {
            int keyNumber = (int)KeyCode.Alpha1 + i;

            KeyCode keyCode = (KeyCode)keyNumber;

            if (Input.GetKeyDown(keyCode))
            {
                selectedObject = objects[i];
                Debug.Log("Objeto Actual -> " + (i + 1));
            }
        }
    }

    private void MoveObject()
    {
        if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.T))
            return;

        Vector3 movement = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            movement += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            movement += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            movement += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            movement += Vector3.right;

        if (Input.GetKey(KeyCode.LeftShift))
            movement += Vector3.up;

        if (Input.GetKey(KeyCode.LeftControl))
            movement += Vector3.down;

        selectedObject.position += movement * (moveSpeed * Time.deltaTime);
    }

    private void RotateObject()
    {
        if (!Input.GetKey(KeyCode.R))
            return;

        Vector3 rotation = Vector3.zero;

        if (Input.GetKey(KeyCode.UpArrow))
            rotation.x -= 1;

        if (Input.GetKey(KeyCode.DownArrow))
            rotation.x += 1;

        if (Input.GetKey(KeyCode.LeftArrow))
            rotation.y -= 1;

        if (Input.GetKey(KeyCode.RightArrow))
            rotation.y += 1;

        if (Input.GetKey(KeyCode.LeftShift))
            rotation.z += 1;

        if (Input.GetKey(KeyCode.LeftControl))
            rotation.z -= 1;

        selectedObject.Rotate(rotation * (rotationSpeed * Time.deltaTime), Space.World);
    }

    private void ScaleObject()
    {
        if (!Input.GetKey(KeyCode.T))
            return;

        float scale = 0;

        if (Input.GetKey(KeyCode.UpArrow))
            scale = 1;

        if (Input.GetKey(KeyCode.DownArrow))
            scale = -1;

        Vector3 newScale = selectedObject.localScale + Vector3.one * (scale * scaleSpeed * Time.deltaTime);

        newScale.x = Mathf.Max(0.1f, newScale.x);
        newScale.y = Mathf.Max(0.1f, newScale.y);
        newScale.z = Mathf.Max(0.1f, newScale.z);

        selectedObject.localScale = newScale;
    }
}