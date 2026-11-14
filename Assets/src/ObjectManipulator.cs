using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ObjectManipulator : MonoBehaviour
{
    const int MAX_OBJECTS = 6;

    [Header("Objects")]
    [SerializeField] private Transform[] objects = new Transform[MAX_OBJECTS];

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 60f;

    [Header("Scale")]
    [SerializeField] private float scaleSpeed = 1f;

    private Transform selectedObject;

    //Bounding Box
    [System.Serializable]
    private struct BoundingBoxData
    {
        public Vector3 min;
        public Vector3 max;
        public Vector3 center;
        public Vector3 size;
    }
    private BoundingBoxData[] boundingBoxes = new BoundingBoxData[MAX_OBJECTS];

    [System.Serializable]
    private struct PlaneData
    {
        public Vector3 normal;
        public float d;
    }
    //Planos de cada objeto
    private List<PlaneData>[] planes = new List<PlaneData>[MAX_OBJECTS];

    // Datos para la grilla cubica equidistante
    const int WIDTH = 5;
    const int HEIGHT = 5;
    const int DEPTH = 5;
    const float POINTS_OFFSET = 1.0f;
    private Vector3[,,] grid = new Vector3[WIDTH, HEIGHT, DEPTH];

    private void Start()
    {
        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            planes[i] = new List<PlaneData>();
        }

        CalculateBoundingBoxes();
        CalculatePlanes();
    }

    // Update is called once per frame
    private void Update()
    {
        SelectObject();

        if (selectedObject != null)
        {
            MoveObject();
            RotateObject();
            ScaleObject();
        }

        CalculateBoundingBoxes();
        CalculatePlanes();
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

        CalculateBoundingBoxes();
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

        CalculateBoundingBoxes();
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

        CalculateBoundingBoxes();

    }
    private void CalculateBoundingBoxes()
    {
        //Recorro cada objeto
        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            MeshFilter[] meshFilters = objects[i].GetComponentsInChildren<MeshFilter>();

            bool firstVertex = true;

            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            for (int j = 0; j < meshFilters.Length; j++)
            {
                //Me quedo con la mesh
                Mesh mesh = meshFilters[j].sharedMesh;

                if (mesh == null)
                {
                    continue;
                }

                //Me guardo cada vertice
                Vector3[] vertices = mesh.vertices;

                //Recorro para sacar el minimo y maximo en cada eje (X, Y, Z)
                for (int k = 0; k < vertices.Length; k++)
                {
                    //Transforma cada vertice de coordenadas locales de la mesh a las globales del mundo
                    Vector3 worldVertex = meshFilters[j].transform.localToWorldMatrix.MultiplyPoint3x4(vertices[k]);

                    // localToWorldMatrix es una matriz de transformación de 4x4
                    // que contiene la información para convertir coordenadas locales a coordenadas globales.

                    if (firstVertex)
                    {
                        min = worldVertex;
                        max = worldVertex;
                        firstVertex = false;
                    }
                    else
                    {
                        min.x = Mathf.Min(min.x, worldVertex.x);
                        min.y = Mathf.Min(min.y, worldVertex.y);
                        min.z = Mathf.Min(min.z, worldVertex.z);

                        max.x = Mathf.Max(max.x, worldVertex.x);
                        max.y = Mathf.Max(max.y, worldVertex.y);
                        max.z = Mathf.Max(max.z, worldVertex.z);
                    }
                }
            }

            if (firstVertex)
            {
                continue;
            }

            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;

            boundingBoxes[i] = new BoundingBoxData
            {
                min = min,
                max = max,
                center = center,
                size = size
            };
        }
    }
    private void CalculatePlanes()
    {
        //Recorro cada objeto
        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            //Vacio la lista: los planos viejos ya no sirven porque el objeto se pudo mover
            planes[i].Clear();

            //Uso el centro de la bounding box como "centro del modelo"
            Vector3 center = boundingBoxes[i].center;

            MeshFilter[] meshFilters = objects[i].GetComponentsInChildren<MeshFilter>();

            for (int j = 0; j < meshFilters.Length; j++)
            {
                Mesh mesh = meshFilters[j].sharedMesh;

                if (mesh == null)
                {
                    continue;
                }

                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;
                Matrix4x4 localToWorld = meshFilters[j].transform.localToWorldMatrix;

                //Recorro de a 3 indices: cada grupo de 3 es un triangulo
                for (int k = 0; k < triangles.Length; k += 3)
                {
                    //P0, P1, P2 en coordenadas del mundo
                    Vector3 p0 = localToWorld.MultiplyPoint3x4(vertices[triangles[k]]);
                    Vector3 p1 = localToWorld.MultiplyPoint3x4(vertices[triangles[k + 1]]);
                    Vector3 p2 = localToWorld.MultiplyPoint3x4(vertices[triangles[k + 2]]);

                    //Normal = producto vectorial de dos aristas
                    Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);

                    //Si el triangulo es degenerado (area 0), no define un plano
                    if (normal.sqrMagnitude < 0.000001f)
                    {
                        continue;
                    }

                    normal.Normalize();

                    //D = -(n . P0)
                    float d = -Vector3.Dot(normal, p0);

                    //Si el centro queda del lado negativo, la normal apunta afuera: la invierto
                    if (Vector3.Dot(normal, center) + d < 0f)
                    {
                        normal = -normal;
                        d = -d;
                    }

                    planes[i].Add(new PlaneData { normal = normal, d = d });
                }
            }

            Debug.Log("Objeto " + (i + 1) + " -> planos: " + planes[i].Count);
        }
    }

    // DIBUJAR BOUNDING BOX CON GIZMOS
    // - Unity ejecuta OnDrawGizmos() automáticamente.
    // - Recorremos las Bounding Boxes calculadas.
    private void OnDrawGizmos()
    {
        if (boundingBoxes == null)
        {
            return;
        }

        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            if (objects == null || objects[i] == null)
            {
                continue;
            }

            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);

            //Dibujar cubo
            Gizmos.DrawCube(boundingBoxes[i].center, boundingBoxes[i].size);

            Gizmos.color = Color.green;

            //Dibujar bordes del cubo
            Gizmos.DrawWireCube(boundingBoxes[i].center, boundingBoxes[i].size);
        }
    }

}