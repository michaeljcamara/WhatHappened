using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class DependencyWindow : EditorWindow {

	// Use this for initialization
	void Start () {
        Init();

        //style = new GUIStyle();
        style = new GUIStyle(GUI.skin.box);

        GUIStyleState styleState = new GUIStyleState();
        styleState.textColor = Color.yellow;

        //style.border = new RectOffset(50, 50, 50, 50);
        //style.padding = new RectOffset(50, 50, 50, 50);
        //style.onNormal = styleState;
        //style.onActive = styleState;
        //style.onFocused = styleState;
        //style.normal = styleState;
        //style.focused = styleState;
        //style.active = styleState;
        
        
        style.font = Resources.Load<Font>("JAi_____");
        //Debug.LogWarning(style.font.fontSize + ", " + style.font.name);
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;
        //style.stretchHeight = true;
        //style.stretchWidth = true;

        //GUI.skin.label = style;
        //GUI.skin.box = style;
        //GUI.skin.font = style.font;

        //skin = new GUISkin(); 
        skin = ScriptableObject.CreateInstance<GUISkin>();
        skin.box = style;
        skin.label = style;
        skin.font = style.font;

               
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    private static DependencyWindow window;
    private static DependencyAnalyzer analyzer;
    static Texture nodeTexture;
    public Rect m_backgroundRect;
    bool isInit;
    int index;

    public GUIStyle style;
    public GUISkin skin;

    [MenuItem("Window/Michael Camara/Dependency Window")]
    public static void Init() {

        window = (DependencyWindow) EditorWindow.GetWindow<DependencyWindow>();
        window.Show();
        //window.minSize = window.maxSize = new Vector2(600, 600);
        window.minSize = new Vector2(600, 600); 

        //window.minSize = new Vector2(-2000, -2000);
        
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

        //**MOVE THIS TO ONGUI...CLICKED???
        List<string> keyNames = new List<string>(analyzer.dependencyTable.Count);
        foreach (Type t in analyzer.dependencyTable.Keys) {
            keyNames.Add(t.Name);
        }

        index = EditorGUI.Popup(new Rect(0, 0, position.width, 20), "Root:", index, keyNames.ToArray());
        //Assembly assembly = Assembly.GetExecutingAssembly();

        Assembly assembly = analyzer.assembly;
        Type chosenType = assembly.GetType(keyNames[index]);
        //foreach(Type t in assembly.GetTypes()) {
        //    Debug.Log("Iterating assembly types: " + t + ";; name is: " + t.Name);
        //}
        //Debug.LogWarning("Chosen Type = " + chosenType);


        //Maybe use textures for boxes, easier to scale???
        //TODO: How to allow pan left, after zooming in, when elements off the LEFT side
        //Rect scaledRect = new Rect(positionOffset.x, positionOffset.y, window.position.width * 3, window.position.height * 3); // Adding *3 mult expands draw space, otherwise cuts off
        //**THIS ALLOWS PANNING FOR OFFSCREEN ELEMENTS
        //TODO figure out how far can expand window in neg dir....inefficient? Other way? Keep track of arbitrary -500 val
        //Rect scaledRect = new Rect(-500 + positionOffset.x, positionOffset.y, window.position.width * 3, window.position.height * 3); // Adding *3 mult expands draw space, otherwise cuts off
        Rect scaledRect = new Rect(-50 * Mathf.Abs(zoomLevel) + positionOffset.x, -50 * Mathf.Abs(zoomLevel) + positionOffset.y, window.position.width * 10, window.position.height * 10); // Adding *3 mult expands draw space, otherwise cuts off
        window.BeginWindows(); // Need this to allow panning while window is docked, no visible difference otherwise(?)
        
        GUI.BeginGroup(scaledRect);

        //TODO allow user selection
        //DrawTreeFromType(typeof(ClassA), new Vector2(50, window.position.height / 2 - 25), 0);
        DrawTreeFromType(chosenType, new Vector2(50, window.position.height / 2 - 25), 0);
        //Handles.DrawLine(new Vector2(window.position.width / 2, 0), new Vector2(window.position.width / 2, window.position.height)); // draw center line vert
        GUI.EndGroup();
        window.EndWindows();


    }

    //void DrawTreeFromType(Type type, Vector2 position, int level) {
    Vector2 DrawTreeFromType(Type type, Vector2 position, int level) {

        // Debug.LogWarning("DepTable count: " + analyzer.dependencyTable.Count + ", numKeys" + analyzer.dependencyTable.Keys.Count); 
        //TODO Could instead use texture for background material

        //float boxWidth = 50;
        //float boxHeight = 50;


        float boxWidth = 50 + 50 * (zoomeScaleStep * zoomLevel);
        float boxHeight = 50 + 50 * (zoomeScaleStep * zoomLevel);

        //position.x *= scale.x;
        //position.y *= scale.y;

        // Center is center
        float windowCenterX = window.position.width / 2 - boxWidth / 2;
        float windowCenterY = window.position.height / 2 - boxHeight / 2;

        //Center is upperLeft
        //float windowCenterX = 1;
        //float windowCenterY = 1;

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

        //position.x = position.x + (window.position.center.x - position.x) * scale.x;
        //position.x = position.x - ((position.x - window.position.center.x) / window.position.center.x) * zoomPositionStep * zoomLevel;
        //position.x = position.x + ((position.x + boxWidth/2 - windowCenterX) / windowCenterX) * zoomPositionStep * zoomLevel;

        //position.x = position.x + ((position.x - (windowCenterX - boxWidth / 2)) / (windowCenterX - boxWidth / 2)) * zoomPositionStep * zoomLevel; 

        //position.y *= scale.y;

        //if left of center, move left ; if right of center, move right
        // if up of center, move up; if down of center, move down
        // magnitude relative to center closeness? use window.position.center?

        //Rect classNode = new Rect(position.x, position.y, boxWidth, boxHeight);
        //TODO REM to adjust this based on mouse cursor position, eventually
        //TODO: only zoom down/right so don't get cutoff???
        float x = position.x + (position.x - windowCenterX) / windowCenterX * zoomPositionStep * zoomLevel;
        float y = position.y + (position.y - windowCenterY) / windowCenterY * zoomPositionStep * zoomLevel;
        ///x += 500;
        x += 50 * Mathf.Abs(zoomLevel);
        y += 50 * Mathf.Abs(zoomLevel);
        //float x = position.x  * zoomLevel * zoomeScaleStep + position.x;
        //float y = position.y  * zoomLevel * zoomeScaleStep + position.y;
        //REM by curose position! might change formula
        //FIGURE OUT HOW TO PAN TO ELEMENTS THAT WERE DRAWN OFFSCREEN

        Rect classNode = new Rect(x, y, boxWidth, boxHeight);

        GUI.color = Color.cyan;

        //GUI.Box(classNode, type.Name);
        GUI.Box(classNode, type.Name, style); //incl style
        //GUI.Box(new Rect(0, 0, 100, 100), "SHSHSHS");
        
        //TODO IF Cyclic dependency (this node same as node "up the tree to the root", then different edge color and STOP)
        if (!analyzer.dependencyTable.ContainsKey(type) || level >= 6) {
            //return;
            //return Vector2.zero;
            return new Vector2(x, y);
        }


        HashSet<Type> dependencies = analyzer.dependencyTable[type];
        

        int numDependencies = dependencies.Count;
        //Debug.Log("Curr type: " + type + ", Num deps:" + numDependencies);
        float stepHeight = window.position.height / (numDependencies + 1);

        // TODO change from arbitrary width
        //Vector2 stepVector = new Vector2(100, stepHeight);
        //Vector2 stepVector = new Vector2(100, stepHeight);

        float stepWidth = 150.0f;

        //Rect classNode = new Rect(50, 50, 50, 50);


        float i = (numDependencies - 1) / -2.0f;
        foreach(Type t in dependencies) {
            //Vector2 newPos = position + stepWidth + stepHeight * i;
            Vector2 newPos = new Vector2(position.x + stepWidth, position.y + stepHeight * i);
            //DrawTreeFromType(t, newPos, level + 1);
            Vector2 drawnVector = DrawTreeFromType(t, newPos, level + 1);
            i++;

            Handles.color = Color.black;
            //Handles.DrawLine(position + new Vector2(boxWidth, boxHeight / 2), newPos + new Vector2(0, boxHeight / 2));
            //TODO cleaner way of saying where the lines should connect between boxes
            Handles.DrawLine(new Vector2(x + boxWidth, y + boxHeight/2), drawnVector + new Vector2(0, boxHeight/2));
            //Debug.Log("Drawline for " + type + " from: " + x + "," + y + "   TO   " + drawnVector);
        }

        return new Vector2(x, y);
        
    }

    private void OnDisable() {
        isInit = false;
        Debug.LogWarning("Window disabled!!");
    }

    private void OnEnable() {
        Debug.LogWarning("ENABLEEEEED");
    }
    private Vector2 scale;
    private Vector2 pivotPoint;
    private Vector2 pivotOffset = Vector2.zero;
    private Vector2 positionOffset = Vector2.zero;

    private float panStep = 50;
    private float zoomPositionStep = 20f;
    private float zoomeScaleStep = 0.1f;
    private float zoomLevel = 0;
    void OnGUI() {
        if (!isInit) {
            Start();
            //Init();
            isInit = true;
            scale = new Vector2(1, 1);
            //scale = new Vector2(0.99f, 0.99f);
            pivotPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
            pivotOffset = Vector2.zero;
            positionOffset = Vector2.zero;
            zoomLevel = 0;
        }
        else {
            if (Event.current.isMouse) { // still captures mouse up, down, and DRAG. Not mouse wheel
                                         //Debug.Log("MOUSE!");
                                         //style.fontSize--;
                                         //Repaint();
                                         //TODO: Need other way to scale EVERYTHING at SAME TIME by SAME AMOUNT; so match font size decrease with box size decrease, keep w/in frames
                                         //REM: Need repaint since otherwise onGUI doesnt refresh every frame

                //Debug.Log("MousePos: " + Event.current.mousePosition);
                
                //EditorGUIUtility.ScaleAroundPivot(new Vector2(0.5f, 0.5f), Vector2.zero);
                //GUIUtility.ScaleAroundPivot(new Vector2(0.5f, 0.5f), new Vector2(Screen.width / 2.0f, Screen.height / 2.0f));
                //EditorGUI.DrawRect;
                //EditorGUILayout.BeginHorizontal();
                //EditorGUILayout.ToggleGroupScope;
            }
             
            if(Event.current.type == EventType.ScrollWheel) {
                if(Event.current.delta.y > 0) {
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

                // Prevent scale being negative, which would invert image
                //scale = Vector2.Max(Vector2.zero, scale);
                scale = Vector2.Max(new Vector2(0.09f,0.09f), scale);

                //TODO Figure out how to cleanly pivot zoom around mouse pos
                //pivotPoint = Event.current.mousePosition;

                //TODO how to draw boxes off screen, such that scale will show when zoom out
            }

            //Rem check if DOWN, not on release
            //REM change pan amount based on scale??

            //TODO TEST THIS WITH DOCKED WINDOW!!
            if(Event.current.type == EventType.KeyDown) {
                switch(Event.current.keyCode) {
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


            //// This whites-out any pixels not displayed on screen at time of painting
            //pivotPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
            //GUIUtility.ScaleAroundPivot(scale, pivotPoint);



            //GUIUtility.ScaleAroundPivot(scale, Event.current.mousePosition);



            //pivotPoint = new Vector2(Screen.width / 2, Screen.height / 2);
            //GUIUtility.ScaleAroundPivot(scale, pivotPoint);
            //if (GUI.Button(new Rect(Screen.width / 2 - 25, Screen.height / 2 - 25, 50, 50), "Big!"))
            //    scale += new Vector2(0.5F, 0.5F);


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

            //GUI.color = Color.blue;
            //GUI.backgroundColor = Color.blue;
            //GUI.contentColor = Color.blue;

            //Debug.LogWarning(window);
            m_backgroundRect = new Rect(0, 0, window.position.width, window.position.height);
            //m_backgroundRect = new Rect(0, 0, window.position.width * 2, window.position.height * 2);

            GUI.color = Color.gray;
            GUI.Box(m_backgroundRect, "");

            //pivotPoint = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
            //GUIUtility.ScaleAroundPivot(scale, pivotPoint + pivotOffset); // only works with offset if scale != 1

            //Handles.color = Color.red;
            //Handles.DrawLine(Vector3.zero, new Vector3(500, 500));
            
            DrawBox();

            //GUILayout.;
            //GUILayoutUtility.GetRect();

            //TODO Use this for boxes, jump to C# Script?
            //GUILayout.BeginArea(new Rect(10, 10, 100, 100));
            //GUILayout.Button("Click me");
            //GUILayout.Button("Or me");
            //GUILayout.EndArea();

            //Think button, flexibleHorizSpace, then vertical column of buttons
            //Style guidelines
            //GUIStyle asd = new GUIStyle();
            //asd.stretchHeight;
            //GUI.skin.box = asd;



            //EditorWindow.focusedWindow  // Good to have when checking inputs
            //if (Event.current.keyCode == KeyCode.mou)
            //    ;
            //if (Event.current.type == EventType.) ;

            Repaint();

            //GUI.BeginGroup(new Rect(Screen.width / 2 - 400, Screen.height / 2 - 300, 800, 600));
            //GUI.Box(new Rect(0, 0, 800, 600), "This box is now centered! - here you would put your main menu");
            //GUI.EndGroup();

            //GUI.BeginGroup(new Rect(window.position.x, window.position.y, window.position.width, window.position.height));
            //GUI.BeginGroup(new Rect(0,0, window.position.width, window.position.height));
            ////GUI.Box(new Rect(0, 0, window.position.width, window.position.height), "This box is now centered! - here you would put your main menu");
            //GUI.Box(new Rect(0, 0, window.position.width, window.position.height), "This box is now centered! - here you would put your main menu", style);
            //GUI.EndGroup();




            //Debug.Log("Window x, y, width, height: " + window.position.x + ", " + window.position.y + ", " + window.position.width + ", " + window.position.height);
            //Debug.Log("Screen width, height: " + Screen.width + ", " + Screen.height);

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
