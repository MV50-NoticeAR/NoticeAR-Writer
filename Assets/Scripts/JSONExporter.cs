using System.IO;
using UnityEngine;
using System.Collections.Generic;
using SimpleFileBrowser;

public class JSONExporter : MonoBehaviour
{
    Texture2D GeneratePreviewImage(Transform child)
    {
        // Generate a preview image
        Texture2D nonReadableImage = RuntimePreviewGenerator.GenerateModelPreview(child, 2048, 2048, true);

        // Create a temporary RenderTexture
        RenderTexture tempRT = RenderTexture.GetTemporary(
            nonReadableImage.width,
            nonReadableImage.height,
            0,
            RenderTextureFormat.ARGB32
        );

        // Blit the pixels on texture to the RenderTexture
        Graphics.Blit(nonReadableImage, tempRT);

        // Backup the currently set RenderTexture
        RenderTexture previous = RenderTexture.active;

        // Set the current RenderTexture to the temporary one we created
        RenderTexture.active = tempRT;

        // Create a new readable Texture2D to copy the pixels to it
        Texture2D readableImage = new Texture2D(nonReadableImage.width, nonReadableImage.height);

        // Copy the pixels from the RenderTexture to the new Texture2D
        readableImage.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);

        // Reset the active RenderTexture
        RenderTexture.active = previous;

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(tempRT);

        readableImage.Apply();

        return readableImage;
    }

    public void OpenFolderBrowser()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON", ".json"));
        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowSaveDialog(
            (string[] paths) => {
                if (paths.Length > 0)
                {
                    string filePath = paths[0];
                    Debug.Log("Destination file path: " + filePath);

                    // Export the data to the selected path
                    ExportData(filePath);
                }
            },
            () => {
                Debug.Log("File selection cancelled.");
            },
            FileBrowser.PickMode.Files,
            false,
            null,
            "Select Destination JSON File",
            "Save"
        );
    }

    void ExportData(string filePath)
    {
        GameObject noticeRoot = GameObject.Find("NoticeRoot");
        if (noticeRoot == null)
        {
            Debug.LogError("Could not find NoticeRoot GameObject for exporting.");
            return;
        }

        // Fetch the updated metadata from the MetadataSchematic component
        SchematicMetadata schematicMetadata = noticeRoot.GetComponent<SchematicMetadata>();
        if (schematicMetadata == null)
        {
            Debug.LogError("NoticeRoot GameObject is missing a SchematicMetadata component.");
            return;
        }

        Schematic schematic = new Schematic
        {
            name = schematicMetadata.name,
            picture = schematicMetadata.picture,
            author = schematicMetadata.author,
            description = schematicMetadata.description,
            version = schematicMetadata.version,
            steps = new List<Step>()
        };

        foreach (Transform stepTransform in noticeRoot.transform)
        {
            StepMetadata stepMetadata = stepTransform.GetComponent<StepMetadata>();
            Step step = new Step
            {
                name = stepTransform.name,
                description = stepMetadata != null ? stepMetadata.Description : "", // Use the description from the StepMetadata component, if it exists
                pieces = new List<Piece>()
            };

            foreach (Transform pieceTransform in stepTransform)
            {
                MeshRenderer meshRenderer = pieceTransform.GetChild(0).GetComponent<MeshRenderer>();

                // Convert color from Unity's format to hexadecimal
                string colorHex = "FFFFFF";
                if (meshRenderer != null)
                {
                    Color32 color32 = meshRenderer.material.color;
                    colorHex = color32.r.ToString("X2") + color32.g.ToString("X2") + color32.b.ToString("X2");
                }

                Piece piece = new Piece
                {
                    model = pieceTransform.name,
                    color = colorHex,
                    position = new Position
                    {
                        x = pieceTransform.position.x,
                        y = pieceTransform.position.y,
                        z = pieceTransform.position.z
                    },
                    rotation = new Rotation
                    {
                        x = pieceTransform.rotation.x,
                        y = pieceTransform.rotation.y,
                        z = pieceTransform.rotation.z,
                        w = pieceTransform.rotation.w
                    }
                };
                step.pieces.Add(piece);
            }
            schematic.steps.Add(step);
        }

        string jsonData = JsonUtility.ToJson(schematic, true);

        try
        {
            File.WriteAllText(filePath, jsonData);
            Debug.Log("Export successful to path: " + filePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Exception when trying to export to .json file: " + e.Message);
        }

        // Generate the preview image
        if (noticeRoot != null)
        {
            Texture2D previewImage = GeneratePreviewImage(noticeRoot.transform);

            // Save the image as a PNG file
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string imageFilePath = Path.Combine(directory, fileName + ".png");
            File.WriteAllBytes(imageFilePath, previewImage.EncodeToPNG());
            Debug.Log("Preview image saved to path: " + imageFilePath);
        }
    }
}
