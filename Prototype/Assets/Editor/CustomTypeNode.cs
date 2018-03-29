using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTypeNode {

    public Rect rect;
    public Vector2 pos;
    public CustomType type;
    public int level;
    private bool _isCyclic;
    public bool isCyclic { get { return _isCyclic; } }

    private CustomTypeNode _parent;
    public CustomTypeNode parent { get { return _parent; } }

    public CustomTypeNode(CustomType type, Vector2 pos, Rect rect) {
        this.rect = rect;
        this.pos = pos;
        this.type = type;
    }

    public CustomTypeNode(CustomType type, CustomTypeNode parent, int level) {
        this.type = type;
        this.level = level;
        this._parent = parent;
    }

    public void SetCyclic(bool isTrue) {
        _isCyclic = isTrue;
    }

    public void SetParent(CustomTypeNode p) {
        _parent = p;
    }

}
