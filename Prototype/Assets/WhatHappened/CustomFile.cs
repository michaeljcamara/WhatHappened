using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
namespace WhatHappened {
    public class CustomFile {

        private List<CustomType> types;
        private FileInfo _info;
        public FileInfo info { get { return _info; } }

        List<CustomType> _topLevelTypes;
        List<CustomType> topLevelTypes {
            get {
                if (_topLevelTypes == null) {
                    _topLevelTypes = new List<CustomType>();
                    foreach (CustomType t in types) {
                        if (!t.type.IsNested) {
                            _topLevelTypes.Add(t);
                        }
                    }
                }

                return _topLevelTypes;
            }
        }

        private static Uri projectUri = new Uri(Path.GetFullPath("."));
        private static Uri assetsUri = new Uri(Application.dataPath);

        public string fullPath { get { return info.FullName; } }

        private string _assetsRelPath;
        public string assetsRelPath {
            get {
                if (_assetsRelPath == null) {
                    Uri assetsRelUri = new Uri(fullPath);
                    assetsRelUri = assetsUri.MakeRelativeUri(assetsRelUri);
                    _assetsRelPath = assetsRelUri.ToString();
                }

                return _assetsRelPath;
            }
        }

        private string _relPath;
        public string relPath {
            get {
                if (_relPath == null) {
                    Uri relUri = new Uri(fullPath);
                    relUri = projectUri.MakeRelativeUri(relUri);
                    _relPath = relUri.ToString();
                }

                return _relPath;
            }
        }

        private string diffText;

        public string name { get { return _info.Name; } }

        public override string ToString() {
            return _info.Name;
        }
        public void ClearPreviousChanges() {
            foreach (CustomType t in types) {
                t.ClearPreviousChanges();
            }
        }
        public CustomFile(FileInfo file) {
            this._info = file;
        }

        public void SetTypesInFile(List<CustomType> types) {
            this.types = types;
        }

        public CustomType GetTypeByLineNumber(int lineNum) {

            CustomType chosenType = null;

            //TODO optimize by always starting at the previous considered type. Skipping for now since likely only 1 type per class
            //int lastIndex = 0;
            for (int i = 0; i < topLevelTypes.Count; i++) {
                CustomType currentType = topLevelTypes[i];
                //Debug.Log("*** TOP LEVEL TYPE: " + currentType);
                //chosenType = GetDeepestNestedTypeAtLineNum(currentType, lineNum);
                chosenType = currentType.GetDeepestNestedTypeAtLineNum(lineNum);
                if (chosenType != null) {
                    break;
                }
            }

            return chosenType;
        }

        private CustomType GetDeepestNestedTypeAtLineNum(CustomType currentType, int lineNum) {

            CustomType deepestType = null;

            if (lineNum >= currentType.startLineNum && lineNum <= currentType.endLineNum) {
                CustomType[] nestedTypes = currentType.GetNestedCustomTypes();

                // If no nested types, then the current type encapsulates the line number
                if (nestedTypes.Length == 0) {
                    Debug.LogError("**No nested types in : " + currentType.type.FullName);
                    deepestType = currentType;
                }
                // Otherwise need to check nested types to see if the line number corresponds specifically to one of them
                else {
                    foreach (CustomType nestedType in nestedTypes) {
                        CustomType t = GetDeepestNestedTypeAtLineNum(nestedType, lineNum);
                        if (t != null) {
                            deepestType = t;
                            break;
                        }
                    }

                    //None of nested types bound the lineNum, so the currentType is the deepest type bounding it
                    if (deepestType == null) {
                        deepestType = currentType;
                    }
                }
            }

            return deepestType;
        }

        public List<CustomType> GetTypesInFile() {
            return types;
        }

        public void OpenDiffTextInEditor() {
            string path = "Assets/WhatHappened/Resources/tempDiff.txt";
            StreamWriter writer = File.CreateText(path); //File.AppendText(path);

            writer.Write(diffText);
            writer.Flush();
            writer.Close();

            //Re-import the file to update the reference in the editor
            UnityEditor.AssetDatabase.ImportAsset(path);

            UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path));

            //TextAsset asset = Resources.Load<TextAsset>("tempDiff");

            //UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path));

            //UnityEditor.AssetDatabase.load

            //Print the text from the file
            //Debug.Log(asset.text);

        }

        public void OpenFileInEditor(int lineNum = 1) {
            UnityEditor.AssetDatabase.OpenAsset(UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(assetsRelPath), lineNum);
        }

        public void SetDiffText(string diff) {
            diffText = diff;
        }
    }
}