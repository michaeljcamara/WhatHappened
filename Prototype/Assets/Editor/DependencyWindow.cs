using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class DependencyWindow : EditorWindow {
   
    private string[] typeNames;
    private Vector2 positionOffset = Vector2.zero;

    private float panStep = 50;
    private float zoomPositionStep = 20f;
    private float zoomeScaleStep = 0.1f;
    private float zoomLevel = 0;

    private static DependencyWindow window;
    private DependencyAnalyzer analyzer;
    public Rect m_backgroundRect;
    bool isStyleInit;
    int index, prevIndex;
    private GUIStyle style;
    private GUISkin skin;

    EditorGUISplitView horizontalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
    EditorGUISplitView verticalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Vertical);

    //TODO
    int maxNodesPerLevel;
    Dictionary<int, int> nodesPerLevel;
    CustomTypeNode selectedNode;
    Dictionary<int, LinkedList<CustomTypeNode>> allNodes;

    Vector2 scrollPosition = Vector2.zero;
    float baseBoxWidth = 50;
    float baseBoxHeight = 50;

    bool isFirstFrameDrawn = true;
    float yPad = 0, xPad = 50;

    private void Awake() {
        Debug.LogWarning("NOW AWAKE!!");
    }

    private void OnEnable() {
        Debug.LogWarning("ENABLEEEEED");
        Debug.Log("STYLE BEFORE INIT: " + style);

        analyzer = new DependencyAnalyzer();

        //nodesPerLevel = new Dictionary<int, int>();

        ////**MOVE THIS TO ONGUI...CLICKED???
        //Dictionary<string, CustomType>.ValueCollection customTypes = analyzer.GetAllCustomTypes();
        //typeNames = new string[customTypes.Count];
        //int typeIndex = 0;
        //foreach (CustomType t in customTypes) {
        //    typeNames[typeIndex] = t.simplifiedFullName;
        //    typeIndex++;
        //}
        //positionOffset = Vector2.zero;
        //zoomLevel = 0;

        Reset();
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

        skin = ScriptableObject.CreateInstance<GUISkin>();
        skin.box = style;
        skin.label = style;
        skin.font = style.font;

        isStyleInit = true;
    }

    [MenuItem("Window/Michael Camara/Dependency Window")]
    public static void Init() {
        Debug.LogError("INITTING WINDOW");
        window = (DependencyWindow) EditorWindow.GetWindow<DependencyWindow>();
        window.Show();
        window.minSize = new Vector2(600, 600);
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

        index = EditorGUI.Popup(new Rect(0, 0, position.width, 20), "Root:", index, typeNames);
        if (index != prevIndex) {
            Reset();
            prevIndex = index;
        }

        DrawNodes();


        horizontalSplitView.Split();

        verticalSplitView.BeginSplitView();

        GUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical();

        GUILayout.FlexibleSpace();

        GUILayout.Label("Centered text");

        GUILayout.FlexibleSpace();

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        GUILayout.EndHorizontal();

        verticalSplitView.Split();
        GUILayout.BeginHorizontal();
        GUILayout.Label("Centered text");
        GUILayout.EndHorizontal();
        verticalSplitView.EndSplitView();
        horizontalSplitView.EndSplitView();

        Repaint();

        Debug.LogError("ENDFRAME");
    }

    void Reset() {

        isFirstFrameDrawn = true;
        maxNodesPerLevel = 0;
        yPad = 0;
        nodesPerLevel = new Dictionary<int, int>();
        Dictionary<string, CustomType>.ValueCollection customTypes = analyzer.GetAllCustomTypes();
        typeNames = new string[customTypes.Count];
        int typeIndex = 0;
        foreach (CustomType t in customTypes) {
            typeNames[typeIndex] = t.simplifiedFullName;
            typeIndex++;
        }
        positionOffset = Vector2.zero;
        zoomLevel = 0;

        CustomType chosenType = DependencyAnalyzer.GetCustomTypeFromString(typeNames[index]);
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
        }

        return allNodes;
    }


    void DrawNodes() {
        GUI.color = Color.cyan;

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
                GUI.Box(node.rect, node.type.simplifiedFullName, style);

                if(node.parent != null) {

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
