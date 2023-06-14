using ImGuiNET;
#if !UIMGUI_REMOVE_IMNODES
using imnodesNET;
#endif
#if !UIMGUI_REMOVE_IMPLOT
using ImPlotNET;
using System.Linq;
using static LegoManager;
using System.Runtime.InteropServices;
using System;
using Unity.Collections;
using System.Collections.Generic;
using System.Collections;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
#endif
#if !UIMGUI_REMOVE_IMGUIZMO
using ImGuizmoNET;
#endif
using UnityEngine;

namespace UImGui
{
    public class ShowDemoWindow : MonoBehaviour
    {
        public CameraController cameraController;
        private List<Texture2D> _texturesToDestroy = new List<Texture2D>();

        GameObject selectedParent = null;  // The currently selected parent GameObject
        GameObject selectedChild = null;  // The currently selected child GameObject

        int selectedPieceIndex = -1; // Index of currently selected piece within a step
        int selectedPieceStepIndex = -1; // Index of the step containing the selected piece

        string newStepName = "";  // For input text field when creating a new step
        Transform selectedPiece = null; // Currently selected piece for moving
        private GameObject selectedPieceFromDrawer = null;

        bool notScrolled = false;
        bool isMouseClickInImGuiWindow = false;

        bool useLocalCoordinates = true;

        GameObject noticeRoot;
        private SchematicMetadata schematicMetadata;

        GameObject destinationStep;

        bool[] isOpenSteps;
        bool isPopupOpen = false;  // A single boolean to keep track of whether the popup is open
        int openPopupIndex = -1;  // The index of the step whose popup is open (-1 indicates no popup)

        // Allow user to specify whether to place the pieces at the start or end
        string[] placementOptions = new string[] { "Start", "End" };
        int currentItem = 0;  // Select "Start" by default

        // Create a list to store selected pieces
        List<Transform> selectedPieces = new List<Transform>();

        List<int> openIndices = new List<int>();

        bool shiftWasUsed = false;

#if !UIMGUI_REMOVE_IMPLOT
        [SerializeField]
        float[] _barValues = Enumerable.Range(1, 10).Select(x => (x * x) * 1.0f).ToArray();
        [SerializeField]
        float[] _xValues = Enumerable.Range(1, 10).Select(x => (x * x) * 1.0f).ToArray();
        [SerializeField]
        float[] _yValues = Enumerable.Range(1, 10).Select(x => (x * x) * 1.0f).ToArray();
#endif

        void Start()
        {

        }

        private Bounds RotateBounds(Transform transform, Bounds bounds)
        {
            var rotatedExtents = transform.rotation * bounds.extents;
            var absRotatedExtents = new Vector3(Mathf.Abs(rotatedExtents.x), Mathf.Abs(rotatedExtents.y), Mathf.Abs(rotatedExtents.z));
            return new Bounds(transform.position, absRotatedExtents * 2);
        }

        void MovePieces(Transform step, GameObject destinationStep, int placementOption)
        {
            for (int pieceIndex = 0; pieceIndex < step.childCount; pieceIndex++)
            {
                Transform piece = step.GetChild(pieceIndex);
                piece.SetParent(destinationStep.transform);

                // Depending on the selected placement option, set at the beginning or the end of the new step
                if (placementOption == 0)
                {
                    piece.SetSiblingIndex(0);  // Move to start of new step
                }
                else
                {
                    piece.SetSiblingIndex(destinationStep.transform.childCount - 1);  // Move to end of new step
                }
            }
        }

        private void OnEnable()
        {
            UImGuiUtility.Layout += OnLayout;
        }

        private void OnDisable()
        {
            UImGuiUtility.Layout -= OnLayout;
        }

        private void OnLayout(UImGui uImGui)
        {
            // Fullscreen viewport
            ImGuiViewportPtr viewport = ImGui.GetMainViewport();
            ImGui.SetNextWindowPos(viewport.Pos);
            ImGui.SetNextWindowSize(viewport.Size);
            ImGui.SetNextWindowViewport(viewport.ID);

            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0, 0, 0, 0)); // Transparent background

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDocking;
            window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
            window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

            // Pass ImGuiDockNodeFlags.PassThruCentralNode to allow input through the dockspace
            ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.PassthruCentralNode;

            bool open = true;
            ImGui.Begin("DockSpace Demo", ref open, window_flags);
            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(1);

            // Dockspace
            uint dockspaceId = ImGui.GetID("MyDockSpace");
            ImGui.DockSpace(dockspaceId, new Vector2(0, 0), dockspace_flags);

