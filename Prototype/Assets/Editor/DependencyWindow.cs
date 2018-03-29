﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;

public class DependencyWindow : EditorWindow {
   
    private string[] typeNames;
    private Vector2 scale;
    private Vector2 pivotPoint;
    private Vector2 pivotOffset = Vector2.zero;
    private Vector2 positionOffset = Vector2.zero;

    private float panStep = 50;
    private float zoomPositionStep = 20f;
    private float zoomeScaleStep = 0.1f;
    private float zoomLevel = 0;

    private static DependencyWindow window;
    private DependencyAnalyzer analyzer;
    public Rect m_backgroundRect;
    bool isStyleInit;
    int index;
    private GUIStyle style;
    private GUISkin skin;

    EditorGUISplitView horizontalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Horizontal);
    EditorGUISplitView verticalSplitView = new EditorGUISplitView(EditorGUISplitView.Direction.Vertical);

    //TODO
    int maxNodesPerLevel;
    Dictionary<int, int> nodesPerLevel;
    CustomTypeNode selectedNode;
    Dictionary<int, LinkedList<CustomTypeNode>> allNodes;

    private void Awake() {
        Debug.LogWarning("NOW AWAKE!!");
    }

    private void OnEnable() {
        Debug.LogWarning("ENABLEEEEED");
        Debug.Log("STYLE BEFORE INIT: " + style);

        analyzer = new DependencyAnalyzer();

        nodesPerLevel = new Dictionary<int, int>();

        //**MOVE THIS TO ONGUI...CLICKED???
        Dictionary<string, CustomType>.ValueCollection customTypes = analyzer.GetAllCustomTypes();
        typeNames = new string[customTypes.Count];
        int typeIndex = 0;
        foreach (CustomType t in customTypes) {
            typeNames[typeIndex] = t.simplifiedFullName;
            typeIndex++;
        }

        scale = new Vector2(1, 1);
        //scale = new Vector2(0.99f, 0.99f);
        pivotPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        pivotOffset = Vector2.zero;
        positionOffset = Vector2.zero;
        zoomLevel = 0;

        //TODO DELETE
        CustomType chosenType = DependencyAnalyzer.GetCustomTypeFromString(typeNames[index]);
        allNodes = CreateDependencyTree(chosenType, null, new LinkedList<CustomType>(), new Dictionary<int, LinkedList<CustomTypeNode>>(), 0);

        
        int nodeCount = 0;
        foreach(KeyValuePair<int, LinkedList<CustomTypeNode>> pair in allNodes) {
            if(pair.Value.Count > maxNodesPerLevel) {
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
                scale -= new Vector2(0.1f, 0.1f);
                zoomLevel--;
                Debug.LogWarning("ZOOM OUT");
            }
            else {
                scale += new Vector2(0.1f, 0.1f);
                zoomLevel++;
                Debug.LogWarning("ZOOM IN");
            }

            Debug.Log("Zoom level = " + zoomLevel);

            //TODO Prevent scale being negative, which would invert image
            scale = Vector2.Max(new Vector2(0.09f, 0.09f), scale);

            //TODO Figure out how to cleanly pivot zoom around mouse pos
            //pivotPoint = Event.current.mousePosition;
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
                    scale += new Vector2(0.1f, 0.1f);
                    zoomLevel++;
                    Debug.LogWarning("ZOOM IN");
                    break;
                case KeyCode.PageDown:
                    scale -= new Vector2(0.1f, 0.1f);
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

        //RectTransform dd = new RectTransform();

        //EditorGUIUtility.ScaleAroundPivot(new Vector2(zoomLevel, zoomLevel), Vector2.zero);
        DrawBox();
        //EditorGUIUtility.ScaleAroundPivot(Vector2.one, Vector2.one);
        

        //Debug.LogWarning("SplitPos BEFORE: " + horizontalSplitView.ToString() + horizontalSplitView.scrollPosition);
        Debug.LogWarning("SplitPos BEFORE: " + horizontalSplitView.ToString());
        horizontalSplitView.Split();
        //Debug.LogWarning("SplitPos AFTER: " + horizontalSplitView.scrollPosition);

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
    }


    void DrawBox() {
        //TODO USE GROUPS TO ALLOW PANNING GROUP!!!!

        index = EditorGUI.Popup(new Rect(0, 0, position.width, 20), "Root:", index, typeNames);
        CustomType chosenType = DependencyAnalyzer.GetCustomTypeFromString(typeNames[index]);

        DrawNodes();

        ////COMMENT THIS
        ////int maxNodes = GetMaxNodesPerLevel(chosenType);

        ////TODO: How to allow pan left, after zooming in, when elements off the LEFT side
        ////Rect scaledRect = new Rect(positionOffset.x, positionOffset.y, window.position.width * 3, window.position.height * 3); // Adding *3 mult expands draw space, otherwise cuts off
        ////**THIS ALLOWS PANNING FOR OFFSCREEN ELEMENTS
        ////TODO figure out how far can expand window in neg dir....inefficient? Other way? Keep track of arbitrary -500 val
        ////Rect scaledRect = new Rect(-500 + positionOffset.x, positionOffset.y, window.position.width * 3, window.position.height * 3); // Adding *3 mult expands draw space, otherwise cuts off
        //Rect scaledRect = new Rect(-50 * Mathf.Abs(zoomLevel) + positionOffset.x, -50 * Mathf.Abs(zoomLevel) + positionOffset.y, window.position.width * 10, window.position.height * 10); // Adding *3 mult expands draw space, otherwise cuts off
        //window.BeginWindows(); // Need this to allow panning while window is docked, no visible difference otherwise(?)
        
        //GUI.BeginGroup(scaledRect);

        //DrawTreeFromType(chosenType, new Vector2(50, window.position.height / 2 - 25), 0);
        //GUI.EndGroup();
        //window.EndWindows();
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
    Vector2 scrollPosition = Vector2.zero;
    void DrawNodes() {

        Debug.Log("Before Window pos:" + window.position);
        //TODO adjust this so top doesnt get cut off!
        //Rect scaledRect = new Rect(-50 * Mathf.Abs(zoomLevel) + positionOffset.x, -50 * Mathf.Abs(zoomLevel) + positionOffset.y, window.position.width * 10, window.position.height * 10); // Adding *3 mult expands draw space, otherwise cuts off
        Rect scaledRect = new Rect(-5000 + positionOffset.x, -5000 + positionOffset.y, 10000, 10000); // Adding *3 mult expands draw space, otherwise cuts off
        GUI.color = Color.cyan;
        //window.BeginWindows(); // Need this to allow panning while window is docked, no visible difference otherwise(?)

        //GUI.BeginGroup(scaledRect);
        //GUI.BeginScrollView(scaledRect, new Vector2(100, 100), new Rect(0, 0, 1000, 1000));

        //window.BeginWindows(); // Need this to allow panning while window is docked, no visible difference otherwise(?)
        scrollPosition = GUI.BeginScrollView(new Rect(0, 0, window.position.width * horizontalSplitView.splitNormalizedPosition, window.position.height), scrollPosition, new Rect(0, 0, 3000, 3000));
        //EditorGUIUtility.ScaleAroundPivot(new Vector2(zoomLevel, zoomLevel), Vector2.zero);

        float boxWidth = 50 + 50 * (zoomeScaleStep * zoomLevel);
        float boxHeight = 50 + 50 * (zoomeScaleStep * zoomLevel);
        //float boxWidth = 50;
        //float boxHeight = 50;

        float maxY = (window.position.height / 2 - boxHeight / 2) - ((maxNodesPerLevel - 1) / 2.0f * boxHeight);
        Vector2 start = new Vector2(50, window.position.height / 2 - boxHeight / 2);

        maxY = 0;

        //TODO CHANGE TO FOREACH, does it matter order of level
        //for (int i = 0; i < allNodes.Keys.Count; i++) {
        foreach (KeyValuePair<int, LinkedList<CustomTypeNode>> pair in allNodes) {
            int level = pair.Key;
            int numNodes = pair.Value.Count;

            float xStep = 150f;
            //float yStep = ((maxNodesPerLevel * 2 - 1 - numNodes) / (numNodes - 1.0f)) * boxHeight;
            //float yStep = ((maxNodesPerLevel * 2 - 1) - numNodes) / ((float) numNodes + 1.0f) * boxHeight;
            float yStep = ((maxNodesPerLevel * 2 - 1) - numNodes) / ((float)numNodes + 1.0f) * 50;

            xStep += xStep * zoomeScaleStep * zoomLevel;
            yStep += yStep * zoomeScaleStep * zoomLevel;
            

            int nodeIndex = 0;
            foreach (CustomTypeNode node in pair.Value) {
                //Debug.Log("Drawing: " + node.type.simplifiedFullName + ", level: " + level + ", Index: " + nodeIndex);
                //node.rect = new Rect(boxWidth + xStep * level, maxY + (nodeIndex * yStep), boxWidth, boxHeight);
                node.rect = new Rect(boxWidth + xStep * level, (nodeIndex + 1) * yStep + (nodeIndex * boxHeight), boxWidth, boxHeight);
                GUI.Box(node.rect, node.type.simplifiedFullName, style);
                nodeIndex++;
            }
        }

        //TODO DRAW LINES TO PARENT



        //Rect classNode = new Rect(x, y, boxWidth, boxHeight);
        //GUI.color = Color.cyan;
        //GUI.Box(classNode, type.simplifiedFullName, style);

        //HashSet<CustomType> dependencies = type.GetDependencies();
        //int numDependencies = dependencies.Count;
        //float stepHeight = window.position.height / (numDependencies + 1);
        //float stepWidth = 150.0f;

        //float index = (numDependencies - 1) / -2.0f;
        //foreach (CustomType t in dependencies) {
        //    Vector2 newPos = new Vector2(position.x + stepWidth, position.y + stepHeight * index);
        //    Vector2 drawnVector = DrawTreeFromType(t, newPos, level + 1);
        //    index++;
        //    Handles.color = Color.black;
        //    //TODO cleaner way of saying where the lines should connect between boxes
        //    Handles.DrawLine(new Vector2(x + boxWidth, y + boxHeight / 2), drawnVector + new Vector2(0, boxHeight / 2));
        //}


        //GUI.Button(new Rect(0, 0, 100, 20), "Top-left");
        //GUI.Button(new Rect(120, 0, 100, 20), "Top-right");
        //GUI.Button(new Rect(0, 180, 100, 20), "Bottom-left");
        //GUI.Button(new Rect(120, 180, 100, 20), "Bottom-right");
        //GUI.EndGroup();
        GUI.EndScrollView();
        //window.EndWindows();
    }

    Vector2 DrawTreeFromType(CustomType type, Vector2 position, int level) {

        float boxWidth = 50 + 50 * (zoomeScaleStep * zoomLevel);
        float boxHeight = 50 + 50 * (zoomeScaleStep * zoomLevel);

        float windowCenterX = window.position.width / 2 - boxWidth / 2;
        float windowCenterY = window.position.height / 2 - boxHeight / 2;

        //BEGIN TEST CENTER
        //GUI.color = Color.red;
        //Rect tempCenterNode = new Rect(windowCenterX - boxWidth / 2, window.position.height / 2 - boxHeight / 2, boxWidth, boxHeight);
        //tempCenterNode.x = tempCenterNode.x + ((tempCenterNode.x - (windowCenterX - boxWidth / 2)) / (windowCenterX - boxWidth / 2)) * zoomPositionStep * zoomLevel;
        //GUI.Box(tempCenterNode, "CENTER", style);
        //GUI.color = Color.cyan;

        //GUI.color = Color.red;
        //Rect tempCenterNodeNear = new Rect(windowCenterX - boxWidth / 2 + 100, window.position.height / 2 - boxHeight / 2, boxWidth, boxHeight);
        //tempCenterNodeNear.x = tempCenterNodeNear.x + ((tempCenterNodeNear.x - (windowCenterX - boxWidth / 2)) / (windowCenterX - boxWidth / 2)) * zoomPositionStep * zoomLevel;
        //GUI.Box(tempCenterNodeNear, "CENTER", style);
        //GUI.color = Color.cyan;

        //GUI.color = Color.red;
        //Rect tempCenterNodeFar = new Rect(windowCenterX - boxWidth / 2 + 300, window.position.height / 2 - boxHeight / 2, boxWidth, boxHeight);
        //tempCenterNodeFar.x = tempCenterNodeFar.x + ((tempCenterNodeFar.x - (windowCenterX - boxWidth / 2)) / (windowCenterX - boxWidth / 2)) * zoomPositionStep * zoomLevel;
        //GUI.Box(tempCenterNodeFar, "CENTER", style);
        //GUI.color = Color.cyan;
        // END TEST CENTER

        //NOTES FOR ZOOMING
        // if left of center, move left ; if right of center, move right
        // if up of center, move up; if down of center, move down
        // magnitude relative to center closeness? use window.position.center?

        //TODO REM to adjust this based on mouse cursor position, eventually
        float x = position.x + (position.x - windowCenterX) / windowCenterX * zoomPositionStep * zoomLevel;
        float y = position.y + (position.y - windowCenterY) / windowCenterY * zoomPositionStep * zoomLevel;
        x += 50 * Mathf.Abs(zoomLevel);
        y += 50 * Mathf.Abs(zoomLevel);

        Rect classNode = new Rect(x, y, boxWidth, boxHeight);  
        GUI.color = Color.cyan;
        GUI.Box(classNode, type.simplifiedFullName, style); //incl style
                                                            //GUI.Box(new Rect(0, 0, 100, 100), "SHSHSHS");

        //TODO PREVENT CYCLIC DEPS
        if (level >= 6) {
            return new Vector2(x, y);
        }

        //CustomTypeNode n = new CustomTypeNode(type, Vector2.zero, classNode);
        

        HashSet<CustomType> dependencies = type.GetDependencies();
        int numDependencies = dependencies.Count;
        float stepHeight = window.position.height / (numDependencies + 1);
        float stepWidth = 150.0f;

        float i = (numDependencies - 1) / -2.0f;
        foreach(CustomType t in dependencies) {
            Vector2 newPos = new Vector2(position.x + stepWidth, position.y + stepHeight * i);
            Vector2 drawnVector = DrawTreeFromType(t, newPos, level + 1);
            i++;
            Handles.color = Color.black;
            //TODO cleaner way of saying where the lines should connect between boxes
            Handles.DrawLine(new Vector2(x + boxWidth, y + boxHeight/2), drawnVector + new Vector2(0, boxHeight/2));
        }

        return new Vector2(x, y);
    }





    

}
