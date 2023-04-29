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
    public Dictionary<string, List<Vector3>> sockets;
}

public class GameManager : MonoBehaviour {
    public GameManager instance = null;
    GameManagerData gameManagerData = new GameManagerData();

    void Awake() {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        FetchBricks();
        DrawSockets();
    }

    void Update() {}

    void DrawSocket(GameObject parent, Vector3 pos, Color color) {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = parent.transform;
        sphere.transform.position = parent.transform.TransformPoint(pos);
        sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
        Material material = renderer.material;
        material.color = color;
    }

    void DrawSockets () {
        foreach (BrickData brickData in gameManagerData.bricks) {
            foreach (KeyValuePair<string, List<Vector3>> entry in brickData.sockets) {
                foreach (Vector3 socket in entry.Value) {
                    DrawSocket(brickData.brick, socket, Color.red);
                }
            }
        }
    }

    void FetchBricks() {
        List<GameObject> allBricks = new List<GameObject>(GameObject.FindGameObjectsWithTag("Brick"));
        gameManagerData.bricks = new List<BrickData>();

        for (int i = 0; i < allBricks.Count; i++) {
            gameManagerData.bricks.Add(new BrickData() 
                {
                    brick = allBricks[i], 
                    sockets = new Dictionary<string, List<Vector3>>(),
                }
            );

            MeshFilter meshFilter = allBricks[i].GetComponent<MeshFilter>();
            List<Vector3> meshVertices = new List<Vector3>(meshFilter.mesh.vertices);

            float top = 0;
            float bottom = 0;
            float left = 0;
            float right = 0;
            float front = 0;
            float back = 0;
            
            Dictionary<string, List<Vector3>> points = new Dictionary<string, List<Vector3>>();
            points.Add("TOP", new List<Vector3>());
            points.Add("LEFT", new List<Vector3>());
            points.Add("RIGHT", new List<Vector3>());
            points.Add("FRONT", new List<Vector3>());
            points.Add("BACK", new List<Vector3>());

            foreach (Vector3 vertex in meshVertices) {
                if (vertex.y > top)
                {
                    top = vertex.y;
                    points["TOP"].Clear();
                    points["TOP"].Add(vertex);
                }
                else if (vertex.y == top) points["TOP"].Add(vertex);

                if (vertex.y < bottom)
                {
                    bottom = vertex.y;
                }

                if (vertex.x < left)
                {
                    left = vertex.x;
                    points["LEFT"].Clear();
                    points["LEFT"].Add(vertex);
                }
                else if (vertex.x == left) points["LEFT"].Add(vertex);

                if (vertex.x > right)
                {
                    right = vertex.x;
                    points["RIGHT"].Clear();
                    points["RIGHT"].Add(vertex);
                }
                else if (vertex.x == right) points["RIGHT"].Add(vertex);

                if (vertex.z > front)
                {
                    front = vertex.z;
                    points["FRONT"].Clear();
                    points["FRONT"].Add(vertex);
                }
                else if (vertex.z == front) points["FRONT"].Add(vertex);

                if (vertex.z < back)
                {
                    back = vertex.z;
                    points["BACK"].Clear();
                    points["BACK"].Add(vertex);
                }
                else if (vertex.z == back) points["BACK"].Add(vertex);
            }

            Dictionary<string, List<Vector3>> filteredPoints = new Dictionary<string, List<Vector3>>();

            foreach (KeyValuePair<string, List<Vector3>> entry in points)
                filteredPoints.Add(entry.Key, new List<Vector3>(new HashSet<Vector3>(entry.Value)));

            foreach (KeyValuePair<string, List<Vector3>> entry in filteredPoints) {
                List<List<Vector3>> groups = new List<List<Vector3>>();

                foreach (Vector3 pointA in entry.Value) {
                    bool added = false;

                    foreach (List<Vector3> group in groups) {
                        // groupe déjà plein
                        if (group.Count == 25) continue;

                        float averageDist = 0;

                        foreach (Vector3 pointB in group)
                            averageDist += Vector3.Distance(pointA, pointB);

                        if (averageDist / group.Count <= GameManagerData.thresholdDistance)
                        {
                            group.Add(pointA);
                            added = true;
                            break;
                        }

                    }

                    if (!added) groups.Add(new List<Vector3>() { pointA });
                }

                foreach (List<Vector3> group in groups) {
                    Vector3 center = Vector3.zero;
                    foreach (Vector3 point in group)
                        center += point;

                    center /= group.Count;

                    switch (entry.Key) {
                        case "TOP":
                            if (gameManagerData.bricks[i].sockets.ContainsKey("TOP"))
                            {
                                gameManagerData.bricks[i].sockets["TOP"].Add(center);
                                gameManagerData.bricks[i].sockets["BOTTOM"].Add(new Vector3(center.x, bottom - GameManagerData.bottomSocketY, center.z));
                            }
                            else
                            {
                                gameManagerData.bricks[i].sockets.Add("TOP", new List<Vector3>() { center });
                                gameManagerData.bricks[i].sockets.Add("BOTTOM", new List<Vector3>() { new Vector3(center.x, bottom - GameManagerData.bottomSocketY, center.z)});
                            }
                            break;
                        
                        default:
                            if (gameManagerData.bricks[i].sockets.ContainsKey(entry.Key))
                                gameManagerData.bricks[i].sockets[entry.Key].Add(center);
                            else
                                gameManagerData.bricks[i].sockets.Add(entry.Key, new List<Vector3>() { center });
                            break;
                    }
                }
            }
        }
    }

}
