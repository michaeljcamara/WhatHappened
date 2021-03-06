﻿// Script originally authored by UnityList contributor, miguel12345
// https://unitylist.com/p/210/Editor-GUI-Split-View
// Modified slightly by Michael Camara

using UnityEngine;
using System.Collections;
using UnityEditor;

public class EditorGUISplitView
{

	public enum Direction {
		Horizontal,
		Vertical
	}

	Direction splitDirection;
    float _splitNormalizedPosition;
    public float splitNormalizedPosition { get { return _splitNormalizedPosition; } set { _splitNormalizedPosition = value; } }
    //public float splitNormalizedPosition { get { return _splitNormalizedPosition; } }
    bool resize;
	public Vector2 scrollPosition;
	Rect availableRect;

    public override string ToString() {
        return ("Normalized: " + _splitNormalizedPosition + ", availRect: " + availableRect + ", ScrollPos: " + scrollPosition);
    }

    public EditorGUISplitView(Direction splitDirection) {
		_splitNormalizedPosition = 0.5f;
		this.splitDirection = splitDirection;
	}

	public void BeginSplitView() {
		Rect tempRect;

		if(splitDirection == Direction.Horizontal)
			tempRect = EditorGUILayout.BeginHorizontal (GUILayout.ExpandWidth(true));
		else 
			tempRect = EditorGUILayout.BeginVertical (GUILayout.ExpandHeight(true));
		
		if (tempRect.width > 0.0f) {
			availableRect = tempRect;
		}
		if(splitDirection == Direction.Horizontal)
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(availableRect.width * _splitNormalizedPosition));
		else
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(availableRect.height * _splitNormalizedPosition));
	}

	public void Split() {
		GUILayout.EndScrollView();
		ResizeSplitFirstView ();
	}

	public void EndSplitView() {

		if(splitDirection == Direction.Horizontal)
			EditorGUILayout.EndHorizontal ();
		else 
			EditorGUILayout.EndVertical ();
	}

	private void ResizeSplitFirstView(){

		Rect resizeHandleRect;

		if(splitDirection == Direction.Horizontal)
			resizeHandleRect = new Rect (availableRect.width * _splitNormalizedPosition, availableRect.y, 2f, availableRect.height);
		else
			resizeHandleRect = new Rect (availableRect.x,availableRect.height * _splitNormalizedPosition, availableRect.width, 2f);

		GUI.DrawTexture(resizeHandleRect,EditorGUIUtility.whiteTexture);

		if(splitDirection == Direction.Horizontal)
			EditorGUIUtility.AddCursorRect(resizeHandleRect,MouseCursor.ResizeHorizontal);
		else
			EditorGUIUtility.AddCursorRect(resizeHandleRect,MouseCursor.ResizeVertical);

		if( Event.current.type == EventType.MouseDown && resizeHandleRect.Contains(Event.current.mousePosition)){
			resize = true;
		}
		if(resize){
			if(splitDirection == Direction.Horizontal)
				_splitNormalizedPosition = Event.current.mousePosition.x / availableRect.width;
			else
				_splitNormalizedPosition = Event.current.mousePosition.y / availableRect.height;
		}
		if(Event.current.type == EventType.MouseUp)
			resize = false;        
	}
}

