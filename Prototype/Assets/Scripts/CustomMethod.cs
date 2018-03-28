using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class CustomMethod {

    public int startLineNum, endLineNum;
    public int additions, deletions;

    public MethodInfo info;

    public CustomMethod(MethodInfo info) {
        this.info = info;
        _regex = CreateMethodRegex();
    }

    private Regex _regex;
    public Regex regex { get { return _regex;}}

    //MAke method for returning regex
    //Account for primitive types, e.g. "int" appears as "Int32" in assembly
    //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table

    private Regex CreateMethodRegex() {
        //        string classPattern = @"(?<leading>.*?)(?<=\bclass\s+)(?<class>\w+\b) (?=.*{)"; //works

        //Debug.LogError("Method: " + info + ", Class: " + info.DeclaringType);

        System.Text.StringBuilder paramBuilder = new System.Text.StringBuilder("");
        foreach (ParameterInfo param in info.GetParameters()) {
            //Debug.Log("Parameter " + param + ", type: " + param.ParameterType + ", name: " + param.Name);
            //TODO lowercase

            string simplifiedType = SimplifyTypeName(param.ParameterType);
            //paramBuilder.Append(@".*?" + simplifiedType + @"\s*?" + param.Name); // using .*? to allow for namespace specifiers, eg Michael.Classb vs ClassB
            //paramBuilder.Append(@".*?" + simplifiedType + @".+?" + param.Name); // using .*? to allow for namespace specifiers, eg Michael.Classb vs ClassB. ALSO for generics, List<TypeHere> name
            //paramBuilder.Append(simplifiedType + @"\s+?" + param.Name + @"(\s*?,\s*?)??"); //now with converted type having optional namespace (namespace.is.here)??name
            paramBuilder.Append(simplifiedType + @"\s*?" + param.Name + @"(\s*?,\s*?)??"); //change back to \s*? since maybe no space if type is generic/array []nameHere <>nameHere

            //Debug.LogError("Simplified Type: " + simplifiedType + ", " + param.Name + ", " + param);
        }
        //Debug.Log("Fully formed parameter pattern: " + paramBuilder.ToString());
        //Fully formed parameter pattern: .*?ClassC\s*?someClass.*?Int32\s*?someInt.*?StringBuilder\s*?someBuilder
        // HOW TO GET "int" instead of "Int32"...just for primitive types?

        //DO THIS IN CUSTOMMETHOD< GET PATTERN/REGEX
        //CONSIDER SAME FOR CUSTOMTYPE
        //If void, lowercase
        //Consider could include namespace, Michael.ClassB vs ClassB
        //string paramPattern = method.info.ReturnParameter.ParameterType.Name + @"\s*?" + method.info.Name + @"\s*?\(" + ;

        //string methodPattern = @"(?<=\bclass\s+" + classString + @"\b) (?<leading>.*?)(?<methodSig>" + method.info.Name + @")"; //works need @
        //string methodPattern = @"(?<=\bclass\s+" + classString + @"\b) (?<leading>.*?)(?<methodSig>"
        string simplifiedReturnType = SimplifyTypeName(info.ReturnType);
        //string methodPattern = simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\)\s*?}";

        //string methodPattern = @"(?<=\bclass\s+" + info.DeclaringType.Name + @"\b) (?<leading>.*?)(?<methodSig>" + simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))\s*?{";
        //string methodPattern = @"(?<=\bclass\s+" + info.DeclaringType.Name + @"\b) (?<leading>.*?)(?<methodSig>" + simplifiedReturnType + @".+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))\s*?{";  // did .+? after return type to consider generics <...stuff here> but now dont need?

        //THIS WORKS
        //string methodPattern = @"(?<=\bclass\s+" + info.DeclaringType.Name + @"\b) (?<leading>.*?)(?<methodSig>" + simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))\s*?{"; //Now having converted name include optional namespace  (namespace.is.here)??name // THIS WORKS, but want to remove leading for optimization, using substrings instead



        //TODO allow interface, or specify
        string typeSpecifier = (info.DeclaringType.IsInterface) ? "interface" : "class";
        //string methodPattern = @"(?<=\bclass\s+?" + info.DeclaringType.Name + @"\b.*?)(?<methodSig>" + simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))(?=\s*?{)";
        string methodPattern = @"(?<=\b" + typeSpecifier + @"\s+?" + info.DeclaringType.Name + @"\b) (?:.*?)(?<methodSig>" + simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))(?=\s*?{)";

        //// using .*? to allow for namespace specifiers, eg Michael.Classb vs ClassB. ALSO for generics, List<TypeHere> name

        //Debug.Log("Full Method Pattern: " + methodPattern);

        Regex methodRegex = new Regex(methodPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        return methodRegex;
    }

    private static Regex arrayRegex = new Regex(@"\[\]");
    /// <summary>
    /// Take a Type and return its name as it would be appear in source code, notably for primitive types.  E.g. change "Int32" type from System.Int32 to "int", as it would commonly be written in code.
    /// List of built-in types taken from: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table
    /// </summary>
    private string SimplifyTypeName(Type primitive) {

        
        //Type being converted is: System.Collections.Generic.List`1[CustomMethod], namespace is: System.Collections.Generic, name is : List`1

        //Debug.Log("Generic: " + primitive.IsGenericParameter + ", " + primitive.IsGenericType + ", " + primitive.IsGenericTypeDefinition +
        //primitive.ContainsGenericParameters + ", " + primitive.IsContextful + primitive.HasElementType + primitive.IsArray + primitive.IsNested);
        
        //if (primitive.Namespace != "System") {
        //    return primitive.Name;
        //}
        string startName = primitive.Name;
        string endName = "";

        //Debug.Log("Type being converted is: " + primitive + ", namespace is: " + primitive.Namespace + ", name is : " + primitive.Name);

        //if (primitive.IsArray || primitive.IsGenericType) {
        //Debug.LogError("IsSpecial?: " + primitive.IsSpecialName + primitive.IsByRef + primitive.IsPointer); 
        if (primitive.IsArray || primitive.IsGenericType || primitive.IsByRef) {
            //Debug.Log("inside thing: " + primitive);
            string splitNameFromContainer = @"(?<name>[^\[`]+)(?<ending>[\[`\&].*?$)";
            Match nameMatch = Regex.Match(primitive.Name, splitNameFromContainer);

            startName = nameMatch.Groups["name"].Captures[0].Value;
            endName = nameMatch.Groups["ending"].Captures[0].Value;
            endName = endName.Replace("[", @"\[");
            endName = endName.Replace("]", @"\]");   

            //Debug.LogError("StartName After: " + startName + ", EndName: " + endName + ", Type: " + primitive);
        }

        //Get alpha-numeric-Underscore NAME first, e.g List not List`1, Int32 not Int32[][][] or Int32[,,]

        switch (startName) {
            case "Byte":
            case "SByte":
            case "Char":
            case "Decimal":
            case "Double":
            case "Object":
            case "String":
            case "Void":
                //startName = primitive.Name.ToLower();
                startName = startName.ToLower();
                break;

            case "Boolean":
                startName = "bool";
                break;
            case "Single":
                startName = "float";
                break;
            case "Int32":
                startName = "int";
                break;
            case "UInt32":
                startName = "uint";
                break;
            case "Int64":
                startName = "long";
                break;
            case "UInt64":
                startName = "ulong";
                break;
            case "Int16":
                startName = "short";
                break;
            case "UInt16":
                startName = "ushort";
                break;
            default:
                //Debug.Log(primitive + " is not a primitive type.");
                break;
        }

        if (primitive.IsGenericType) {
            ////convertedName = primitive.Name.Substring(0, primitive.Name.IndexOf('`')) + @"\<";
            //convertedName = primitive.Name.Substring(0, primitive.Name.IndexOf('`')) + @"\<\s*?"; // .*? at end to handle explicit namespace reference

            ////TODO what if generic type is, itself a generic type? Like Dictionary<int, List<int>>...make recursive
            ////TODO MAKE RECURSIVE!
            //Type[] genericTypes = primitive.GetGenericArguments();
            //for(int i = 0; i < genericTypes.Length; i++) {
            //    convertedName += SimplifyTypeName(genericTypes[i]);
            //    if(i < genericTypes.Length -1) {
            //        convertedName += @"\s*?,\s*?";
            //    }
            //    else {
            //        convertedName += @"\s*?>";
            //    }
            //}
            ////List< List< Dictionary< List<int>, List<int>>>>
            ////TODO switch to stringbuilder

            //TODO what if array, too?
            startName = SimplifyGenericType(primitive);

        }
        else if (primitive.IsArray) {
            //Debug.LogError("   Startname before: " + startName);
            startName += endName;
            //TODO handle nested arrays, [,] [,,] [] [][] etc
            //Debug.LogError("   Startname after: " + startName + ",  end = " + endName);
        }

        //TODO consider returning both "int" and "System.Int32" since technically either could be used

        //return convertedName;
        if(primitive.Namespace == "") {
            return startName;
        }
        else {
            return @"(" + primitive.Namespace + @"\.)??" + startName;
        }
        
    }

    string SimplifyGenericType(Type generic) {

        //Dictionary<List<int>, Dictionary<List<int>, List<int>>> DictOfLists () { return null; }
        //System.Collections.Generic.Dictionary`2[System.Collections.Generic.List`1[System.Int32],System.Collections.Generic.Dictionary`2[System.Collections.Generic.List`1[System.Int32],System.Collections.Generic.List`1[System.Int32]]], namespace is: System.Collections.Generic, name is : Dictionary`2

        //List<List<int>> someInts;

        string simplifiedName = generic.Name.Substring(0, generic.Name.IndexOf('`')) + @"\s*?\<\s*?";
        Type[] genericTypes = generic.GetGenericArguments();
        for (int i = 0; i < genericTypes.Length; i++) {
            simplifiedName += SimplifyTypeName(genericTypes[i]);
            if (i < genericTypes.Length - 1) {
                simplifiedName += @"\s*?,\s*?";
            }
            else {
                simplifiedName += @"\s*?>";
            }
        }

        /*
         * if(generic) remove '2, insert <
        Look at first generic argument
        System.Collections.Generic.List`1
        if(generic) remove '1, insert <
        look at first generic argument
        System.int32
        else (Simplify this name, return int
        if more arguments, ",", else ">
        */
        return simplifiedName;
    }

    public override string ToString() {
        return info.Name;
    }
}
