using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomTypeNode {

    private Rect _rect;
    public Rect rect { get { return _rect; }
    set {
            _rect = value;
            leftAnchor = rect.position + new Vector2(0, rect.height / 2f);
            rightAnchor = rect.position + new Vector2(rect.width, rect.height / 2f);
        }
    }
    public Vector2 pos;
    public CustomType type;
    public int level;
    private bool _isCyclic;
    public bool isCyclic { get { return _isCyclic; } }

    private CustomTypeNode _parent;
    public CustomTypeNode parent { get { return _parent; } }

    public Vector3 leftAnchor;
    public Vector3 rightAnchor;

    public Vector3[] GetArrow() {
        Vector3[] anotherArrow = new Vector3[3];

        float angle = Mathf.Atan2(leftAnchor.y - parent.rightAnchor.y, leftAnchor.x - parent.rightAnchor.x) * 180 / Mathf.PI; //eh works
        //float angle = Mathf.Atan2(Mathf.Abs(leftAnchor.y - parent.rightAnchor.y), Mathf.Abs(leftAnchor.x - parent.rightAnchor.x)) * -180 / Mathf.PI; //eh works
        //float angle = Mathf.Atan2(parent.rightAnchor.y - leftAnchor.y, parent.rightAnchor.x - leftAnchor.x) * 180 / Mathf.PI;
        //angle = Mathf.Abs(angle);
        //source Mathf.Atan2(vec2.y - vec1.y, vec2.x - vec1.x);

        Debug.LogWarning("Angle bw " + parent.type + " to " + type + ": " + angle + ";; Parent: " + parent.rightAnchor + ", Node: " + leftAnchor);

        Vector3 point1 = new Vector3(leftAnchor.x - rect.width / 4f, leftAnchor.y - rect.height / 4f, 0);
        Vector3 point2 = new Vector3(leftAnchor.x - rect.width / 4f, leftAnchor.y + rect.height / 4f, 0);

        Vector3 dir1 = point1 - leftAnchor;
        dir1 = Quaternion.Euler(new Vector3(0, 0, angle)) * dir1;
        point1 = dir1 + leftAnchor;

        Vector3 dir2 = point2 - leftAnchor;
        dir2 = Quaternion.Euler(new Vector3(0, 0, angle)) * dir2;
        point2 = dir2 + leftAnchor;


        //point1.x = Mathf.Cos(angle) * (point1.x - leftAnchor.x) - Mathf.Sin(angle) * (point1.y - leftAnchor.y) + leftAnchor.x;
        //point1.y = Mathf.Sin(angle) * (point1.x - leftAnchor.x) + Mathf.Cos(angle) * (point1.y - leftAnchor.y) + leftAnchor.y;

        //point2.x = Mathf.Cos(angle) * (point2.x - leftAnchor.x) - Mathf.Sin(angle) * (point2.y - leftAnchor.y) + leftAnchor.x;
        //point2.y = Mathf.Sin(angle) * (point2.x - leftAnchor.x) + Mathf.Cos(angle) * (point2.y - leftAnchor.y) + leftAnchor.y;

        //float x1 = Mathf.Cos(angle) * (-rect.width / 4f) - Mathf.Sin(angle) * (-rect.height / 4f) + leftAnchor.x;
        //float y1 = Mathf.Sin(angle) * (-rect.width / 4f) + Mathf.Cos(angle) * (-rect.height / 4f) + leftAnchor.y;

        //float x2 = Mathf.Cos(angle) * (-rect.width / 4f) - Mathf.Sin(angle) * (rect.height / 4f) + leftAnchor.x;
        //float y2 = Mathf.Sin(angle) * (-rect.width / 4f) + Mathf.Cos(angle) * (rect.height / 4f) + leftAnchor.y;

        /*
         * ox = pivotPoint
            p'x = cos(theta) * (px-ox) - sin(theta) * (py-oy) + ox

            p'y = sin(theta) * (px-ox) + cos(theta) * (py-oy) + oy
         */

        //anotherArrow[0] = leftAnchor;
        //anotherArrow[1] = new Vector3(x1, y1, 0);
        //anotherArrow[2] = new Vector3(x2, y2, 0);

        //return anotherArrow;
        return new Vector3[] { leftAnchor, point1, point2 };
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
