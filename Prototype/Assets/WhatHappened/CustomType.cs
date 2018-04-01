using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace WhatHappened {
    public class CustomType {

        //TODO change to readonly properties

        public Type type;
        public CustomFile file;
        public int additionsInMethods, additionsOutsideMethods, deletionsInMethods, deletionsOutsideMethods;
        public int totalAdditions { get { return additionsInMethods + additionsOutsideMethods; } }
        public int totalDeletions { get { return deletionsInMethods + deletionsOutsideMethods; } }
        public int totalChangesInMethods { get { return additionsInMethods + deletionsInMethods; } }
        public int totalChangesOutsideMethods { get { return additionsOutsideMethods + deletionsOutsideMethods; } }
        public int totalLineChanges { get { return totalAdditions + totalDeletions; } }
        public bool hasChanged { get { return (totalLineChanges > 0); } }

        private string _simplifiedFullName;
        public string simplifiedFullName {
            get {
                if (_simplifiedFullName == null) {
                    _simplifiedFullName = type.FullName.Replace(type.Namespace + ".", "");
                }
                return _simplifiedFullName;
            }
        }

        public CustomType[] dependsOn, usedBy;
        public CustomType[] parents, children;

        public DateTime dateChanged;
        public string authorOfChange;
        public float impactStrength;

        public int startLineNum;//START is where "class" appears in code, as in "class CustomType"
        public int endLineNum;  //END is where last closing brace is '}'

        public List<CustomMethod> methods;
        private HashSet<CustomType> dependencies;

        public CustomType(Type type) {
            this.type = type;

            methods = new List<CustomMethod>();
            //TODO merge with instantiation from DependencyAnalyzer
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {

                //TODO investigate other weird name conventions for methods in assembly
                // First is as written in code, Second is another method seemingly created by assembly???
                //Method: System.Collections.Generic.List`1[System.String] GetTypeStringInFile(System.IO.FileInfo) 
                //Method: System.String <GetTypeStringInFile>m__0(System.Text.RegularExpressions.Match) 
                if (!method.Name.StartsWith("<")) {
                    methods.Add(new CustomMethod(method));
                }

            }
        }

        public void ClearPreviousChanges() {
            additionsInMethods = additionsOutsideMethods = deletionsInMethods = deletionsOutsideMethods = 0;
            foreach (CustomMethod m in methods) {
                m.ClearPreviousChanges();
            }
        }

        public void SetDependencies(HashSet<CustomType> dependencies) {
            this.dependencies = dependencies;
        }

        public HashSet<CustomType> GetDependencies() {
            return dependencies;
        }

        override public string ToString() {
            return type.FullName;
        }

        public List<CustomMethod> GetMethods() {
            return methods;
        }

        public void SetFile(CustomFile file) {
            this.file = file;
        }

        private CustomType[] nestedCustomTypes;

        //TODO THIS HAS TO COME AFTER ALL OF TYPETABLE HAS BEEN POPULATED
        public CustomType[] GetNestedCustomTypes() {

            if (nestedCustomTypes != null) {
                return nestedCustomTypes;
            }

            // If null, first time called, initialize customTypeArray

            Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (nestedTypes.Length > 0) {
                nestedCustomTypes = new CustomType[nestedTypes.Length];

                for (int i = 0; i < nestedTypes.Length; i++) {
                    Type currentType = nestedTypes[i];
                    //rem nested notation namespace.is.here.Then+Outer+Inner;
                    string simplifiedName = SimplifyTypeFullName(currentType);
                    //CustomType customType = DependencyAnalyzer.GetCustomTypeFromString(simplifiedName);
                    CustomType customType = DependencyAnalyzer.GetCustomTypeFromString(simplifiedName);
                    if (customType != null) {
                        nestedCustomTypes[i] = customType;
                    }
                    else {
                        Debug.LogError("!! COULD NOT FIND " + simplifiedName + " in CustomType lookup table. Abort!");
                    }
                }
            }
            //No nested types
            else {
                nestedCustomTypes = new CustomType[0];
            }

            return nestedCustomTypes;
        }

        private string SimplifyTypeFullName(Type t) {
            return t.FullName.Replace(t.Namespace + ".", "");
        }

        //TODO change parameters, cant omit currenttype, have invoked directly by type itself
        public CustomType GetDeepestNestedTypeAtLineNum(int lineNum) {

            CustomType deepestType = null;

            if (lineNum >= startLineNum && lineNum <= endLineNum) {
                CustomType[] nestedTypes = GetNestedCustomTypes();

                // If no nested types, then the current type encapsulates the line number
                if (nestedTypes.Length == 0) {
                    deepestType = this;
                }
                // Otherwise need to check nested types to see if the line number corresponds specifically to one of them
                else {
                    foreach (CustomType nestedType in nestedTypes) {
                        CustomType t = nestedType.GetDeepestNestedTypeAtLineNum(lineNum);
                        if (t != null) {
                            deepestType = t;
                            break;
                        }
                    }

                    //None of nested types bound the lineNum, so the currentType is the deepest type bounding it
                    if (deepestType == null) {
                        deepestType = this;
                    }
                }
            }

            return deepestType;
        }

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


        public IEnumerable<CustomMethod> GetSortedMethodsByChanges() {
            // Returns new list, original is unmodified (important for parsing)
            return methods.OrderByDescending(i => i.totalChanges);
        }
    }
}