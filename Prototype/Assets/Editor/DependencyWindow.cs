using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DependencyWindow : EditorWindow {

	// Use this for initialization
	void Start () {
        Init();


	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private static DependencyWindow window;
    private static DependencyAnalyzer analyzer;
    static Texture nodeTexture;
    public Rect m_backgroundRect;
    bool isInit;

    [MenuItem("Window/Michael Camara/Dependency Window")]
    public static void Init() {

        window = (DependencyWindow) EditorWindow.GetWindow<DependencyWindow>();
        window.Show();
        window.minSize = window.maxSize = new Vector2(600, 600);
        nodeTexture = Resources.Load<Texture>("SquareNode");
        Debug.LogWarning("This tex: " + nodeTexture);
        //Background background = new Background();

        //analyzer = UnityEngine.MonoBehaviour.FindObjectOfType<DependencyAnalyzer>();

        analyzer = new DependencyAnalyzer();
        
    }

    DependencyWindow() {
        m_backgroundRect = new Rect(0, 0, 500, 500);
    }

    void DrawBox() {



        // Arbitrarily choose ClassA as root
        // TODO allow user to browse through files/classes

        //foreach(KeyValuePair<Type, HashSet<Type>> pair in analyzer.dependencyTable) {
        //    Debug.LogWarning(pair);
        //}
        
        DrawTreeFromType(typeof(ClassA), new Vector2(50, window.position.height / 2 - 25), 0);
        //DrawTreeFromType(), new Vector2(50, window.position.height / 2), 0);
    }

    void DrawTreeFromType(Type type, Vector2 position, int level) {

        // Debug.LogWarning("DepTable count: " + analyzer.dependencyTable.Count + ", numKeys" + analyzer.dependencyTable.Keys.Count); 
        //TODO Could instead use texture for background material

        float boxWidth = 50;
        float boxHeight = 50;
        Rect classNode = new Rect(position.x, position.y, boxWidth, boxHeight);
        //Handles.color = Color.cyan;
        GUI.color = Color.cyan;
        GUI.Box(classNode, type.Name);

        if (!analyzer.dependencyTable.ContainsKey(type) || level >= 6) { 
            return;
        }


        HashSet<Type> dependencies = analyzer.dependencyTable[type];
        

        int numDependencies = dependencies.Count;
        Debug.Log("Curr type: " + type + ", Num deps:" + numDependencies);
        float stepHeight = window.position.height / (numDependencies + 1);

        // TODO change from arbitrary width
        //Vector2 stepVector = new Vector2(100, stepHeight);
        //Vector2 stepVector = new Vector2(100, stepHeight);

        float stepWidth = 150.0f;

        //Rect classNode = new Rect(50, 50, 50, 50);


        float i = (numDependencies - 1) / -2.0f;
        foreach(Type t in dependencies) {
            Debug.Log("Iterating dependencies of " + type + ", Current = " + t);
            //Vector2 newPos = position + stepWidth + stepHeight * i;
            Vector2 newPos = new Vector2(position.x + stepWidth, position.y + stepHeight * i);
            Debug.Log("New pos = " + newPos);
            DrawTreeFromType(t, newPos, level + 1);
            i++;
            Debug.Log("Now drawing type: " + t);

            Handles.color = Color.black;
            Handles.DrawLine(position + new Vector2(boxWidth, boxHeight / 2), newPos + new Vector2(0, boxHeight / 2));
        }
        
    }

    private void OnDisable() {
        isInit = false;
        Debug.LogWarning("Window disabled!!");
    }

    private void OnEnable() {
        Debug.LogWarning("ENABLEEEEED");
    }

    void OnGUI() {
        if (!isInit) {
            Init();
            isInit = true;
        }
        else {
            GUISkin skin = GUI.skin;
            //GUI.skin = null;
            Color defaultColor = GUI.color;
            //GUI.color = new Color(.102f, .102f, .102f);
            GUI.color = new Color(0.5f, 0, 0);

            //GUI.Box(m_backgroundRect);
            GUI.color = defaultColor;
            GUI.color = Color.blue;

            //DrawLines(offset);
            //GUI.skin = skin;
            GUI.color = Color.gray;
            GUI.color = new Color(150, 150, 150);
            //Debug.LogWarning(window);
            m_backgroundRect = new Rect(0, 0, window.position.width, window.position.height);
            GUI.Box(m_backgroundRect, "");

            //Handles.color = Color.red;
            //Handles.DrawLine(Vector3.zero, new Vector3(500, 500));
            DrawBox();

            //TODO find way to scale
            //if (Input.GetKeyDown(KeyCode.Space)) {
            //    Debug.LogWarning("OMG");
            //}

            //try {

            //    graphOffset = Vector2.Lerp(graphOffset, targetGraphOffset, .05f);
            //    GUI.skin = skin;





            //    if (m_forceData != null)
            //        if (m_forceData.m_withinThreshold) {
            //            if (!saveChecked) {
            //                if (!Serializer.CurrentFileExists()) {
            //                    Serializer.Serialize(m_visualNodes, m_visualEdges, graphOffset, out saveState);
            //                    _settingsPage.SaveSettings();
            //                }
            //                saveChecked = true;
            //            }
            //            else {
            //                Repaint();
            //                saveChecked = false;

            //            }
            //        }
            //    DrawScriptDataElements();
            //}
            //catch {
            //    Init();
            //}

            //Init();
            try {
                //DrawUIElements();
            }
            catch {

            }
        }
    }

}
