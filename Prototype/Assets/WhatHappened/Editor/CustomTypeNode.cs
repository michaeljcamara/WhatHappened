using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WhatHappened {

    public class CustomTypeNode {

        private Rect _rect;
        public Rect rect {
            get { return _rect; }
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
        float absoluteImpactStrength, normalizedImpactStrength;

        public bool hasChanged;

        private CustomTypeNode _parent;
        public CustomTypeNode parent { get { return _parent; } }

        public Vector3 leftAnchor;
        public Vector3 rightAnchor;

        public Vector3[] GetArrow() {
            float angle = Mathf.Atan2(leftAnchor.y - parent.rightAnchor.y, leftAnchor.x - parent.rightAnchor.x) * 180 / Mathf.PI; //eh works

            //Rotation code adapted from Unity forum user, aldonaletto: https://answers.unity.com/questions/532297/rotate-a-vector-around-a-certain-point.html
            Vector3 point1 = new Vector3(leftAnchor.x - rect.width / 4f, leftAnchor.y - rect.height / 4f, 0);
            Vector3 dir1 = point1 - leftAnchor;
            dir1 = Quaternion.Euler(new Vector3(0, 0, angle)) * dir1;
            point1 = dir1 + leftAnchor;

            Vector3 point2 = new Vector3(leftAnchor.x - rect.width / 4f, leftAnchor.y + rect.height / 4f, 0);
            Vector3 dir2 = point2 - leftAnchor;
            dir2 = Quaternion.Euler(new Vector3(0, 0, angle)) * dir2;
            point2 = dir2 + leftAnchor;

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

        public float CalculateAbsoluteImpactStrength(int totalChangesInTree, int numLevelsInTree) {
            int linesOfCodeChanged = type.totalLineChanges;
            int distanceFromRoot = level;

            absoluteImpactStrength = (linesOfCodeChanged / (float)totalChangesInTree) * ((numLevelsInTree - distanceFromRoot) / (float) numLevelsInTree);

            //Debug.Log("TotalChanges: " + totalChangesInTree + ", NumLevs: " + numLevelsInTree);
            //Debug.LogWarning("Strength, Level: " + level + ",Type: " + type + ": " + impactStrength);
            //TODO other metrics, like time since change made, num similar nodes in tree

            return absoluteImpactStrength;
            //figure out the max strength of all nodes, make that one strength = 1, so dark red, gradation from there.
        }

        //TODO if cyclic, get impact strength from cyclic parent, rather than recalculating
        public float CalculateNormalizedImpactStrength(float maxImpactStrength) {
            if(maxImpactStrength == 0) {
                return 0;
            }
            else {
                normalizedImpactStrength = absoluteImpactStrength / maxImpactStrength;
                return normalizedImpactStrength;
            }
        }

        public float GetNormalizedImpactStrength() {
            return normalizedImpactStrength;
        }
    }
}