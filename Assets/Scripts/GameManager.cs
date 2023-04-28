using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameManager : MonoBehaviour
{
    public GameManager instance = null;
    public List<GameObject> bricks = new List<GameObject>();
    public float brickCircleRadius = 0.05f;
    
    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        SetupScene();
    }

    void Update() {}

    void SetupScene()
    {
        FetchBricks();
    }

    void FetchBricks()
    {
        bricks = new List<GameObject>(GameObject.FindGameObjectsWithTag("Brick"));
        foreach (var brick in bricks)
        {
            Debug.Log(brick.name);

            MeshFilter meshFilter = brick.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = brick.GetComponent<MeshRenderer>();
            SphereCollider sphereCollider = brick.GetComponent<SphereCollider>();

            // Récupération des vertices du mesh
            List<Vector3> meshVertices = new List<Vector3>(meshFilter.mesh.vertices);

            // Variable tampon pour stocker le point le plus haut du mesh
            float highestY = 0;
            // Ensemble des points les plus hauts du mesh
            List<Vector3> points = new List<Vector3>();

            // On parcourt tous les points du mesh pour trouver les points les plus haut
            // TODO : utiliser un algorithme de recherche plus efficace (sans doublons)
            foreach (Vector3 vertex in meshVertices)
            {
                if (vertex.y > highestY)
                {
                    highestY = vertex.y;
                    points.Clear();
                    points.Add(vertex);
                }
                else if (vertex.y == highestY)
                {
                    points.Add(vertex);
                }
            }

            // On supprime les doublons
            // TODO : à enlever une fois l'algorithme de recherche plus efficace implémenté
            points = new List<Vector3>(new HashSet<Vector3>(points));
            // Debug.Log("Number of highest points: " + points.Count);

            // Regroupement des points les plus hauts en fonction de leur distance moyenne au sein d'un même groupe
            // Distance requise pour faire partis du même groupe
            float thresholdDistance = 0.05f;
            // Ensemble des groupes de points
            List<List<Vector3>> groups = new List<List<Vector3>>();

            foreach (Vector3 pointA in points)
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
                    if (averageDistance / group.Count <= thresholdDistance)
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

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.parent = brick.transform;
                sphere.transform.position = brick.transform.TransformPoint(center);
                sphere.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                
                MeshRenderer renderer = sphere.GetComponent<MeshRenderer>();
                Material material = renderer.material;
                material.color = Color.red;

                // TODO : à voir comment exposer de façon publique les points de chaque socket 
            }
        }
    }

}