            //#if !UIMGUI_REMOVE_IMPLOT
            //            if (ImGui.Begin("Plot Window Sample"))
            //            {
            //                ImGui.SetNextWindowSize(Vector2.one * 200, ImGuiCond.Once);
            //                ImPlot.BeginPlot("Plot test");
            //                ImPlot.PlotBars("My Bar Plot", ref _barValues[0], _barValues.Length + 1);
            //                ImPlot.PlotLine("My Line Plot", ref _xValues[0], ref _yValues[0], _xValues.Length, 0, 0);
            //                ImPlot.EndPlot();

            //                ImGui.End();
            //            }
            //#endif

            //#if !UIMGUI_REMOVE_IMNODES
            //            if (ImGui.Begin("Nodes Window Sample"))
            //            {
            //                ImGui.SetNextWindowSize(Vector2.one * 300, ImGuiCond.Once);
            //                imnodes.BeginNodeEditor();
            //                imnodes.BeginNode(1);

            //                imnodes.BeginNodeTitleBar();
            //                ImGui.TextUnformatted("simple node :)");
            //                imnodes.EndNodeTitleBar();

            //                imnodes.BeginInputAttribute(2);
            //                ImGui.Text("input");
            //                imnodes.EndInputAttribute();

            //                imnodes.BeginOutputAttribute(3);
            //                ImGui.Indent(40);
            //                ImGui.Text("output");
            //                imnodes.EndOutputAttribute();

            //                imnodes.EndNode();
            //                imnodes.EndNodeEditor();
            //                ImGui.End();
            //            }
            //#endif

            //ImGui.ShowDemoWindow();

            // Get the LegoManager instance from the "LegoManager" GameObject
            LegoManager legoManager = GameObject.Find("LegoManager").GetComponent<LegoManager>();

            Texture2D rgba32Texture;

