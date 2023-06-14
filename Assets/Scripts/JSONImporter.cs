using System.IO;
using UnityEngine;
using System.Collections.Generic;
using SimpleFileBrowser;
using static Unity.VisualScripting.Member;

[System.Serializable]
public class Position
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class Rotation
{
    public float x;
    public float y;
    public float z;
    public float w;
}

[System.Serializable]
public class Piece
{
    public string model;
    public string color;
    public Position position;
    public Rotation rotation;
}

[System.Serializable]
public class Step
{
    public string name;
    public string description;
    public List<Piece> pieces;
}

[System.Serializable]
public class Schematic
{
    public string name;
    public string picture;
    public string author;
    public string description;
    public string version;
    public List<Step> steps;
}

public class JSONImporter : MonoBehaviour
{
    private string jsonDirectoryPath;
    private GameObject parentObject;

    public void OpenFolderBrowser()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowLoadDialog(
            (string[] paths) => {
                if (paths.Length > 0)
                {
                    jsonDirectoryPath = paths[0];
                    Debug.Log("Selected file path: " + jsonDirectoryPath);

                    // Import the JSON file from the selected path
                    ImportJSONFile(jsonDirectoryPath);
                }
            },
            () => {
                Debug.Log("File selection cancelled.");
            },
            FileBrowser.PickMode.Files,
            false,
            null,
            "Select JSON File",
            "Select"
        );
    }

    void ImportJSONFile(string filePath)
    {
        GameObject parentObjectToCleanup = GameObject.Find("NoticeRoot");
        if (parentObjectToCleanup)
        {
            Destroy(parentObjectToCleanup);
        }
        parentObject = new GameObject("NoticeRoot");

        string jsonFileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
        Debug.Log("Importing .json file: " + jsonFileName);

        try
        {
            string jsonFileContent = File.ReadAllText(filePath);
            Schematic schematic = JsonUtility.FromJson<Schematic>(jsonFileContent);

            // Create a SchematicMetadata component and assign the values
            SchematicMetadata schematicMetadata = parentObject.AddComponent<SchematicMetadata>();
            schematicMetadata.name = schematic.name;
            schematicMetadata.picture = schematic.picture;
            schematicMetadata.author = schematic.author;
            schematicMetadata.description = schematic.description;
            schematicMetadata.version = schematic.version;

            foreach (Step step in schematic.steps)
            {
                GameObject stepObj = new GameObject(step.name);
                stepObj.transform.SetParent(parentObject.transform);

                // Create a StepMetadata component and assign the description
                StepMetadata stepMetadata = stepObj.AddComponent<StepMetadata>();
                stepMetadata.Description = step.description;

                foreach (Piece piece in step.pieces)
                {
                    Transform pieceTransform = GameObject.Find("ImportedObjs").transform.Find(piece.model);
                    if (pieceTransform == null)
                    {
                        Debug.LogError("Could not find a GameObject named " + piece.model);
                        continue;
                    }

                    GameObject pieceObj = Instantiate(pieceTransform.gameObject);
                    pieceObj.name = pieceTransform.gameObject.name; // prevent the addition of (clone) made by Instantiate
                    pieceObj.transform.position = new Vector3(piece.position.x, piece.position.y, piece.position.z);
                    pieceObj.transform.rotation = new Quaternion(piece.rotation.x, piece.rotation.y, piece.rotation.z, piece.rotation.w);
                    pieceObj.transform.SetParent(stepObj.transform);

                    // Set the color of the piece
                    MeshRenderer meshRenderer = pieceObj.transform.GetChild(0).GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        Color color = new Color32(
                            byte.Parse(piece.color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                            byte.Parse(piece.color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                            byte.Parse(piece.color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber),
                            255
                        );
                        meshRenderer.material.color = color;
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception when trying to import .json file: " + e.Message);
        }
    }
}
