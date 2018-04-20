// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using System.Linq;

namespace WhatHappened {
    public class CustomType {

        public Type assemblyType { get; }
        public CustomFile file { get; set; }
        public List<CustomMethod> methods { get; }
        public HashSet<CustomType> dependencies { get; set; }

        private CustomType[] _nestedCustomTypes;
        public CustomType[] nestedCustomTypes {
            get {
                if (_nestedCustomTypes == null) {
                    _nestedCustomTypes = FindNestedCustomTypes();
                }
                return _nestedCustomTypes;
            }
        }

        // Keep track of changes detected from Git repository (see GitAnalyzer)
        public int additionsInMethods, additionsOutsideMethods, deletionsInMethods, deletionsOutsideMethods;
        public int totalAdditions { get { return additionsInMethods + additionsOutsideMethods; } }
        public int totalDeletions { get { return deletionsInMethods + deletionsOutsideMethods; } }
        public int totalChangesInMethods { get { return additionsInMethods + deletionsInMethods; } }
        public int totalChangesOutsideMethods { get { return additionsOutsideMethods + deletionsOutsideMethods; } }
        public int totalLineChanges { get { return totalAdditions + totalDeletions; } }
        public bool bHasChanged { get { return (totalLineChanges > 0); } }

        public string simplifiedFullName { get; }
        public int startLineNum { get; set; }
        public int endLineNum { get; set; }

        public CustomType(Type type) {
            assemblyType = type;
            methods = new List<CustomMethod>();
            simplifiedFullName = SimplifyTypeFullName(type);
            
            // Create CustomMethods for each MethodInfo in the assembly type
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                //Prevent erroneous methods (created by assembly) from being added to this type's method collection
                if (!method.Name.StartsWith("<")) {
                    methods.Add(new CustomMethod(method));
                }
            }
        }

        /// <summary>
        /// Return all CustomTypes that are nested within this CustomType
        /// </summary>
        private CustomType[] FindNestedCustomTypes() {

            CustomType[] nested;
            Type[] nestedTypes = assemblyType.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (nestedTypes.Length > 0) {
                nested = new CustomType[nestedTypes.Length];

                for (int i = 0; i < nestedTypes.Length; i++) {
                    Type currentType = nestedTypes[i];
                    string simplifiedName = SimplifyTypeFullName(currentType);
                    CustomType customType = DependencyAnalyzer.GetCustomTypeFromString(simplifiedName);
                    if (customType != null) {
                        nested[i] = customType;
                    }
                    else {
                        Debug.LogError("!! COULD NOT FIND " + simplifiedName + " in CustomType lookup table. Abort!");
                    }
                }
            }

            //Else no nested types found, so init empty array
            else {
                nested = new CustomType[0];
            }

            return nested;
        }

        /// <summary>
        /// Return the deepest nested type at the indicated line number in the source code
        /// </summary>
        public CustomType GetDeepestNestedTypeAtLineNum(int lineNum) {
            CustomType deepestType = null;

            if (lineNum >= startLineNum && lineNum <= endLineNum) {

                // If no nested types, then the current type encapsulates the line number
                if (nestedCustomTypes.Length == 0) {
                    deepestType = this;
                }
                // Otherwise need to check nested types to see if the line number corresponds specifically to one of them
                else {
                    foreach (CustomType nestedType in nestedCustomTypes) {
                        CustomType t = nestedType.GetDeepestNestedTypeAtLineNum(lineNum);
                        if (t != null) {
                            deepestType = t;
                            break;
                        }
                    }

                    //If none of nested types bound the lineNum, then this type is the deepest type bounding it
                    if (deepestType == null) {
                        deepestType = this;
                    }
                }
            }

            return deepestType;
        }

        /// <summary>
        /// Return the method at the given line number in the source code, or null if the line it outside of any declared methods
        /// </summary>
        public CustomMethod GetMethodAtLineNum(int lineNum) {

            CustomMethod chosenMethod = null;

            //TODO store startIndex globally, prevent needing to consider previous methods we have lexically passed already
            int startIndex = 0;

            for (int i = startIndex; i < methods.Count; i++) {
                CustomMethod m = methods[i];
                if (lineNum >= m.startLineNum && lineNum <= m.endLineNum) {
                    chosenMethod = m;
                    break;
                }
            }

            return chosenMethod;
        }

        /// <summary>
        /// Reset any previously recorded additions and deletions detected in this CustomType via the GitAnalyzer
        /// </summary>
        public void ClearPreviousChanges() {
            additionsInMethods = additionsOutsideMethods = deletionsInMethods = deletionsOutsideMethods = 0;
            foreach (CustomMethod m in methods) {
                m.ClearPreviousChanges();
            }
        }

        /// <summary>
        /// Return a sorted list of this CustomType's CustomMethods in descending order based on number of detected
        /// changes from the GitAnalyzer
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CustomMethod> GetSortedMethodsByChanges() {
            // Returns new list, original is unmodified (important for parsing)
            return methods.OrderByDescending(i => i.totalChanges);
        }

        override public string ToString() {
            return assemblyType.FullName;
        }

        public static string SimplifyTypeFullName(Type t) {
            return t.FullName.Replace(t.Namespace + ".", "");
        }
    }
}