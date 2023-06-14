using System.IO;
using UnityEngine;
using SimpleFileBrowser;

public class OBJImporter : MonoBehaviour
{
    private string objDirectoryPath; // The path to the directory containing the OBJ files

    public bool activeByDefault = false;

    void Start()
    {
        //OpenFolderBrowser();
    }

    public void OpenFolderBrowser()
    {
        FileBrowser.ShowLoadDialog(
            (string[] paths) => {
                if (paths.Length > 0)
                {
                    objDirectoryPath = paths[0];
                    Debug.Log("Selected folder path: " + objDirectoryPath);

                    // Import the OBJ files from the selected folder
                    ImportOBJFiles(objDirectoryPath);

                    // after obj import, import legos
                    LegoManager legoManager = GameObject.Find("LegoManager").GetComponent<LegoManager>();
                    legoManager.ImportLegos();
                }
            },
            () => {
                Debug.Log("Folder selection cancelled.");
            },
            FileBrowser.PickMode.Folders,
            false,
            null,
            "Select Folder",
            "Select"
        );
    }

    void ImportOBJFiles(string folderPath)
    {
        GameObject parentObjectToCleanup = GameObject.Find("ImportedObjs");
        if (parentObjectToCleanup)
        {
            Destroy(parentObjectToCleanup);
        }
        GameObject parentObject = new GameObject("ImportedObjs");
        parentObject.transform.position = new Vector3(1000000, 0, 1000000); // Hide from view

        string[] objFiles;
        try
        {
            objFiles = System.IO.Directory.GetFiles(folderPath, "*.obj");
            Debug.Log("Found " + objFiles.Length + " .obj files");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception when trying to get .obj files: " + e.Message);
            return;
        }

        foreach (string objFilePath in objFiles)
        {
            string objFileName = System.IO.Path.GetFileNameWithoutExtension(objFilePath);
            Debug.Log("Importing .obj file: " + objFileName);

            try
            {
                OBJImporterScript importerScript = parentObject.AddComponent<OBJImporterScript>();
                importerScript.ImportOBJ(objFilePath, parentObject, activeByDefault);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Exception when trying to import .obj file: " + e.Message);
            }
        }
    }

}