            ImGui.SetNextWindowDockID(dockspaceId, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(new Vector2(viewport.Size.x / 5, viewport.Size.y));


            if (ImGui.Begin("Lego Pieces"))
            {
                foreach (var piece in legoManager.LegoPieces)
                {
                    //ImGui.Text(piece.Name);
                    // Find the GameObject associated with this Lego piece
                    Transform pieceTransform = GameObject.Find("ImportedObjs").transform.Find(piece.Name);

                    if (pieceTransform == null)
                    {
                        Debug.LogError("Could not find a GameObject named " + piece.Name);
                        continue;
                    }

                    // Allow the user to select a piece
                    if (ImGui.Selectable(piece.Name, selectedPieceFromDrawer == pieceTransform.gameObject))
                    {
                        selectedPieceFromDrawer = pieceTransform.gameObject;
                    }

                    if (ImGui.BeginPopupContextItem("contextMenuPiece" + piece.Name))
                    {
                        if (selectedPieceStepIndex >= 0 && ImGui.MenuItem("Add to Current Piece Step"))
                        {
                            // Find the selected step
                            Transform selectedStep = noticeRoot.transform.GetChild(selectedPieceStepIndex);

                            // Instantiate a new piece
                            GameObject newPiece = Instantiate(pieceTransform.gameObject);
                            newPiece.name = piece.Name; // prevent the addition of (clone) made by Instantiate
                            newPiece.transform.SetParent(selectedStep);

                            // Set it to be the last child in the hierarchy
                            newPiece.transform.SetAsLastSibling();

                            // If there's a piece in this step, set its position to be just above the last piece
                            if (selectedStep.childCount > 1)
                            {
                                Transform lastPiece = selectedStep.GetChild(selectedStep.childCount - 2);
                                Bounds newPieceBounds = newPiece.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                                newPieceBounds = RotateBounds(newPiece.transform, newPieceBounds);
                                Vector3 newPosition = lastPiece.position + Vector3.up * newPieceBounds.size.y;
                                newPiece.transform.position = newPosition;
                            }
                        }

                        if (ImGui.BeginMenu("Add to Step"))
                        {
                            for (int i = 0; i < noticeRoot.transform.childCount; i++)
                            {
                                Transform step = noticeRoot.transform.GetChild(i);
                                if (ImGui.MenuItem(step.name))
                                {
                                    // Instantiate a new piece
                                    GameObject newPiece = Instantiate(pieceTransform.gameObject);
                                    newPiece.name = piece.Name; // prevent the addition of (clone) made by Instantiate
                                    newPiece.transform.SetParent(step);

                                    // Set it to be the last child in the hierarchy
                                    newPiece.transform.SetAsLastSibling();

                                    // If there's a piece in this step, set its position to be just above the last piece
                                    if (step.childCount > 1)
                                    {
                                        Transform lastPiece = step.GetChild(step.childCount - 2);
                                        Bounds newPieceBounds = newPiece.GetComponentInChildren<MeshFilter>().sharedMesh.bounds;
                                        newPieceBounds = RotateBounds(newPiece.transform, newPieceBounds);
                                        Vector3 newPosition = lastPiece.position + Vector3.up * newPieceBounds.size.y;
                                        newPiece.transform.position = newPosition;
                                    }
                                }
                            }
                            ImGui.EndMenu();
                        }

                        if (selectedPieceFromDrawer != null && ImGui.MenuItem("Add Above Piece"))
                        {
                            // Instantiate a new piece
                            GameObject newPiece = Instantiate(pieceTransform.gameObject);
                            newPiece.name = piece.Name;
                            newPiece.transform.SetParent(selectedPiece.transform.parent);

                            // Get the mesh filter component and calculate the height
                            MeshFilter meshFilter = newPiece.GetComponentInChildren<MeshFilter>();
                            Bounds newPieceBounds = meshFilter.sharedMesh.bounds;
                            newPieceBounds = RotateBounds(newPiece.transform, newPieceBounds);

                            // Set it to be the next sibling of the selected piece
                            newPiece.transform.SetSiblingIndex(selectedPiece.GetSiblingIndex() + 1);

                            // Set its position to be just above the selected piece
                            newPiece.transform.position = selectedPiece.transform.position + Vector3.up * newPieceBounds.size.y;
                        }

                        ImGui.EndPopup();
                    }

                    //ImGui.TextWrapped
                    //ImGui.Text(piece.Description);

                    // Get raw texture data
                    NativeArray<byte> texData = piece.PlaceholderImage.GetRawTextureData<byte>();
                    IntPtr texPtr = Marshal.AllocHGlobal(texData.Length);
                    Marshal.Copy(texData.ToArray(), 0, texPtr, texData.Length);

                    // Create a new Texture2D in RGBA32 format with the same width and height as the original
                    rgba32Texture = new Texture2D(piece.PlaceholderImage.width, piece.PlaceholderImage.height, TextureFormat.RGBA32, false);
                    // Copy the pixel data from the original texture
                    rgba32Texture.SetPixels(piece.PlaceholderImage.GetPixels());
                    // Apply the changes
                    rgba32Texture.Apply();

                    IntPtr imageTextureID = UImGuiUtility.GetTextureId(rgba32Texture);

                    // Assuming the TextureFormat is RGBA32
                    unsafe
                    {
                        ImGui.Image(imageTextureID, new Vector2(piece.PlaceholderImage.width, piece.PlaceholderImage.height));
                    }

                    // Free the memory after we're done with it
                    Marshal.FreeHGlobal(texPtr);

                    // Add the texture to a list so that we can free the texture when done
                    _texturesToDestroy.Add(rgba32Texture);

                    ImGui.Separator();
                }

                ImGui.End();
            }

            ImGui.End(); // End dockspace

            ImGui.Begin("Navigation Controls");
            {
                ImGui.Text("Target Position");
                ImGui.InputFloat3("##target", ref cameraController.target);

                ImGui.Text("Pan Speed");
                ImGui.InputFloat("##panSpeed", ref cameraController.panSpeed);

                ImGui.Text("Zoom Speed");
                ImGui.InputFloat("##zoomSpeed", ref cameraController.zoomSpeed);

                ImGui.Text("Rotation Speed");
                ImGui.InputFloat("##rotationSpeed", ref cameraController.rotationSpeed);

                ImGui.Text("Fly Speed");
                ImGui.InputFloat("##flySpeed", ref cameraController.flySpeed);

                ImGui.Text("Fly Mode");
                ImGui.Checkbox("##flyMode", ref cameraController.flyMode);

                ImGui.End();
            }

            OBJImporter oBJImporter = GameObject.Find("RuntimeObjImporter").GetComponent<OBJImporter>();
            JSONImporter jSONImporter = GameObject.Find("RuntimeJsonImporter").GetComponent<JSONImporter>();
            JSONExporter jSONExporter = GameObject.Find("RuntimeJsonExporter").GetComponent<JSONExporter>();

            ImGui.Begin("Notice Writer App");
            {
                ImGui.TextWrapped("Welcome to the notice writer app !");

                ImGui.TextWrapped("You can navigate using Blender like controls or fly through");
                ImGui.TextWrapped("- Blender like controls :");
                ImGui.TextWrapped("  - Middle mouse : rotate around an object");
                ImGui.TextWrapped("  - Middle mouse + shift : pan");
                ImGui.TextWrapped("  - Middle mouse + ctrl OR scroll : zoom");
                ImGui.TextWrapped("  - Middle mouse + left alt : choose a new target position to rotate around");
                ImGui.TextWrapped("- Fly through controls :");
                ImGui.TextWrapped("  - ZQSD or arrow keys : move left/right and forwards/backwards");
                ImGui.TextWrapped("  - A and E : move up and down");
                ImGui.TextWrapped("  - Left click : Look around");
                ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().x);
                ImGui.TextColored(new Vector4(255, 0, 0, 1), "Note: You can switch between the two mode with / or the checkbox");
                ImGui.PopTextWrapPos();

                ImGui.TextWrapped("To start you can import obj files :");
                if (ImGui.Button("Import objs"))
                {
                    //Debug.Log("Clickity Click!");
                    oBJImporter.OpenFolderBrowser();
                }
                ImGui.TextWrapped("and their associated json if it exists:");
                if (ImGui.Button("Import json notice"))
                {
                    //Debug.Log("Clickity Click!");
                    jSONImporter.OpenFolderBrowser();
                }
                ImGui.PushTextWrapPos(ImGui.GetContentRegionMax().x);
                ImGui.TextColored(new Vector4(255, 0, 0, 1), "Don't forget to import the necessary obj files before importing the json otherwise there would be missing models !");
                ImGui.TextColored(new Vector4(255, 0, 0, 1), "Warning ! : Importing will overwrite existing data !");
                ImGui.PopTextWrapPos();
                if (ImGui.Button("Export json notice"))
                {
                    //Debug.Log("Clickity Click!");
                    jSONExporter.OpenFolderBrowser();
                }
                ImGui.End();
            }

