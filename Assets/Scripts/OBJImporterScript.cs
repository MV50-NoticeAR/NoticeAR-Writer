using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Dummiesman;

public class OBJImporterScript : MonoBehaviour
{
    public void ImportOBJ(string objFilePath, GameObject parentObject, bool activeByDefault)
    {
        OBJLoader objLoader = new OBJLoader();

        // Load the OBJ file
        OBJLoader loader = new OBJLoader();
        GameObject objObject = loader.Load(objFilePath);

        // Assuming the child GameObject that has MeshFilter is the first child
        Transform childTransform = objObject.transform.GetChild(0);

        // Add MeshCollider to the child object
        MeshCollider meshCollider = childTransform.gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = childTransform.gameObject.GetComponent<MeshFilter>().mesh;

        //// Override :Create the parent object
        //GameObject parentObject = new GameObject("ImportedObjs");
        //objObject.transform.parent = parentObject.transform;

        // Set the parent object as the parent of the imported object
        objObject.transform.SetParent(parentObject.transform, false);

        // Optionally, perform additional operations on the imported object, such as scaling or rotation
        objObject.transform.localScale = Vector3.one;
        objObject.transform.rotation = Quaternion.identity;

        // Hide the imported object by default
        objObject.SetActive(activeByDefault);
    }
}

