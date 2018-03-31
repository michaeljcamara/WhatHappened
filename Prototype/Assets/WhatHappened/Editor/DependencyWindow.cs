using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace WhatHappened {
    public class DependencyWindow : EditorWindow {

        private string[] typeNames;
        private Vector2 positionOffset = Vector2.zero;

        private float panStep = 50;
        private float zoomPositionStep = 20f;
        private float zoomeScaleStep = 0.1f;
        private float zoomLevel = 0;

        private static DependencyWindow window;
        private DependencyAnalyzer analyzer;
        private GitAnalyzer gitAnalyzer;
        public Rect m_backgroundRect;
        bool isStyleInit;
        int typeIndex, prevTypeIndex;
        int commitIndex, prevCommitIndex;
        private GUIStyle style, popupStyle;
        private GUISkin skin;

        EditorGUISplitView horizontalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
        EditorGUISplitView verticalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Vertical);
        EditorGUISplitView detailsSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Vertical);

        //TODO
        int maxNodesPerLevel;
        Dictionary<int, int> nodesPerLevel;
        CustomTypeNode selectedNode, prevSelectedNode;
        Dictionary<int, LinkedList<CustomTypeNode>> allNodes;
        HashSet<CustomFile> allFiles;

        Vector2 scrollPosition = Vector2.zero, detailsScrollPosition = Vector2.zero;
        float baseBoxWidth = 50;
        float baseBoxHeight = 50;

        bool isFirstFrameDrawn = true;
        float yPad = 0, xPad = 50;
        string[] commitNames;

        CustomTypeNode specialNode;
        EventType previousEvent;
        GUIStyle labelStyle;

        private void Awake() {
            Debug.LogWarning("NOW AWAKE!!");
        }

        private void OnEnable() {
            Debug.LogWarning("ENABLEEEEED");

            if (window == null) {
                Init();
            }

            analyzer = new DependencyAnalyzer();
            gitAnalyzer = new GitAnalyzer();

            verticalSplitView.splitNormalizedPosition = 0.2f;
            //verticalSplitView.scrollPosition = new Vector2(0, 500);


            //commitNames = new List<string>(gitAnalyzer.commitList.Count + 1);
            commitNames = new string[gitAnalyzer.commitList.Count + 1];
            //commitNames.Add("NOTHING SELECTED");
            commitNames[0] = "NOTHING SELECTED";
            for (int i = 0; i < gitAnalyzer.commitList.Count; i++) {
                //foreach(LibGit2Sharp.Commit c in gitAnalyzer.commitList) {
                //commitNames.Add(c.Author.When + ", " + c.Author.Name + ": " + c.MessageShort);
                LibGit2Sharp.Commit c = gitAnalyzer.commitList[i];
                //string commitName = (i+1) + ": " + c.Author.When.DateTime.ToString("F") + ", " + c.Author.Name + ": " + c.MessageShort;
                //string commitName = (i + 1) + ": " + c.Author.When.DateTime.ToString("F") + ", " + c.Author.Name + ": " + c.Message;
                string commitName = (i + 1) + ": [" + c.Author.When.DateTime.ToString("g") + "] (" + c.Author.Name + "): " + c.Message;
                commitName = commitName.Replace("/", "-"); // The EditorGUILayout.PopUp uses '/' to branch the selection, which I dont want
                commitName = commitName.Replace("\n", " "); // The EditorGUILayout.PopUp uses '/' to branch the selection, which I dont want
                commitName = commitName.Substring(0, Mathf.Min(100, commitName.Length)) + "...";
                //commitNames[i + 1] = c.Author.Name + ", " + i;
                commitNames[i + 1] = commitName;
            }

            commitIndex = 0;
            typeIndex = 0;

            ResetTree();
        }

        private void OnDisable() {
            isStyleInit = false;
            Debug.LogWarning("Window disabled!!");
        }

        private void InitGUIStyle() {
            style = new GUIStyle(GUI.skin.box);
            style.font = Resources.Load<Font>("JAi_____");
            style.fontSize = 12;
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

        [MenuItem("Window/WhatHappened")]
        public static void Init() {
            Debug.LogError("INITTING WINDOW");
            window = (DependencyWindow)EditorWindow.GetWindow<DependencyWindow>("WhatHappened");
            window.Show();
            window.minSize = new Vector2(600, 600); //TODO change
        }

        private void HandleInput() {

            if (Event.current.type == EventType.ScrollWheel) {
                if (Event.current.delta.y > 0) {
                    zoomLevel--;
                    Debug.LogWarning("ZOOM OUT");
                }
                else {
                    zoomLevel++;
                    Debug.LogWarning("ZOOM IN");
                }

                Debug.Log("Zoom level = " + zoomLevel);
            }

            //TODO TEST THIS WITH DOCKED WINDOW!!
            if (Event.current.type == EventType.KeyDown) {
                switch (Event.current.keyCode) {
                    case KeyCode.RightArrow:
                        Debug.LogWarning("PAN RIGHT");
                        positionOffset -= new Vector2(panStep, 0);
                        break;
                    case KeyCode.LeftArrow:
                        Debug.LogWarning("PAN LEFT");
                        positionOffset += new Vector2(panStep, 0);
                        break;
                    case KeyCode.UpArrow:
                        Debug.LogWarning("PAN UP");
                        positionOffset += new Vector2(0, panStep);
                        break;
                    case KeyCode.DownArrow:
                        Debug.LogWarning("PAN DOWN");
                        positionOffset -= new Vector2(0, panStep);
                        break;
                    case KeyCode.PageUp:
                        zoomLevel++;
                        Debug.LogWarning("ZOOM IN");
                        break;
                    case KeyCode.PageDown:
                        zoomLevel--;
                        Debug.LogWarning("ZOOM OUT");
                        break;
                }
            }

            if (previousEvent == EventType.Layout) {
                if (Event.current.type == EventType.MouseDown) {

                    Vector2 mousePos = Event.current.mousePosition;

                    foreach (LinkedList<CustomTypeNode> nodeList in allNodes.Values) {
                        foreach (CustomTypeNode n in nodeList) {
                            if (n.rect.Contains(mousePos + scrollPosition)) {
                                Debug.LogWarning("CLICKED ON : " + n.type);
                                //prevSelectedNode = selectedNode;
                                //selectedNode = n;
                                specialNode = n;
                            }
                        }
                    }
                }
            }

            if (previousEvent == EventType.Repaint) {
                selectedNode = specialNode;
            }
        }

        void OnGUI() {

            if (!isStyleInit) {
                InitGUIStyle();
            }

            HandleInput();

            horizontalSplitView.BeginSplitView();
            m_backgroundRect = new Rect(0, 0, window.position.width, window.position.height);
            GUI.color = Color.gray;
            GUI.Box(m_backgroundRect, "");

            DrawNodes();

            horizontalSplitView.Split();

            verticalSplitView.BeginSplitView();

            //GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            //GUILayout.FlexibleSpace();

            CreateOptionsPanel();

            //GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            //GUILayout.FlexibleSpace();
            //GUILayout.EndHorizontal();

            verticalSplitView.Split();

            GUILayout.BeginVertical();
            CreateDetailsPanel();
            GUILayout.EndVertical();
            verticalSplitView.EndSplitView();
            horizontalSplitView.EndSplitView();

            Repaint();
            previousEvent = Event.current.type;
        }

        void CreateDetailsPanel() {

            //TODO ONLY IF SELECTEDNODE !+ PREVSELECTEDNODE
            //if (selectedNode == prevSelectedNode) {
            //    return;
            //}
            //prevSelectedNode = selectedNode; 

            if (selectedNode == null) {
                //detailsScrollPosition = GUI.BeginScrollView(new Rect(0, 0, window.position.width * horizontalSplitView.splitNormalizedPosition, window.position.height), scrollPosition, scaledRect);
                detailsScrollPosition = GUILayout.BeginScrollView(detailsScrollPosition);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("-- Selected Node Details --", labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.EndScrollView();
                return;
            }

            CustomType t = selectedNode.type;

            //detailsSplitView.BeginSplitView();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("-- " + t + " Node Details -- (click to open)", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool clickedOpenType = GUILayout.Button(t.ToString() + " (in file: " + t.file.name + ")");
            if (clickedOpenType) {
                Debug.LogWarning("Attempting to open: " + t);
                t.file.OpenFileInEditor(t.startLineNum);
            }
            GUILayout.EndHorizontal();

            if (t.totalLineChanges > 0) {
                GUILayout.BeginHorizontal();
                bool clickedOpenDiff = GUILayout.Button("Open Full Diff Text");
                if (clickedOpenDiff) {
                    Debug.LogWarning("Attempting to open diff: " + t);
                    t.file.OpenDiffTextInEditor();
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("    Total Changes: " + t.totalLineChanges);
            GUILayout.Label("    Changes Outside Methods: " + t.totalChangesOutsideMethods);
            GUILayout.Label("        Additions: " + t.additionsOutsideMethods);
            GUILayout.Label("        Deletions: " + t.deletionsOutsideMethods);
            GUILayout.Label("    Changes Inside Methods: " + t.totalChangesInMethods);
            GUILayout.Label("        Additions: " + t.additionsInMethods);
            GUILayout.Label("        Deletions: " + t.deletionsInMethods);

            //detailsSplitView.Split();
            //GUILayout.Box("", GUILayout.ExpandWidth(true));
            GUILayout.Box("", GUILayout.Height(3), GUILayout.ExpandWidth(true));

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("-- Method Details -- (click to open)", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            detailsScrollPosition = GUILayout.BeginScrollView(detailsScrollPosition);
            //GUILayout.Space(10);

            bool startedUnchanged = false;
            int methodIndex = 0;
            foreach (CustomMethod m in t.GetSortedMethodsByChanges()) {
                if (startedUnchanged == false) {
                    if (m.totalChanges == 0) {
                        GUILayout.Label("-- UNCHANGED METHODS --", labelStyle);
                        startedUnchanged = true;
                    }
                    else if (methodIndex == 0) {
                        GUILayout.Label("-- CHANGED METHODS --", labelStyle);
                    }
                }
                //TODO HOW TO START CHANGED METHODS 

                GUILayout.BeginHorizontal();

                bool clickedOpenMethod = GUILayout.Button(m.GetSimplifiedMethodSignature() + ":", GUILayout.ExpandWidth(false));
                if (clickedOpenMethod) {
                    Debug.LogWarning("Attempting to open: " + m + " at :" + m.startLineNum);
                    t.file.OpenFileInEditor(m.startLineNum);
                }
                GUILayout.EndHorizontal();

                if (!startedUnchanged) {
                    GUILayout.Label("    Total Changes: " + m.totalChanges);
                    GUILayout.Label("        Additions: " + m.additions);
                    GUILayout.Label("        Deletions: " + m.deletions);
                }

                methodIndex++;
            }

            GUILayout.EndScrollView();
            //detailsSplitView.EndSplitView();

        }

        void CreateOptionsPanel() {
            //GUI.color = new Color(102, 178, 178);
            GUI.color = new Color(168 / 255f, 247 / 255f, 255 / 255f);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("-- WhatHappened Options --", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            //GUILayoutOption[] popupOptions = { GUILayout.ExpandWidth(true)};//, GUILayout.ExpandHeight(true) };

            typeIndex = EditorGUILayout.Popup("Select Root Type:", typeIndex, typeNames, popupStyle);


            if (typeIndex != prevTypeIndex) {
                ResetTree();
                prevTypeIndex = typeIndex;
            }

            commitIndex = EditorGUILayout.Popup("Select Past Commit:", commitIndex, commitNames, popupStyle);

            if (commitIndex != prevCommitIndex) {

                DiffFilesInTree();
                prevCommitIndex = commitIndex;
            }

            //TODO FILTER BUTTON!!!!!!!!!

        }

        void DiffFilesInTree() {
            foreach (CustomFile f in allFiles) {
                gitAnalyzer.DiffFile(f, commitIndex - 1); // -1 since index 0 is just "NO SELECTION"
            }
        }


        void ResetTree() {
            selectedNode = null;
            specialNode = null;
            prevSelectedNode = null;
            isFirstFrameDrawn = true;
            maxNodesPerLevel = 0;
            yPad = 0;
            nodesPerLevel = new Dictionary<int, int>();
            allFiles = new HashSet<CustomFile>();
            Debug.Log("Is analyzer null?: " + analyzer + ", " + ((analyzer == null) ? "NULL" : "NOT NULL"));
            Dictionary<string, CustomType>.ValueCollection customTypes = analyzer.GetAllCustomTypes();
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
            positionOffset = Vector2.zero;
            zoomLevel = 0;

            allNodes = CreateDependencyTree(chosenType, null, new LinkedList<CustomType>(), new Dictionary<int, LinkedList<CustomTypeNode>>(), 0);

            int nodeCount = 0;
            foreach (KeyValuePair<int, LinkedList<CustomTypeNode>> pair in allNodes) {
                if (pair.Value.Count > maxNodesPerLevel) {
                    maxNodesPerLevel = pair.Value.Count;
                }
                int nodeIndex = 0;
                foreach (CustomTypeNode node in pair.Value) {
                    Debug.LogWarning("  NODE IS: " + node.type + ", Level is: " + pair.Key + ", index = " + nodeIndex);
                    nodeCount++;
                    nodeIndex++;
                }
            }
            Debug.LogWarning("TOTAL NODES MADE: " + allNodes.Count);
        }

        Dictionary<int, LinkedList<CustomTypeNode>> CreateDependencyTree(CustomType type, CustomTypeNode parent, LinkedList<CustomType> currentBranch, Dictionary<int, LinkedList<CustomTypeNode>> allNodes, int level) {

            CustomTypeNode node = new CustomTypeNode(type, parent, level);
            if (!allNodes.ContainsKey(level)) {
                LinkedList<CustomTypeNode> listThisLevel = new LinkedList<CustomTypeNode>();
                listThisLevel.AddLast(node);
                allNodes.Add(level, listThisLevel);
            }
            else {
                allNodes[level].AddLast(node);
            }

            //Debug.Log("Type in tree: " + type);
            //Debug.Log("   Is File null?: " + ((type.file == null) ? "TRUE" : "FALSE"));
            //Debug.Log("   FileInfo from Type: " + type.file.info);
            //Debug.Log("     FileName from Type: " + type.file.info.FullName);
            allFiles.Add(type.file);

            HashSet<CustomType> dependencies = type.GetDependencies();

            if (currentBranch.Contains(type)) {
                node.SetCyclic(true);
            }
            else if (dependencies.Count > 0) {
                currentBranch.AddLast(type);

                foreach (CustomType dependency in dependencies) {
                    //CreateDependencyTree(stack, allNodes, level + 1);            
                    CreateDependencyTree(dependency, node, currentBranch, allNodes, level + 1);
                }
                currentBranch.RemoveLast();
            }

            return allNodes;
        }


        void DrawNodes() {
            GUI.color = Color.white;

            float scaledBoxWidth = baseBoxWidth + baseBoxWidth * (zoomeScaleStep * zoomLevel);
            float scaledBoxHeight = baseBoxHeight + baseBoxHeight * (zoomeScaleStep * zoomLevel);
            float scaledTreeHeight = scaledBoxHeight * (maxNodesPerLevel * 2 - 1);
            float scaledWindowHeight = window.position.height + window.position.height * (zoomeScaleStep * zoomLevel);
            Rect scaledRect = new Rect(0, 0, scaledBoxWidth * 3 * allNodes.Count, Mathf.Max(scaledWindowHeight, scaledTreeHeight));

            if (scaledTreeHeight < scaledRect.height) {
                yPad = scaledRect.height / 2f - scaledTreeHeight / 2f;
            }
            else if (isFirstFrameDrawn) {
                yPad = 0;
                scrollPosition.y = scaledTreeHeight / 2f;
            }
            isFirstFrameDrawn = false;

            scrollPosition = GUI.BeginScrollView(new Rect(0, 0, window.position.width * horizontalSplitView.splitNormalizedPosition, window.position.height), scrollPosition, scaledRect);

            foreach (KeyValuePair<int, LinkedList<CustomTypeNode>> pair in allNodes) {
                int level = pair.Key;
                int numNodes = pair.Value.Count;

                float xStep = baseBoxHeight * 3;
                float yStep = ((maxNodesPerLevel * 2 - 1) - numNodes) / (numNodes + 1.0f) * baseBoxHeight;

                xStep += xStep * zoomeScaleStep * zoomLevel;
                yStep += yStep * zoomeScaleStep * zoomLevel;

                int nodeIndex = 0;
                foreach (CustomTypeNode node in pair.Value) {
                    //Debug.Log("Drawing: " + node.type.simplifiedFullName + ", level: " + level + ", Index: " + nodeIndex);
                    node.rect = new Rect(xPad + xStep * level, yPad + (nodeIndex + 1) * yStep + (nodeIndex * scaledBoxHeight), scaledBoxWidth, scaledBoxHeight);

                    // Draw outline around node (by putting a bigger yellow box behind it)
                    if (node == selectedNode) {
                        GUI.color = Color.yellow;
                        Rect biggerRect = new Rect(node.rect);
                        biggerRect.size *= 1.2f;
                        biggerRect.center = node.rect.center;
                        GUI.Box(biggerRect, node.type.simplifiedFullName, style);
                    }

                    if (node.type.hasChanged) {
                        //TODO gradation based on impact strength
                        GUI.color = Color.red;
                    }
                    else {
                        GUI.color = Color.white;
                    }

                    GUI.Box(node.rect, node.type.simplifiedFullName, style);



                    if (node.parent != null) {

                        if (node.isCyclic) {
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
            GUI.EndScrollView();
        }
    }
}