            //Dictionary<int, GameObject> gameObjects = new Dictionary<int, GameObject>();

            //GameObject noticeRoot = GameObject.Find("NoticeRoot");
            //IntPtr instanceIDPtr = IntPtr.Zero;  // Initialize instanceIDPtr

            //ImGui.Begin("Hierarchy");
            //{
            //    foreach (Transform step in noticeRoot.transform)
            //    {
            //        if (ImGui.TreeNode(step.name))
            //        {
            //            foreach (Transform piece in step.transform)
            //            {
            //                ImGui.Selectable(piece.name);

            //                // If this piece is active and the user starts dragging it...
            //                if (ImGui.BeginDragDropSource())
            //                {
            //                    // Convert the instance ID to bytes and store it in an IntPtr
            //                    int instanceID = piece.gameObject.GetInstanceID();
            //                    byte[] instanceIDBytes = BitConverter.GetBytes(instanceID);
            //                    instanceIDPtr = Marshal.AllocHGlobal(instanceIDBytes.Length);
            //                    Marshal.Copy(instanceIDBytes, 0, instanceIDPtr, instanceIDBytes.Length);

            //                    // Store the piece in the dictionary using its instance ID as the key
            //                    gameObjects[instanceID] = piece.gameObject;

            //                    ImGui.SetDragDropPayload("DRAGGED_PIECE", instanceIDPtr, (uint)instanceIDBytes.Length, ImGuiCond.Once);
            //                    ImGui.Text(piece.name);
            //                    ImGui.EndDragDropSource();
            //                }

            //                // If this piece is an active drag and drop target...
            //                if (ImGui.BeginDragDropTarget())
            //                {
            //                    ImGuiPayloadPtr payload = ImGui.AcceptDragDropPayload("DRAGGED_PIECE");
            //                    if (payload.IsDataType("SOME_OTHER_TYPE"))
            //                    {
            //                        IntPtr receivedInstanceIDPtr = payload.Data;
            //                        if (receivedInstanceIDPtr != IntPtr.Zero)  // Check if payload.Data is valid
            //                        {
            //                            // Get the instance ID from the IntPtr and convert it back to an int
            //                            int receivedInstanceID = Marshal.ReadInt32(receivedInstanceIDPtr);

            //                            // Look up the GameObject in the dictionary using the instance ID
            //                            if (gameObjects.TryGetValue(receivedInstanceID, out GameObject draggedPiece))
            //                            {
            //                                // Change the parent of draggedPiece
            //                                draggedPiece.transform.SetParent(step);
            //                            }
            //                        }
            //                    }

            //                    ImGui.EndDragDropTarget();
            //                }
            //            }
            //            ImGui.TreePop();
            //        }
            //    }
            //    ImGui.End();
            //    if (instanceIDPtr != IntPtr.Zero)
            //        Marshal.FreeHGlobal(instanceIDPtr);  // Free the memory here
            //}

