using System;
using System.Collections.Generic;
using UnityEngine;
using ImGuiNET;
using Unity.Collections;
using System.Runtime.InteropServices;
using UImGui;

public class LegoManager : MonoBehaviour
{
    public struct LegoPiece
    {
        public string Name;
        public string Description;
        public Texture2D PlaceholderImage;
        public long ImageTextureID;
    }

    private List<LegoPiece> legoPieces = new List<LegoPiece>();

    // Property that allows access to legoPieces from outside.
    public IReadOnlyList<LegoPiece> LegoPieces => legoPieces;

    //void Start()
    public void ImportLegos()
    {
        // Clear the existing list
        legoPieces.Clear();

        //GameObject parentObject = GameObject.Find("UsableLegoPieces");
        GameObject parentObject = GameObject.Find("ImportedObjs");
        if (parentObject == null)
        {
            Debug.LogError("No GameObject named ImportedObjs found.");
            return;
        }

        Transform[] allChildren = parentObject.GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child.gameObject == parentObject) // Skip the parent object itself
                continue;

            MeshRenderer meshRenderer = child.gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) // Skip if there is no MeshRenderer
                continue;

            //string name = child.gameObject.name;
            string name = child.parent.name;

            var descriptionComponent = child.gameObject.GetComponent<Description>();
            string description = "None";
            if (descriptionComponent == null)
            {
                //Debug.LogWarning($"No Description component found on {name}");
                //continue;
            }
            else { description = descriptionComponent.Text; }
            //Texture2D image = child.gameObject.GetComponent<SpriteRenderer>().sprite.texture;

            // Generate a preview image
            //Texture2D image = RuntimePreviewGenerator.GenerateModelPreview(child, 128, 128, true);
            //legoPieces.Add(new LegoPiece { Name = name, Description = description, PlaceholderImage = image });

            // Generate a preview image
            Texture2D nonReadableImage = RuntimePreviewGenerator.GenerateModelPreview(child, 128, 128, true);

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

            //legoPieces.Add(new LegoPiece { Name = name, Description = description, PlaceholderImage = readableImage });

            // Register the texture with UImGui
            long imageTextureID = UImGuiUtility.GetTextureId(readableImage).ToInt32();

            legoPieces.Add(new LegoPiece { Name = name, Description = description, PlaceholderImage = readableImage, ImageTextureID = imageTextureID });
        }
    }

    //void OnGUI()
    //{
    //    if (ImGui.Begin("Lego Pieces"))
    //    {
    //        foreach (var piece in legoPieces)
    //        {
    //            ImGui.Text(piece.Name);
    //            ImGui.Text(piece.Description);

    //            // Get raw texture data
    //            NativeArray<byte> texData = piece.PlaceholderImage.GetRawTextureData<byte>();
    //            IntPtr texPtr = Marshal.AllocHGlobal(texData.Length);
    //            Marshal.Copy(texData.ToArray(), 0, texPtr, texData.Length);

    //            // Assuming your TextureFormat is RGBA32
    //            unsafe
    //            {
    //                ImGui.Image((IntPtr)texPtr,
    //                    new UnityEngine.Vector2(piece.PlaceholderImage.width, piece.PlaceholderImage.height),
    //                    UnityEngine.Vector2.zero, UnityEngine.Vector2.one, UnityEngine.Vector4.one);
    //            }

    //            Marshal.FreeHGlobal(texPtr); // Free the memory after you're done using it

    //            ImGui.Separator();
    //        }

    //        ImGui.End();
    //    }
    //}

    void OnDestroy()
    {
        // Free the unmanaged resources
        foreach (var piece in legoPieces)
        {
            NativeArray<byte> texData = piece.PlaceholderImage.GetRawTextureData<byte>();
            IntPtr texPtr = Marshal.AllocHGlobal(texData.Length);
            Marshal.FreeHGlobal(texPtr);

            if (piece.PlaceholderImage != null)
            {
                Destroy(piece.PlaceholderImage);
            }
        }
    }
}
