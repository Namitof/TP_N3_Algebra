using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

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
    [SerializeField] private Transform testPoint;
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

    //Planos de cada objeto
    [System.Serializable]
    private struct PlaneData
    {
        public Vector3 normal;
        public float d;
        public Vector3 point;
    }
    private List<PlaneData>[] planes = new List<PlaneData>[MAX_OBJECTS];

    // Datos para la grilla cubica equidistante
    const int WIDTH = 15;
    const int HEIGHT = 15;
    const int DEPTH = 15;
    const float POINTS_OFFSET = 1.0f;
    private Vector3[,,] grid = new Vector3[WIDTH, HEIGHT, DEPTH];

    private string collisionStatus = "";

    private void Start()
    {
        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            planes[i] = new List<PlaneData>();
        }

        InitializeGrid();

        CalculateBoundingBoxes();
        CalculatePlanes();
        CheckBoundingBoxCollisions();
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

        CheckBoundingBoxCollisions();
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
        CalculatePlanes();
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
        CalculatePlanes();
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
        CalculatePlanes();

    }
    private void CalculateBoundingBoxes()
    {
        //https://www.pbr-book.org/3ed-2018/Geometry_and_Transformations/Bounding_Boxes

        //Recorro cada objeto
        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            //MeshFilters -> componente de Unity que contiene una referencia a la geometría (Mesh) del objeto.
            MeshFilter[] meshFilters = objects[i].GetComponentsInChildren<MeshFilter>();

            bool firstVertex = true;

            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            for (int j = 0; j < meshFilters.Length; j++)
            {
                //Me quedo con la mesh (usando sharedMesh)
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
                    //MultiplyPoint3x4 -> Rota, escala y traslada un punto en el espacio 3D

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
        //https://aga.frba.utn.edu.ar/ecuaciones-del-plano/

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
                    //Pasar las coordenadas locales de la malla a las coordenadas globales del mundo.
                    //P0, P1, P2 en coordenadas del mundo
                    Vector3 p0 = localToWorld.MultiplyPoint3x4(vertices[triangles[k]]);
                    Vector3 p1 = localToWorld.MultiplyPoint3x4(vertices[triangles[k + 1]]);
                    Vector3 p2 = localToWorld.MultiplyPoint3x4(vertices[triangles[k + 2]]);

                    //Normal = producto vectorial de dos aristas
                    Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0);

                    //Si el triangulo es degenerado (area 0), no define un plano (si la magnitud del vector que representa la normal es cero)
                    if (normal.magnitude < 0.000001f)
                    {
                        continue;
                    }

                    //Para normalizar divido cada componente del vector por su magnitud
                    normal.Normalize();

                    //Ecuación Normal del Plano -> N . (P - P0) = 0
                    // N -> Vector normal
                    // P -> Punto a evaluar
                    // P0 -> Punto perteneciente al plano

                    //Hago distributiva con N
                    // (N . P) - (N . P0) = 0
                    // -N . P0 da siempre un numero fijo -> D

                    // N . P + D = 0

                    //D = -(n . P0)
                    float d = -Vector3.Dot(normal, p0);

                    //Si el centro queda del lado negativo, la normal apunta afuera: la invierto
                    if (Vector3.Dot(normal, center) + d < 0.000001f) //tolerancia o precision
                    {
                        normal = -normal;
                        d = -d;
                    }

                    //Centroide del triángulo (baricentro) para dibujar mas adelante la normal
                    Vector3 trianglePoint = (p0 + p1 + p2) / 3f;
                    planes[i].Add(new PlaneData { normal = normal, d = d, point = trianglePoint });
                }
            }
        }
    }

    // Devuelve true si el punto esta dentro del modelo del objeto indicado
    private bool IsPointInsideModel(int objectIndex, Vector3 point)
    {
        List<PlaneData> objectPlanes = planes[objectIndex];

        if (objectPlanes == null || objectPlanes.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < objectPlanes.Count; i++)
        {
            //Distancia con signo del punto al plano
            float distance = Vector3.Dot(objectPlanes[i].normal, point) + objectPlanes[i].d;

            //Si esta del lado negativo de algun plano, esta afuera
            if (distance < 0.000001f)
            {
                return false;
            }
        }

        //Estuvo del lado positivo de todos los planos
        return true;
    }

    // Devuelve true si las bounding boxes de dos objetos se superponen
    private bool BoundingBoxesOverlap(int indexA, int indexB)
    {
        //https://developer.mozilla.org/en-US/docs/Games/Techniques/3D_collision_detection

        BoundingBoxData a = boundingBoxes[indexA];
        BoundingBoxData b = boundingBoxes[indexB];

        //Dos cajas se superponen solo si se superponen en LOS TRES ejes
        bool overlapX = a.min.x <= b.max.x && a.max.x >= b.min.x;
        bool overlapY = a.min.y <= b.max.y && a.max.y >= b.min.y;
        bool overlapZ = a.min.z <= b.max.z && a.max.z >= b.min.z;

        return overlapX && overlapY && overlapZ;
    }
    private void CheckBoundingBoxCollisions()
    {
        collisionStatus = "";

        for (int i = 0; i < MAX_OBJECTS; i++)
        {
            if (objects[i] == null) continue;

            //j arranca en i + 1 para no comparar un objeto consigo mismo ni repetir pares (1-2 y 2-1)
            for (int j = i + 1; j < MAX_OBJECTS; j++)
            {
                if (objects[j] == null) continue;

                if (BoundingBoxesOverlap(i, j))
                {
                    collisionStatus += "AABB | El objeto " + (i + 1) + " y el objeto " + (j + 1) + " colisionan\n";
                    CheckCollision(i, j); //Chequear colision completa
                }
            }
        }

        if (collisionStatus == "")
        {
            collisionStatus = "Sin colision";
        }
    }

    //Chequear colisiones pasando index de 2 modelos
    private void CheckCollision(int indexA, int indexB)
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int z = 0; z < DEPTH; z++)
                {
                    if (IsPointInsideModel(indexA, grid[x, y, z]) && IsPointInsideModel(indexB, grid[x, y, z]))
                    {
                        Debug.Log("COLISION EXTACTA ENTRE " + (indexA+1) + " Y " + (indexB+1));
                        collisionStatus = "COLISION EXACTA " + (indexA + 1) + " Y " + (indexB + 1);
                    }
                }
            }
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 32;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.yellow;

        float width = 700f;
        float height = 210f;
        float x = (Screen.width - width) / 8f;
        float y = (Screen.height - height) / 8f;

        GUI.Box(new Rect(x, y, width, height), "");
        GUI.Label(new Rect(x + 10, y + 10, width - 20, height - 20), collisionStatus, style);
    }

    private void InitializeGrid()
    {
        Vector3 origin = transform.position; //Transform del object manipulator

        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int z = 0; z < DEPTH; z++)
                {
                    grid[x, y, z] = new Vector3(origin.x + (x * POINTS_OFFSET), origin.y + (y * POINTS_OFFSET), origin.z + (z * POINTS_OFFSET));
                }
            }
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

            if (planes[i] == null)
            {
                continue;
            }

            Gizmos.color = Color.red;

            for (int p = 0; p < planes[i].Count; p++)
            {
                Gizmos.DrawRay(planes[i][p].point, planes[i][p].normal * 0.3f);
            }
        }

        //Dibujar puntos de la grilla cubica equidistante
        Gizmos.color = Color.cyan;

        //Recorrer grilla cubica equidistante
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int z = 0; z < DEPTH; z++)
                {
                    Gizmos.DrawSphere(grid[x, y, z], 0.05f); //Ultimo parametro es el radio
                }
            }
        }
    }

}