            ImGui.Begin("Hierarchy");
            {
                if (ImGui.Button("Create New Step"))
                {
                    GameObject newStep = new GameObject();
                    newStep.transform.SetParent(noticeRoot.transform);
                }

                for (int stepIndex = 0; stepIndex < noticeRoot.transform.childCount; stepIndex++)
                {
                    Transform step = noticeRoot.transform.GetChild(stepIndex);
                    string stepName = stepIndex + " - " + step.name;

                    // Whenever a piece is selected
                    if (notScrolled)
                    {
                        Debug.Log("Selected: " + selectedPiece.name);
                        if (!openIndices.Contains(stepIndex))
                        {
                            openIndices.Add(stepIndex);
                        }
                    }

                    if (openIndices.Contains(stepIndex))
                    {
                        ImGui.SetNextItemOpen(true);
                    }

                    if (ImGui.TreeNode(stepName))
                    {
                        StepMetadata metadata = step.GetComponent<StepMetadata>();
                        if (metadata == null)
                        {
                            metadata = step.gameObject.AddComponent<StepMetadata>();
                        }

                        string description = metadata.Description ?? "";
                        if (ImGui.InputText("Description", ref description, 100))
                        {
                            metadata.Description = description;
                        }

                        if (ImGui.Button("Move Step Up##" + step.name) && stepIndex > 0)
                        {
                            // Swap this step with the previous one
                            step.SetSiblingIndex(stepIndex - 1);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Move Step Down##" + step.name) && stepIndex < noticeRoot.transform.childCount - 1)
                        {
                            // Swap this step with the next one
                            step.SetSiblingIndex(stepIndex + 1);
                        }

                        // Change the step name to input when Rename Step button is pressed
                        if (ImGui.Button("Rename Step##" + step.name) && !string.IsNullOrEmpty(newStepName))
                        {
                            step.name = newStepName;
                            newStepName = "";  // Clear input field after renaming
                        }
                        ImGui.SameLine();
                        ImGui.InputText("##NewStepName", ref newStepName, 100);

                        if (ImGui.Button("Delete Step##" + step.name))
                        {
                            isPopupOpen = true;
                            openPopupIndex = stepIndex;  // Set the index of the step to open the popup for
                            ImGui.OpenPopup("Delete Confirmation##" + step.name);
                        }

                        // Delete Confirmation popup
                        //if (ImGui.BeginPopupModal("Delete Confirmation##" + step.name, ref isOpenSteps[stepIndex], ImGuiWindowFlags.AlwaysAutoResize))
                        //{
                        if (isPopupOpen && openPopupIndex == stepIndex && ImGui.BeginPopupModal("Delete Confirmation##" + step.name, ref isPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
                        {
                            ImGui.Text("Are you sure you want to delete this step?\nThis operation cannot be undone!\n\n");
                            ImGui.Separator();

                            //// Allow user to select a destination step
                            //for (int sIndex = 0; sIndex < noticeRoot.transform.childCount; sIndex++)
                            //{
                            //    Transform s = noticeRoot.transform.GetChild(sIndex);
                            //    if(s == step.gameObject) { continue; }
                            //    if (ImGui.Selectable(s.name))
                            //    {
                            //        // Save selected destination step
                            //        destinationStep = s.GameObject();
                            //    }
                            //}

                            //bool comboJustUsed = ImGui.Combo("Placement", ref currentItem, placementOptions, 2);
                            bool comboJustUsed = false;
                            if (!comboJustUsed)
                            {
                                //if (ImGui.Button("OK", new Vector2(120, 0)))
                                //{
                                //    // Check if a destination step is selected
                                //    if (destinationStep != null)
                                //    {
                                //        // Move all child pieces to the selected destination step before deleting
                                //        for (int pieceIndex = 0; pieceIndex < step.childCount; pieceIndex++)
                                //        {
                                //            Transform piece = step.GetChild(pieceIndex);
                                //            piece.SetParent(destinationStep.transform);

                                //            // Depending on the selected placement option, set at the beginning or the end of the new step
                                //            if (currentItem == 0)
                                //            {
                                //                piece.SetSiblingIndex(0);  // Move to start of new step
                                //            }
                                //            else
                                //            {
                                //                piece.SetSiblingIndex(destinationStep.transform.childCount - 1);  // Move to end of new step
                                //            }
                                //        }
                                //    }

                                //    // Delete selected step
                                //    Destroy(step.gameObject);
                                //    ImGui.CloseCurrentPopup();
                                //}
                                //if (ImGui.Button("Confirm Move", new Vector2(120, 0)))
                                //{
                                //    // Check if a destination step is selected
                                //    if (destinationStep != null)
                                //    {
                                //        // Move all child pieces to the selected destination step
                                //        MovePieces(step, destinationStep, currentItem);
                                //    }

                                //    ImGui.CloseCurrentPopup();
                                //}

                                if (ImGui.Button("Confirm Deletion", new Vector2(120, 0)))
                                {
                                    // Delete selected step
                                    Destroy(step.gameObject);
                                    ImGui.CloseCurrentPopup();
                                }

                                ImGui.SetItemDefaultFocus();
                                ImGui.SameLine();
                                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                                {
                                    ImGui.CloseCurrentPopup();
                                }
                                ImGui.EndPopup();
                            }
                        }

                        for (int pieceIndex = 0; pieceIndex < step.childCount; pieceIndex++)
                        {
                            Transform piece = step.GetChild(pieceIndex);
                            string pieceName = pieceIndex + ". " + piece.name;
                            //if (ImGui.Selectable(pieceName, selectedPiece == piece))
                            //{
                            //    // Select this piece when clicked
                            //    selectedPiece = piece;
                            //    selectedPieceIndex = pieceIndex;
                            //    selectedPieceStepIndex = stepIndex;
                            //}

                            if (ImGui.Selectable(pieceName, selectedPieces.Contains(piece)))
                            {
                                // Get key state
                                bool isCtrlDown = ImGui.GetIO().KeyCtrl;
                                bool isShiftDown = ImGui.GetIO().KeyShift;
                                bool wasPieceSelected = false;
                                shiftWasUsed = false;

                                // If Shift is held down, select all pieces between the last and the new selected
                                if (isShiftDown && selectedPieces.Count > 0)
                                {
                                    // Get the indices of the last and the new selected pieces
                                    Transform lastSelectedPiece = selectedPieces[selectedPieces.Count - 1];
                                    int lastIndex = lastSelectedPiece.GetSiblingIndex();
                                    int lastStepIndex = lastSelectedPiece.parent.GetSiblingIndex();

                                    int newIndex = piece.GetSiblingIndex();
                                    int newStepIndex = piece.parent.GetSiblingIndex();
                                    
                                    // Check if the last and new selected pieces are in the same step
                                    if (lastStepIndex == newStepIndex)
                                    {
                                        // If the last and new selected pieces are in the same step, select all pieces between them
                                        for (int i = Mathf.Min(lastIndex, newIndex); i <= Mathf.Max(lastIndex, newIndex); i++)
                                        {
                                            Transform pieceToSelect = noticeRoot.transform.GetChild(lastStepIndex).GetChild(i);
                                            if (!selectedPieces.Contains(pieceToSelect))  // check if it's not already selected
                                            {
                                                selectedPieces.Add(pieceToSelect);
                                                wasPieceSelected = true;
                                            }
                                        }
                                        selectedPiece = piece;
                                        shiftWasUsed = true;
                                    }
                                }

                                // If Ctrl is not held down and no piece was selected with Shift, clear the current selection
                                if (!isCtrlDown && !wasPieceSelected)
                                {
                                    selectedPieces.Clear();
                                }

                                // Add the clicked piece to the selection if it's not already there
                                if (!selectedPieces.Contains(piece))
                                {
                                    selectedPieces.Add(piece);
                                    wasPieceSelected = true;
                                }

                                // If a piece was selected, output a message
                                if (wasPieceSelected)
                                {
                                    Debug.Log("Selected: " + piece.name);
                                }
                            }

                            if (notScrolled && selectedPiece == piece)
                            {
                                // Scroll to selected item
                                ImGui.SetScrollHereY();
                                notScrolled = false;
                            }

                            // Context menu for moving selected pieces (multiples)
                            if (ImGui.BeginPopupContextItem("contextMenu" + pieceName))
                            {
                                if (ImGui.BeginMenu("Move here"))
                                {
                                    for (int moveStepIndex = 0; moveStepIndex < noticeRoot.transform.childCount; moveStepIndex++)
                                    {
                                        string moveStepName = $"{moveStepIndex} - {noticeRoot.transform.GetChild(moveStepIndex).name}";

                                        if (ImGui.BeginMenu(moveStepName))
                                        {
                                            for (int movePieceIndex = 0; movePieceIndex < noticeRoot.transform.GetChild(moveStepIndex).childCount; movePieceIndex++)
                                            {
                                                GameObject targetPiece = noticeRoot.transform.GetChild(moveStepIndex).GetChild(movePieceIndex).gameObject;
                                                string targetPieceName = $"{movePieceIndex}. {targetPiece.name}";

                                                // Check if any selected pieces match the target
                                                bool isSelected = selectedPieces.Any(p => p.gameObject == targetPiece);

                                                if (ImGui.MenuItem(targetPieceName, "", false, !isSelected))
                                                {
                                                    foreach (Transform selectedPiece in selectedPieces)
                                                    {
                                                        // If the target piece is not the selected piece itself
                                                        if (targetPiece != selectedPiece.gameObject)
                                                        {
                                                            // Move the selected piece to a new step if it's not already in the same step
                                                            if (selectedPiece.transform.parent != targetPiece.transform.parent)
                                                            {
                                                                selectedPiece.transform.SetParent(targetPiece.transform.parent);
                                                            }
                                                            // Place the selected piece below the target piece
                                                            selectedPiece.transform.SetSiblingIndex(movePieceIndex + 1);
                                                        }
                                                    }
                                                }
                                            }
                                            ImGui.EndMenu();
                                        }
                                    }
                                    ImGui.EndMenu();
                                }
                                if (ImGui.BeginMenu("Move to Step"))
                                {
                                    for (int sIndex = 0; sIndex < noticeRoot.transform.childCount; sIndex++)
                                    {
                                        Transform s = noticeRoot.transform.GetChild(sIndex);
                                        if (ImGui.MenuItem(s.name))
                                        {
                                            // Move all selected pieces to this step
                                            foreach (Transform selectedPiece in selectedPieces)
                                            {
                                                selectedPiece.SetParent(s);
                                                selectedPiece.SetSiblingIndex(s.childCount - 1);  // Move to end of new step
                                            }
                                            selectedPieces.Clear();  // Deselect after moving
                                        }
                                    }
                                    ImGui.EndMenu();
                                }
                                ImGui.EndPopup();
                            }
                        }
                        ImGui.TreePop();
                    }
                }
                openIndices.Clear();
            }
            ImGui.End();

            // Separate window for modifying the selected GameObject
            if (selectedPiece != null)
            {
                ImGui.Begin("Selected Piece");
                {
                    Vector3 position = selectedPiece.position;
                    Vector3 rotation = selectedPiece.rotation.eulerAngles;
                    Color color = Color.white;
                    MeshRenderer meshRenderer = selectedPiece.GetChild(0).GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        color = meshRenderer.material.color;
                    }

                    Vector3 positionArr = new Vector3(position.x, position.y, position.z);
                    Vector3 rotationArr = new Vector3(rotation.x, rotation.y, rotation.z);
                    Vector4 colorArr = new Vector4(color.r, color.g, color.b, color.a);

                    ImGui.Checkbox("Use Local Coordinates", ref useLocalCoordinates); // Checkbox for choosing local or global coordinates

                    if (useLocalCoordinates)
                    {
                        if (ImGui.DragFloat3("Position (Local)", ref positionArr, 0.1f))
                        {
                            selectedPiece.localPosition = new Vector3(positionArr.x, positionArr.y, positionArr.z);
                        }

                        if (ImGui.DragFloat3("Rotation (Local)", ref rotationArr, 0.1f))
                        {
                            selectedPiece.localRotation = Quaternion.Euler(rotationArr.x, rotationArr.y, rotationArr.z);
                        }
                    }
                    else
                    {
                        if (ImGui.DragFloat3("Position (Global)", ref positionArr, 0.1f))
                        {
                            selectedPiece.position = new Vector3(positionArr.x, positionArr.y, positionArr.z);
                        }

                        if (ImGui.DragFloat3("Rotation (Global)", ref rotationArr, 0.1f))
                        {
                            selectedPiece.rotation = Quaternion.Euler(rotationArr.x, rotationArr.y, rotationArr.z);
                        }
                    }

                    if (ImGui.ColorEdit4("Color", ref colorArr))
                    {
                        if (meshRenderer != null)
                        {
                            meshRenderer.material.color = new Color(colorArr.x, colorArr.y, colorArr.z, colorArr.w);
                        }
                    }

                    // Delete Piece button
                    if (ImGui.Button("Delete Piece"))
                    {
                        ImGui.OpenPopup("Delete Confirmation");
                    }

                    bool isOpen = true;
                    // Delete Confirmation popup
                    if (ImGui.BeginPopupModal("Delete Confirmation", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
                    {
                        ImGui.Text("Are you sure you want to delete this piece?\nThis operation cannot be undone!\n\n");
                        ImGui.Separator();

                        if (ImGui.Button("OK", new Vector2(120, 0)))
                        {
                            // delete selected piece
                            Destroy(selectedPiece.gameObject);
                            selectedPiece = null;

                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.SetItemDefaultFocus();
                        ImGui.SameLine();
                        if (ImGui.Button("Cancel", new Vector2(120, 0)))
                        {
                            ImGui.CloseCurrentPopup();
                        }
                        ImGui.EndPopup();
                    }
                }
                ImGui.End();
            }

            //ImGui.Begin("Hierarchy Tools");
            //{
            //    // Button to assign the selected child GameObject to the selected parent GameObject
            //    if (ImGui.Button("Assign Selected Child to Selected Parent"))
            //    {
            //        if (selectedParent != null && selectedChild != null)
            //        {
            //            selectedChild.transform.SetParent(selectedParent.transform);
            //        }
            //    }
            //    ImGui.End();
            //}

            //ImGui.Begin("Hierarchy Parameters");
            //{
            //    ImGui.End();
            //}

            // Check if the SchematicMetadata component already exists
            schematicMetadata = noticeRoot.GetComponent<SchematicMetadata>();
            if (schematicMetadata == null)
            {
                // Add the SchematicMetadata component if it doesn't exist
                schematicMetadata = noticeRoot.AddComponent<SchematicMetadata>();
            }

            // Add a new window for updating the schematic metadata
            ImGui.Begin("Schematic Metadata");
            {
                string name = schematicMetadata.name ?? "";
                string picture = schematicMetadata.picture ?? "";
                string author = schematicMetadata.author ?? "";
                string description = schematicMetadata.description ?? "";
                string version = schematicMetadata.version;
                ImGui.InputText("Name", ref name, 100);
                //ImGui.InputText("Picture", ref picture, 100);
                ImGui.InputText("Author", ref author, 100);
                ImGui.InputText("Description", ref description, 100);
                ImGui.InputText("Version", ref version, 100);
                schematicMetadata.name = name;
                schematicMetadata.picture = picture;
                schematicMetadata.author = author;
                schematicMetadata.description = description;
                schematicMetadata.version = version;
            }
            ImGui.End();

            if (selectedPieces.Count > 0 && !shiftWasUsed)
            {
                selectedPiece = selectedPieces[selectedPieces.Count - 1];
            }
        }

        // Run this function at the end of the frame, after the UI has been drawn otherwise it will delete them too soon
        private IEnumerator CleanupTextures()
        {
            // Wait until end of frame
            yield return new WaitForEndOfFrame();

            // Now destroy all textures
            foreach (var texture in _texturesToDestroy)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            _texturesToDestroy.Clear();
        }

        // Call this in your update loop
        private void Update()
        {
            StartCoroutine(CleanupTextures());

            noticeRoot = GameObject.Find("NoticeRoot");

            // Ensure the isOpen array is always the same size as the number of steps
            if (isOpenSteps == null || isOpenSteps.Length != noticeRoot.transform.childCount)
            {
                isOpenSteps = new bool[noticeRoot.transform.childCount];
            }

            // Check for left mouse button click
            if (Input.GetMouseButtonDown(0))
            {
                // Check if the mouse click was inside an ImGui window
                if (ImGui.GetIO().WantCaptureMouse)
                {
                    isMouseClickInImGuiWindow = true;
                }
                else
                {
                    // Create a ray from the camera to the mouse position
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    // Perform the raycast
                    //if (Physics.Raycast(ray, out RaycastHit hit))
                    //{
                    RaycastHit[] hits = Physics.RaycastAll(ray);
                    if (hits.Length > 0)
                    {
                        RaycastHit closestHit = hits[0];
                        for (int i = 1; i < hits.Length; i++)
                        {
                            if (hits[i].distance < closestHit.distance)
                            {
                                closestHit = hits[i];
                            }
                        }

                        // If a GameObject was hit by the ray, check if it's a part of a piece
                        Transform hitTransform = closestHit.transform;

                        // If the hit GameObject is a grandchild of a step (and hence a 3D part of a piece)
                        if (hitTransform.parent != null && hitTransform.parent.parent != null && hitTransform.parent.parent.parent == noticeRoot.transform)
                        {
                            selectedPiece = hitTransform.parent;

                            // Also update selectedPieceIndex and selectedPieceStepIndex
                            selectedPieceIndex = selectedPiece.GetSiblingIndex();
                            selectedPieceStepIndex = selectedPiece.parent.GetSiblingIndex();

                            // Check if Ctrl is held down
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                // If hitPiece is already selected, remove it from the selection
                                if (selectedPieces.Contains(selectedPiece))
                                {
                                    selectedPieces.Remove(selectedPiece);
                                }
                                // Otherwise, add hitPiece to the selection
                                else
                                {
                                    selectedPieces.Add(selectedPiece);
                                }
                            }
                            else
                            {
                                // Clear the selection and add hitPiece
                                selectedPieces.Clear();
                                selectedPieces.Add(selectedPiece);
                            }

                            Debug.Log("Selected: " + selectedPiece.name);
                            notScrolled = true;
                        }
                    }
                }

            }

            if (selectedPieces.Count > 0 && !shiftWasUsed)
            {
                selectedPiece = selectedPieces[selectedPieces.Count - 1];
            }

            //// Check for right mouse button click to open context menu
            //if (Input.GetMouseButtonDown(1))
            //{
            //    if (selectedPiece != null)
            //    {
            //        // Open context menu for selected piece
            //        ImGui.OpenPopup("contextMenu" + selectedPiece.name);
            //    }
            //}
        }

    }
}
