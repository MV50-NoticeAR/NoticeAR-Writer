using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

struct GameManagerData
{
    public List<BrickData> bricks;
    public const float brickCircleRadius = 0.05f;
    public const float thresholdDistance = 0.05f;
    public const float bottomSocketY = -0.0185f; // 0.018 -> 0.032
}

struct BrickData
{
    public GameObject brick;
    public List<Vector3> topSocket;
    public List<Vector3> bottomSocket;
}

public class GameManager : MonoBehaviour
{
    public GameManager instance = null;
    GameManagerData gameManagerData = new GameManagerData();

    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        FetchBricks();
        DrawSockets();
    }

    void Update() {}

    void DrawSockets ()
    {
        foreach (BrickData brickData in gameManagerData.bricks)
        {
            foreach (Vector3 socket in brickData.topSocket)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.parent = brickData.brick.transform;
                sphere.transform.position = brickData.brick.transform.TransformPoint(socket);
                sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                
                MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
                Material material = renderer.material;
                material.color = Color.red;
            }

            foreach (Vector3 socket in brickData.bottomSocket)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.parent = brickData.brick.transform;
                sphere.transform.position = brickData.brick.transform.TransformPoint(socket);
                sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                
                MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
                Material material = renderer.material;
                material.color = Color.white;
            }
        }
    }

    void FetchBricks()
    {
        List<GameObject> allBricks = new List<GameObject>(GameObject.FindGameObjectsWithTag("Brick"));
        gameManagerData.bricks = new List<BrickData>();

        for (int i = 0; i < allBricks.Count; i++) 
        {
            gameManagerData.bricks.Add(new BrickData() 
                {
                    brick = allBricks[i], 
                    topSocket = new List<Vector3>(),
                    bottomSocket = new List<Vector3>(),
                }
            );

            MeshFilter meshFilter = allBricks[i].GetComponent<MeshFilter>();
            List<Vector3> meshVertices = new List<Vector3>(meshFilter.mesh.vertices);

            float highestY = 0;
            float lowestY = 0;
            List<Vector3> highestPoints = new List<Vector3>();

            foreach (Vector3 vertex in meshVertices)
            {
                if (vertex.y > highestY)
                {
                    highestY = vertex.y;
                    highestPoints.Clear();
                    highestPoints.Add(vertex);
                }
                else if (vertex.y == highestY)
                {
                    highestPoints.Add(vertex);
                }

                if (vertex.y < lowestY)
                {
                    lowestY = vertex.y;
                }
            }
            
            highestPoints = new List<Vector3>(new HashSet<Vector3>(highestPoints));
            List<List<Vector3>> groups = new List<List<Vector3>>();

            foreach (Vector3 pointA in highestPoints)
            {
                bool added = false;

                foreach (List<Vector3> group in groups)
                {
                    // groupe déjà plein
                    // TODO: le nombre de points par groupe peut varier en fonction du mesh, donc du modèle 3D
                    // TODO: Vérifier que les modèles de briques ont toujours 25 points par groupe
                    if (group.Count == 25) continue;

                    // Distance moyenne entre le point A et les points du groupe
                    float averageDistance = 0;

                    foreach (Vector3 pointB in group)
                    {
                        averageDistance += Vector3.Distance(pointA, pointB);
                    }

                    // On ajoute le point A au groupe si la distance moyenne est inférieure à la distance requise
                    if (averageDistance / group.Count <= GameManagerData.thresholdDistance)
                    {
                        group.Add(pointA);
                        added = true;
                        break;
                    }
                }

                // Si le point n'a pas été ajouté à un groupe existant, on crée un nouveau groupe
                if (!added)
                {
                    List<Vector3> newGroup = new List<Vector3>();
                    newGroup.Add(pointA);
                    groups.Add(newGroup);
                }
            }

            foreach (List<Vector3> group in groups)
            {
                Vector3 center = Vector3.zero;
                foreach (Vector3 point in group)
                {
                    center += point;
                }
                center /= group.Count;
                gameManagerData.bricks[i].topSocket.Add(center);
                gameManagerData.bricks[i].bottomSocket.Add(new Vector3(center.x, lowestY - GameManagerData.bottomSocketY, center.z));
            }
        }
    }

}
