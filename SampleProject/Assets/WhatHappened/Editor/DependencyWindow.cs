// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WhatHappened {
    public class DependencyWindow : EditorWindow {

        private static DependencyWindow window;
        private DependencyAnalyzer depAnalyzer;
        private GitAnalyzer gitAnalyzer;

        // Data structures for holding all created nodes, and all files represented in tree
        private Dictionary<int, LinkedList<CustomTypeNode>> allNodes;
        private HashSet<CustomFile> allFiles;
        private int maxNodesPerLevel;

        // Names of all types, and reference to current/prev selected type index
        private string[] typeNames;
        private int typeIndex, prevTypeIndex;

        // Names for all commit messages for Git repo, and reference to current/prev selected commit index
        private string[] commitNames;
        private int commitIndex, prevCommitIndex;

        // Node selected by the user
        private CustomTypeNode selectedNode, tempSelectedNode;

        // Styles used for general appearance of UI elements
        private GUIStyle style, popupStyle, labelStyle;
        private GUISkin skin;
        private bool isStyleInit;
        private bool isFirstFrameDrawn = true;

        // Split views for partitioning window into 3 parts
        private EditorGUISplitView horizontalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
        private EditorGUISplitView verticalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Vertical);
        
        // Offset for scroll view positioning of drawn tree
        private Vector2 scrollPosition = Vector2.zero, detailsScrollPosition = Vector2.zero;

        // Toggle for determining if unchanged nodes are drawn
        private bool hideUnchanged = false, previousToggle = false;

        // Last window drawing event detected
        private EventType previousEvent;

        // Set default values for parameters affecting node position and scale in tree
        //TODO allow user configuration
        private float panStep = 50;
        private float zoomeScaleStep = 0.1f;
        private float zoomLevel = 0;
        private int defaultFontSize = 12;
        private float baseBoxWidth = 75;
        private float baseBoxHeight = 75;
        private float yPad = 0, xPad = 50;

        // Keep track of global impact strength modifiers
        private int totalChangesInTree = 0;
        private float maxImpactStrength = 0;

        private void OnEnable() {
            if (window == null) {
                Init();
            }

            depAnalyzer = new DependencyAnalyzer();
            gitAnalyzer = new GitAnalyzer();

            verticalSplitView.splitNormalizedPosition = 0.2f;
            
            // Create shortened names for commit messages, shown in drop-down menu
            commitNames = new string[gitAnalyzer.commitList.Count + 1];
            commitNames[0] = "NOTHING SELECTED";
            for (int i = 0; i < gitAnalyzer.commitList.Count; i++) {
                LibGit2Sharp.Commit c = gitAnalyzer.commitList[i];
                string commitName = (i + 1) + ": [" + c.Author.When.DateTime.ToString("g") + "] (" + c.Author.Name + "): " + c.Message;
                commitName = commitName.Replace("/", "-"); // The EditorGUILayout.PopUp uses '/' to branch the selection, which I dont want
                commitName = commitName.Replace("\n", " "); // Newlines cause subsequent options to be pushed down, which I dont want
                commitName = commitName.Substring(0, Mathf.Min(100, commitName.Length)) + "...";
                commitNames[i + 1] = commitName;
            }

            commitIndex = 0;
            typeIndex = 0;
            hideUnchanged = false;

            ResetTree();
        }

        private void OnDisable() {
            isStyleInit = false;
        }

        /// <summary>
        /// Create the actual window where all UI elements are placed
        /// </summary>
        [MenuItem("Window/WhatHappened")]
        public static void Init() {
            window = (DependencyWindow)EditorWindow.GetWindow<DependencyWindow>("WhatHappened");
            window.Show();
            window.minSize = new Vector2(300, 300); //TODO change
        }

        /// <summary>
        /// OnGUI is called at least once per frame, drawing all of the window contents
        /// </summary>
        private void OnGUI() {

            // Set the UI style if it hasn't already been set
            if (!isStyleInit) {
                InitGUIStyle();
            }

            // Intrepret input from user (keystrokes, mouse clicking)
            HandleInput();

            horizontalSplitView.BeginSplitView();

            // Draw the dependency tree for the type selected from the drop-down menu
            DrawTree();
            horizontalSplitView.Split();
            verticalSplitView.BeginSplitView();

            // Create the panel containing selection for types in project, commits, and toggle for filtering nodes
            CreateOptionsPanel();
            verticalSplitView.Split();

            // Create the panel displaying relevant information about the selected node
            CreateDetailsPanel();
            verticalSplitView.EndSplitView();
            horizontalSplitView.EndSplitView();

            Repaint();
            previousEvent = Event.current.type;
        }

        /// <summary>
        /// Set style parameters for how UI elements are displayed
        /// </summary>
        private void InitGUIStyle() {
            style = new GUIStyle(GUI.skin.box);
            style.font = Resources.Load<Font>("JAi_____");
            style.fontSize = defaultFontSize;
            style.alignment = TextAnchor.MiddleCenter;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.fontSize = 13;

            popupStyle = new GUIStyle(EditorStyles.popup);
            popupStyle.wordWrap = false;
            popupStyle.fontSize = 12;

            skin = ScriptableObject.CreateInstance<GUISkin>();
            skin.box = style;
            skin.label = style;
            skin.font = style.font;

            isStyleInit = true;
        }

        private void HandleInput() {

            // Handle positioning and scaling of scroll view and nodes
            if (Event.current.type == EventType.KeyDown) {
                switch (Event.current.keyCode) {
                    case KeyCode.RightArrow:
                        scrollPosition += new Vector2(panStep, 0);
                        break;
                    case KeyCode.LeftArrow:
                        scrollPosition -= new Vector2(panStep, 0);
                        break;
                    case KeyCode.UpArrow:
                        scrollPosition -= new Vector2(0, panStep);
                        break;
                    case KeyCode.DownArrow:
                        scrollPosition += new Vector2(0, panStep);
                        break;
                    case KeyCode.PageUp:
                        if (!Event.current.modifiers.ToString().Contains("Shift")) {
                            zoomLevel++;
                        }
                        style.fontSize++;

                        Debug.LogWarning("ZOOM IN");
                        break;
                    case KeyCode.PageDown:
                        if (!Event.current.modifiers.ToString().Contains("Shift")) {
                            zoomLevel--;
                        }
                        style.fontSize--;
                        Debug.LogWarning("ZOOM OUT");
                        break;
                }
            }

            // Determine which node user has left-clicked on
            // NOTE: need to store selected node in "tempSelectedNode" to prevent Unity bug that occurs when 
            //      selected node is immediately modified in same frame
            if (previousEvent == EventType.Layout) {
                if (Event.current.type == EventType.MouseDown) {
                    Vector2 mousePos = Event.current.mousePosition;
                    foreach (LinkedList<CustomTypeNode> nodeList in allNodes.Values) {
                        foreach (CustomTypeNode n in nodeList) {
                            if (n.rect.Contains(mousePos + scrollPosition)) {
                                tempSelectedNode = n;
                            }
                        }
                    }
                }
            }

            // Need this interim step to prevent Unity bug when immediately referencing the selected node
            if (previousEvent == EventType.Repaint) {
                selectedNode = tempSelectedNode;
            }
        }

        /// <summary>
        /// Draw the dependeny tree using the previously created allNodes data structure.
        /// </summary>
        private void DrawTree() {

            // Draw the background
            GUI.color = Color.gray;
            GUI.Box(new Rect(0, 0, window.position.width, window.position.height), "");
            GUI.color = Color.white;

            // Scale dimensions of boxes/window by zoom factor
            float scaledBoxWidth = baseBoxWidth + baseBoxWidth * (zoomeScaleStep * zoomLevel);
            float scaledBoxHeight = baseBoxHeight + baseBoxHeight * (zoomeScaleStep * zoomLevel);
            float scaledWindowHeight = window.position.height + window.position.height * (zoomeScaleStep * zoomLevel);

            // Calculate absolute height of tree based on the max number of nodes at any level of the tree
            float scaledTreeHeight = scaledBoxHeight * (maxNodesPerLevel * 2 - 1);

            Rect scaledRect = new Rect(0, 0, scaledBoxWidth * 3 * allNodes.Count, Mathf.Max(scaledWindowHeight, scaledTreeHeight));

            // Adjust yPad so that the scroll view centers at the same level as the root node
            if (scaledTreeHeight < scaledRect.height) {
                yPad = scaledRect.height / 2f - scaledTreeHeight / 2f;
            }
            else if (isFirstFrameDrawn) {
                yPad = 0;
                //yPad = scaledRect.height / 2f - scaledTreeHeight / 2f;
                scrollPosition.y = scaledTreeHeight / 2f - scaledWindowHeight / 2f;
            }
            isFirstFrameDrawn = false;

            scrollPosition = GUI.BeginScrollView(new Rect(0, 0, window.position.width * horizontalSplitView.splitNormalizedPosition, window.position.height), scrollPosition, scaledRect);

            DrawNodesByLevel(scaledBoxWidth, scaledBoxHeight);

            GUI.EndScrollView();
        }

        /// <summary>
        /// Iterate through all nodes by level, and then by node, drawing nodes and edges left-to-right, and
        /// top-to-bottom.
        /// </summary>
        private void DrawNodesByLevel(float scaledBoxWidth, float scaledBoxHeight) {

            // Iterate through each level of the tree (drawing left to right)
            foreach (KeyValuePair<int, LinkedList<CustomTypeNode>> pair in allNodes) {
                int level = pair.Key;
                int numNodes = pair.Value.Count;

                // Calculate distance between nodes and scale based on zoom factor
                float xStep = baseBoxWidth * 3;
                xStep += xStep * zoomeScaleStep * zoomLevel;
                float yStep = ((maxNodesPerLevel * 2 - 1) - numNodes) / (numNodes + 1.0f) * baseBoxHeight;
                yStep += yStep * zoomeScaleStep * zoomLevel;

                // Iterate through each node in the level (drawing top to bottom)
                int nodeIndex = 0;

                foreach (CustomTypeNode node in pair.Value) {
                    float yPos = 0, xPos = 0;

                    //Ensure that levels with the maximal number of nodes begin at the top of the screen
                    if (numNodes == maxNodesPerLevel) {
                        yPos = yPad + 2 * scaledBoxHeight * nodeIndex;
                    }
                    else {
                        yPos = yPad + yStep * (nodeIndex + 1) + scaledBoxHeight * nodeIndex;
                    }

                    xPos = xPad + xStep * level;
                    node.rect = new Rect(xPos, yPos, scaledBoxWidth, scaledBoxHeight);

                    // Draw outline around node (by putting a bigger yellow box behind it)
                    if (node == selectedNode) {
                        GUI.color = Color.yellow;
                        Rect biggerRect = new Rect(node.rect);
                        biggerRect.size *= 1.2f;
                        biggerRect.center = node.rect.center;
                        GUI.Box(biggerRect, "", style);
                    }

                    // Use a red sequential color scheme to color nodes based on their impact strength
                    if (node.type.bHasChanged) {
                        // Calculate the relative impact strength of the node (compared to all others)
                        float normalizedImpactStrength = node.CalculateNormalizedImpactStrength(maxImpactStrength);

                        GUI.color = Color.HSVToRGB(0, normalizedImpactStrength, 1);
                    }
                    else {
                        GUI.color = Color.white;
                    }

                    // Draw the node!
                    GUI.Box(node.rect, node.type.simplifiedFullName, style);

                    //Draw the line and arrow from parent to child node
                    if (node.parent != null) {
                        if (node.bIsCyclic) {
                            Handles.color = Color.blue;
                        }
                        else {
                            Handles.color = Color.black;
                        }
                        Handles.BeginGUI();
                        Handles.DrawLine(node.parent.rightAnchor, node.leftAnchor);
                        Handles.DrawAAConvexPolygon(node.GetArrow());
                        Handles.EndGUI();
                    }

                    nodeIndex++;
                }
            }
        }

        /// <summary>
        /// Draw panel that houses drop-down menus for selecting the root type of the tree and which
        /// commit to diff files with. Also contains a toggle to hide unchanged nodes in the tree.
        /// TODO need to consolidate code, reduce redundancy
        /// </summary>
        private void CreateOptionsPanel() {
            GUILayout.BeginVertical();
            GUI.color = new Color(168 / 255f, 247 / 255f, 255 / 255f); // light teal

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("-- WhatHappened Options --", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Drop-down menu to select root node of tree
            typeIndex = EditorGUILayout.Popup("Select Root Type:", typeIndex, typeNames, popupStyle);
            if (typeIndex != prevTypeIndex) {
                hideUnchanged = false;
                ResetTree();
                DiffFilesInTree();
                prevTypeIndex = typeIndex;
            }

            // Drop-down menu to select which commit to use to diff files
            commitIndex = EditorGUILayout.Popup("Select Past Commit:", commitIndex, commitNames, popupStyle);
            if (commitIndex != prevCommitIndex) {
                hideUnchanged = false;
                ResetTree();
                //float tempStrength = maxImpactStrength;
                DiffFilesInTree();
                // If going from unfiltered to filtered, then ensure the maxImpact strength reflects the unfiltered tree
                //if (hideUnchanged == true) {
                //    maxImpactStrength = tempStrength;
                //}

                prevCommitIndex = commitIndex;
            }

            // Toggle button to hide/show unchanged nodes in the tree
            hideUnchanged = EditorGUILayout.Toggle("Hide Unchanged Nodes:", hideUnchanged);
            if(hideUnchanged != previousToggle) {
                previousToggle = hideUnchanged;
                float tempStrength = maxImpactStrength;
                ResetTree(); //but keep same type/git index
                DiffFilesInTree();

                // If going from unfiltered to filtered, then ensure the maxImpact strength reflects the unfiltered tree
                if (hideUnchanged == true) {
                    //maxImpactStrength = tempStrength;
                }
                //TODO: Consider if maxNumLevels in tree changes, alter impact strength
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Diffs all files represented by the nodes in the currently displayed scene, comparing their version
        /// from the working directory with their version from the selected commit.  Further keeps tallies on the total
        /// changes in tree and global maximum for impact strength.
        /// </summary>
        private void DiffFilesInTree() {
            totalChangesInTree = 0;
            foreach (CustomFile f in allFiles) {
                totalChangesInTree += gitAnalyzer.DiffFile(f, commitIndex - 1); // -1 since index 0 is just "NO SELECTION"
            }

            // Determine the max absolute impact strength out of all nodes in tree
            maxImpactStrength = 0;
            foreach (LinkedList<CustomTypeNode> nodes in allNodes.Values) {
                foreach (CustomTypeNode node in nodes) {
                    float currentStrength = node.CalculateAbsoluteImpactStrength(totalChangesInTree, allNodes.Count);
                    if (currentStrength > maxImpactStrength) {
                        maxImpactStrength = currentStrength;
                    }
                }
            }
        }

        /// <summary>
        /// Create panel showing information about changes to the selected node on both a type- and method- level.
        /// Also includes buttons to allow quick navigation to types/methods, and the diff patch (if applicable)
        /// </summary>
        void CreateDetailsPanel() {

            // Display limited info if no node selected
            if (selectedNode == null) {
                detailsScrollPosition = GUILayout.BeginScrollView(detailsScrollPosition);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("-- Selected Node Details --", labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                return;
            }

            CustomType selectedType = selectedNode.type;

            GUILayout.BeginVertical();

            ShowTypeDetails(selectedType);
            ShowMethodDetails(selectedType);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Create the subsection of the details panel pertaining to the selected node's type
        /// </summary>
        private void ShowTypeDetails(CustomType selectedType) {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("-- " + selectedType + " Node Details -- (click to open)", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            // Create button to open file corresponding to selected type
            bool clickedOpenType = GUILayout.Button(selectedType.ToString() + " (in file: " + selectedType.file + ")");
            if (clickedOpenType) {
                Debug.LogWarning("Attempting to open: " + selectedType);
                selectedType.file.OpenFileInEditor(selectedType.startLineNum);
            }
            GUILayout.EndHorizontal();

            // Create button to open the patch for the file where the selectedType resides
            if (selectedType.totalLineChanges > 0) {
                GUILayout.BeginHorizontal();
                bool clickedOpenDiff = GUILayout.Button("Open Full Diff Text");
                if (clickedOpenDiff) {
                    Debug.LogWarning("Attempting to open diff: " + selectedType);
                    selectedType.file.OpenDiffPatchInEditor();
                }
                GUILayout.EndHorizontal();
            }

            // Display info about changes detected from the diff
            GUILayout.Label("    Impact Strength: " + Math.Round(selectedNode.normalizedImpactStrength, 2) + " out of 1");
            GUILayout.Label("    Total Changes: " + selectedType.totalLineChanges);
            GUILayout.Label("    Changes Outside Methods: " + selectedType.totalChangesOutsideMethods);
            GUILayout.Label("        Additions: " + selectedType.additionsOutsideMethods);
            GUILayout.Label("        Deletions: " + selectedType.deletionsOutsideMethods);
            GUILayout.Label("    Changes Inside Methods: " + selectedType.totalChangesInMethods);
            GUILayout.Label("        Additions: " + selectedType.additionsInMethods);
            GUILayout.Label("        Deletions: " + selectedType.deletionsInMethods);
            GUILayout.Box("", GUILayout.Height(3), GUILayout.ExpandWidth(true));
        }

        /// <summary>
        /// Create the subsection of the details panel pertaining to the selected node's methods
        /// </summary>
        private void ShowMethodDetails(CustomType selectedType) {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("-- Method Details -- (click to open)", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Display all methods in selected type, in descending order based on number of changes detected
            detailsScrollPosition = GUILayout.BeginScrollView(detailsScrollPosition);
            bool startedUnchanged = false;
            int methodIndex = 0;
            foreach (CustomMethod m in selectedType.GetSortedMethodsByChanges()) {
                if (startedUnchanged == false) {
                    if (m.totalChanges == 0) {
                        GUILayout.Label("-- UNCHANGED METHODS --", labelStyle);
                        startedUnchanged = true;
                    }
                    else if (methodIndex == 0) {
                        GUILayout.Label("-- CHANGED METHODS --", labelStyle);
                    }
                }

                // Create button to allow file to be opened at the line num of this method
                GUILayout.BeginHorizontal();
                bool clickedOpenMethod = GUILayout.Button(m.GetSimplifiedMethodSignature() + ":", GUILayout.ExpandWidth(false));
                if (clickedOpenMethod) {
                    selectedType.file.OpenFileInEditor(m.startLineNum);
                }
                GUILayout.EndHorizontal();

                // Display changes specific to this method
                if (!startedUnchanged) {
                    GUILayout.Label("    Total Changes: " + m.totalChanges);
                    GUILayout.Label("        Additions: " + m.additions);
                    GUILayout.Label("        Deletions: " + m.deletions);
                }
                methodIndex++;
            }

            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Create the allNodes data structure that represents the dependency tree, branching from the root selected
        /// type.  Uses a depth-first-search approach to navigate through the root node's type dependencies, either until a type
        /// has no dependencies, or a cyclic dependency is detected.  Returns true if a change is detected in the current
        /// node or any children along its branch (used to filter nodes based on changes)
        /// </summary>
        bool CreateDependencyTree(CustomType type, CustomTypeNode parent, LinkedList<CustomType> currentBranch, int level) {

            CustomTypeNode node = new CustomTypeNode(type, parent, level);
            HashSet<CustomType> dependencies = type.dependencies;

            bool anyChildrenChanged = false;
            if (currentBranch.Contains(type)) {
                node.bIsCyclic = true;
            }
            else if (dependencies.Count > 0) {
                currentBranch.AddLast(type);

                foreach (CustomType dependency in dependencies) {
                    bool isChanged = CreateDependencyTree(dependency, node, currentBranch, level + 1);
                    if(isChanged) {
                        anyChildrenChanged = true;
                    }
                }
                currentBranch.RemoveLast();
            }

            // Don't include this node if the "hideUnchanged" toggle is on and no changes have happened in this branch
            if (!hideUnchanged || (hideUnchanged && (type.bHasChanged || anyChildrenChanged || level == 0))) {

                if (!allNodes.ContainsKey(level)) {
                    LinkedList<CustomTypeNode> listThisLevel = new LinkedList<CustomTypeNode>();
                    listThisLevel.AddLast(node);
                    allNodes.Add(level, listThisLevel);
                }
                else {
                    allNodes[level].AddLast(node);
                }

                allFiles.Add(type.file);
                return true;
            }
            else {
                return false;
            }
        }

        

        void ResetTree(bool keepIndicesSame = false) {

            if (style != null) {
                style.fontSize = defaultFontSize;
            }

            // Reset vars to default values
            selectedNode = null;
            tempSelectedNode = null;
            isFirstFrameDrawn = true;
            maxNodesPerLevel = 0;
            totalChangesInTree = 0;
            maxImpactStrength = 0;
            yPad = 0;
            zoomLevel = 0;
            scrollPosition = Vector2.zero;

            // Recreate the global data structures
            allNodes = new Dictionary<int, LinkedList<CustomTypeNode>>();
            allFiles = new HashSet<CustomFile>();

            // Find the CustomType selected from the drop-down menu
            Dictionary<string, CustomType>.ValueCollection customTypes = depAnalyzer.GetAllCustomTypes();
            typeNames = new string[customTypes.Count];
            CustomType chosenType = null;
            int i = 0;
            foreach (CustomType t in customTypes) {
                if (i == typeIndex) {
                    chosenType = DependencyAnalyzer.GetCustomTypeFromString(t.simplifiedFullName);
                }
                typeNames[i] = t.simplifiedFullName + " (in file: " + t.file + ")";
                i++;
            }
            
            // Populate the allNodes data structure, using the chosenType as the starting point
            CreateDependencyTree(chosenType, null, new LinkedList<CustomType>(), 0);

            // Calculate the maximum number of nodes at any level of the tree
            foreach (KeyValuePair<int, LinkedList<CustomTypeNode>> pair in allNodes) {
                if (pair.Value.Count > maxNodesPerLevel) {
                    maxNodesPerLevel = pair.Value.Count;
                }
            }
        }
    }
}