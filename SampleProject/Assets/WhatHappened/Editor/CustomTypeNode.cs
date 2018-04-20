// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

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
        public CustomType type { get; }
        public int level { get; }
        public bool bIsCyclic { get; set; }
        public CustomTypeNode parent { get; }
        public Vector3 leftAnchor { get; private set; }
        public Vector3 rightAnchor { get; private set; }

        //Impact strength [0,1] showing how likely changes in this node affect the root node
        public float absoluteImpactStrength { get; private set; }

        // Impact strength [0, maxImpactStrength], normalized with the max impact strength of all nodes in tree
        public float normalizedImpactStrength { get; private set; }

        public CustomTypeNode(CustomType type, CustomTypeNode parent, int level) {
            this.type = type;
            this.level = level;
            this.parent = parent;
        }

        /// <summary>
        /// Calculate three points to construct an arrow mesh directed toward the left anchor of this node, originating
        /// from its parent
        /// </summary>
        /// <returns></returns>
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

        public float CalculateAbsoluteImpactStrength(int totalChangesInTree, int numLevelsInTree) {

            if(totalChangesInTree == 0) {
                return 0;
            }

            int linesOfCodeChanged = type.totalLineChanges;
            int distanceFromRoot = level;

            absoluteImpactStrength = (linesOfCodeChanged / (float)totalChangesInTree) * ((numLevelsInTree - distanceFromRoot) / (float) numLevelsInTree);

            //TODO other metrics, like time since change made, num similar nodes in tree

            return absoluteImpactStrength;
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
    }
}