using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Reflection;

public class CustomType {

    //TODO change to readonly properties

    public Type type;
    public bool bHasChanged;
    public CustomFile file;
    public string[] changedLines;
    public int numChangedLines;
    public int[] changedLineNums;

    public CustomType[] dependsOn, usedBy;
    public CustomType[] parents, children;

    public DateTime dateChanged;
    public string authorOfChange;
    public float impactStrength;

    public int startLineNum;//START is where "class" appears in code, as in "class CustomType"
    public int endLineNum;  //END is where last closing brace is '}'

    public List<CustomMethod> methods;
    

	public CustomType(Type type) {
        this.type = type;
 
        methods = new List<CustomMethod>();
        //TODO merge with instantiation from DependencyAnalyzer
        foreach(MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {

            //TODO investigate other weird name conventions for methods in assembly
            // First is as written in code, Second is another method seemingly created by assembly???
            //Method: System.Collections.Generic.List`1[System.String] GetTypeStringInFile(System.IO.FileInfo) 
            //Method: System.String <GetTypeStringInFile>m__0(System.Text.RegularExpressions.Match) 
            if (!method.Name.StartsWith("<")) {
                methods.Add(new CustomMethod(method));
            }
            
        }
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
}
