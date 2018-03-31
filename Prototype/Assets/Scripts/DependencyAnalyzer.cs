using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq; //TODO any way to avoid "using system.linq" for just this one extension method? other way to do
using LibGit2Sharp;

public class DependencyAnalyzer {

    //public Dictionary<CustomType, HashSet<CustomType>> dependencyTable;
    public Assembly assembly; // TODO REMOVE THIS EVENTUALLY
    private static Dictionary<string, CustomType> customTypeLookup;

    public static CustomType GetCustomTypeFromString(string typeName) {
        if(customTypeLookup.ContainsKey(typeName)) {
            return customTypeLookup[typeName];
        }
        else {
            return null;
        }
    }

    //public CustomType GetCustomTypeFromString(string typeName) {
    //    if (customTypeLookup.ContainsKey(typeName)) {
    //        return customTypeLookup[typeName];
    //    }
    //    else {
    //        return null;
    //    }
    //}

    public Dictionary<string, CustomType>.ValueCollection GetAllCustomTypes() {
        return customTypeLookup.Values;
    }

    //TODO IMPORTANT: Check if public/private classes WITHIN a class are captured by the GetALlTypes method
    //YES: CurrentType: ClassB+anotherClass1 //TODO need to handle this corrrectly, indicate private class within ClassB
    //TODO: Need to consider datastructures of types, like List<SomeType> or SomeType[]
    //List: System.Collections.Generic.List`1[CustomMethod]
    //TODO consider partial namespace prefixes, e.g. Int32 instead of System.Int32...umm, or need better example
    //TODO If dependency is generic type, get genericParams, add to dependency list
    //TODO need to add nested types to dep list

    HashSet<Type> GetFieldDependencies(Type type) {

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
        HashSet<Type> types = new HashSet<Type>();

        foreach (FieldInfo field in fields) {
            types.Add(field.FieldType);
            //Debug.Log("In GetFieldDeps, cur field = " + field + ", name = " + field.Name + ", fieldType = " + field.FieldType); //In GetFieldDeps, cur field = System.String authorOfChange, fieldType = System.String

            //Debug.LogError("  Field name: " + field.FieldType.Name + ", fullname: " + field.FieldType.FullName + ", isNested(has+): " + field.FieldType.IsNested + ", namespace: " + field.FieldType.Namespace + ", FULLTYPE: " + field.FieldType);

            // Add any generic types included in this type
            if (field.FieldType.IsGenericType) {
                types.UnionWith(GetAllNestedGenerics(field.FieldType));
            }
        }

        return types;
    }

    HashSet<Type> GetMethodDependencies(Type type) {
        //Consider, adding separate data structure for EACH method, so can 

        MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly); // ONLY base, target     

        HashSet<Type> types = new HashSet<Type>();

        foreach (MethodInfo method in methods) {

            // Parameter dependencies
            ParameterInfo[] parameters = method.GetParameters();
            foreach (ParameterInfo parameter in parameters) {
                //types.Add(parameter.Member.MemberType.GetType()); //TODO CHECK, REPLACED THIS
                types.Add(parameter.ParameterType); //TODO CHECK, REPLACED THIS

                // Add any generic types in this type
                if (parameter.ParameterType.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(parameter.ParameterType));
                }
            }

            // Return type dependencies
            if (method.ReturnType != typeof(void)) {
                types.Add(method.ReturnType);

                // Add any generic types in this type
                if (method.ReturnType.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(method.ReturnType));
                }
            }

            // Local dependencies
            IList<LocalVariableInfo> localVars = methods[0].GetMethodBody().LocalVariables;
            foreach (LocalVariableInfo local in localVars) {
                types.Add(local.LocalType);

                if(local.LocalType.IsGenericType) {
                    types.UnionWith(GetAllNestedGenerics(local.LocalType));
                }
            }
        }

        return types;
    }

    HashSet<Type> GetNestedDependencies(Type type) {
        Type[] nested = type.GetNestedTypes(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        HashSet<Type> types = new HashSet<Type>();

        foreach (Type t in nested) {
            types.Add(t);

            if (t.IsGenericType) {
                types.UnionWith(GetAllNestedGenerics(t));
            }
        }

        return types;
    }

    HashSet<Type> GetInheritedDependencies(Type type) {
        Type[] interfaces = type.GetInterfaces();
        Type baseType = type.BaseType;

        HashSet<Type> types = new HashSet<Type>();
        types.Add(baseType);

        if (baseType.IsGenericType) {
            types.UnionWith(GetAllNestedGenerics(baseType));
        }

        foreach (Type i in interfaces) {
            types.Add(i);
            if(i.IsGenericType) {
                types.UnionWith(GetAllNestedGenerics(i));
            }
        }

        return types;
    }

    HashSet<Type> GetAllNestedGenerics(Type type) {
        HashSet<Type> types = new HashSet<Type>();

        // Add any generic types in this type, searching recursively for ALL generics in definition
        // E.g. capture class A in:  Dictionary< Dictionary<int, A>, int> dict;
        foreach (Type genType in type.GetGenericArguments()) {
            types.Add(genType);
            types.UnionWith(GetAllNestedGenerics(genType));
        }

        return types;
    }

    public DependencyAnalyzer() {

        //TODO CONSTRUCTOR DEPS
        //dependencyTable = new Dictionary<CustomType, HashSet<CustomType>>();
        assembly = Assembly.GetExecutingAssembly();
        customTypeLookup = new Dictionary<string, CustomType>(); //NEWW

        Debug.Log(Assembly.GetExecutingAssembly().GetFiles().Length);
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles()[0].Name); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Library\ScriptAssemblies\Assembly-CSharp.dll
        Debug.Log("TYPES: " + assembly.GetTypes());
        Debug.Log("TYPES LENGTH: " + assembly.GetTypes().Length);

        Type[] types = assembly.GetTypes();

        foreach(Type currentType in types) {
            if ((currentType.Namespace != null && !currentType.Namespace.Contains("Unity") && !currentType.Namespace.Contains("System")) || (currentType != typeof(CustomType) && currentType != typeof(DependencyAnalyzer))) {
                CustomType customType = new CustomType(currentType);
                customTypeLookup.Add(customType.simplifiedFullName, customType);
            }
        }

        foreach (CustomType customType in customTypeLookup.Values) {
            HashSet<Type> dependencySet = new HashSet<Type>();

            dependencySet.UnionWith(GetFieldDependencies(customType.type));
            dependencySet.UnionWith(GetMethodDependencies(customType.type));
            dependencySet.UnionWith(GetNestedDependencies(customType.type));
            dependencySet.UnionWith(GetInheritedDependencies(customType.type));
            
            HashSet<CustomType> customDependencySet = new HashSet<CustomType>();
            foreach (Type t in dependencySet) {

                //Ignore dependency with self
                if (t != customType.type) {
                    string simplifiedName = SimplifyTypeFullName(t);

                    //Dont add dependencies that aren't CustomTypes (e.g. those in System or Unity namespaces, as previously filtered)
                    if (customTypeLookup.ContainsKey(simplifiedName)) {
                        customDependencySet.Add(customTypeLookup[simplifiedName]);
                    }

                }
            }

            customType.SetDependencies(customDependencySet);
        }

        //for (int i = 0; i < types.Length; i++) {

        //    HashSet<Type> dependencySet = new HashSet<Type>();

        //    dependencySet.UnionWith(GetFieldDependencies(currentType));
        //    dependencySet.UnionWith(GetMethodDependencies(currentType));
        //    //dependencySet.UnionWith(GetInheritanceDependencies()); //TODO

        //    HashSet<CustomType> customDependencySet = new HashSet<CustomType>();
        //    foreach (Type t in dependencySet) {

        //        if (t.Namespace != null && (t.Namespace.Contains("Unity") || t.Namespace.Contains("System")) || t == typeof(CustomType) || currentType == typeof(DependencyAnalyzer) || t == currentType) {
        //            continue;
        //        }
        //        CustomType chosenCustomType = null;
        //        string name = SimplifyTypeFullName(t);

        //        //Ensure that each customType is a singleton, same reference across all data structures
        //        if (customTypeLookup.ContainsKey(name)) {
        //            chosenCustomType = customTypeLookup[name];
        //        }
        //        else {
        //            chosenCustomType = new CustomType(t);
        //            customTypeLookup.Add(chosenCustomType.simplifiedFullName, chosenCustomType);
        //        }
        //        customDependencySet.Add(chosenCustomType);
        //    }

        //    customType.SetDependencies(customDependencySet);

        //    //orig.ConvertAll(x => new TargetType { SomeValue = x.SomeValue });
        //    //TODO exclude system, unityengine types
        //    //foreach (Type type in dependencySet) {
        //    //    Debug.Log("HASHSET TYPES: " + type); //System.Int32
        //    //    Debug.Log("Type name: " + type.Name + ", namespace: " + type.Namespace); //CustomType, namespace: 
        //    //    //Debug.LogWarning(type.GetMethod("LogTest",BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
        //    //}
        //}
        //end

        //for (int i = 0; i < types.Length; i++) {

        //    Type currentType = types[i];
        //    //Dont include any namespaces not created by the user
        //    //TODO refine this to ensure only UnityEngine/UnityEditor/System namespaces included, not GreatSystem etc
        //    //if (currentType.Namespace != null && (currentType.Namespace.Contains("Unity") || currentType.Namespace.Contains("System"))) {
        //    //TODO ideally exclude all of my namespace WhatChanged or whatever
        //    if (currentType.Namespace != null && (currentType.Namespace.Contains("Unity") || currentType.Namespace.Contains("System")) || currentType == typeof(CustomType) || currentType == typeof(DependencyAnalyzer)) {
        //    continue;
        //    }

        //    Debug.LogError("Type name: " + currentType.Name + ", fullname: " + currentType.FullName + ", isNested(has+): " + currentType.IsNested + ", namespace: " + currentType.Namespace + ", FULLTYPE: " + currentType); //Type name: anotherClass1, fullname: ClassB+anotherClass1, isNested(has+): True, namespace: // NEWWW

        //    //Ensure every customType is a singleton
        //    //TODO better way?
        //    CustomType customType;
        //    string typeName = SimplifyTypeFullName(currentType);
        //    if (customTypeLookup.ContainsKey(typeName)) {
        //        customType = customTypeLookup[typeName];
        //    }
        //    else {
        //        customType = new CustomType(currentType);
        //        customTypeLookup.Add(customType.simplifiedFullName, customType);
        //    }


        //    HashSet<Type> dependencySet = new HashSet<Type>();

        //    dependencySet.UnionWith(GetFieldDependencies(currentType));
        //    dependencySet.UnionWith(GetMethodDependencies(currentType));
        //    //dependencySet.UnionWith(GetInheritanceDependencies()); //TODO

        //    HashSet<CustomType> customDependencySet = new HashSet<CustomType>();
        //    foreach(Type t in dependencySet) {

        //        if (t.Namespace != null && (t.Namespace.Contains("Unity") || t.Namespace.Contains("System")) || t == typeof(CustomType) || currentType == typeof(DependencyAnalyzer) || t == currentType) {
        //            continue;
        //        }
        //        CustomType chosenCustomType = null;
        //        string name = SimplifyTypeFullName(t);

        //        //Ensure that each customType is a singleton, same reference across all data structures
        //        if(customTypeLookup.ContainsKey(name)) {
        //            chosenCustomType = customTypeLookup[name];
        //        }
        //        else {
        //            chosenCustomType = new CustomType(t);
        //            customTypeLookup.Add(chosenCustomType.simplifiedFullName, chosenCustomType);
        //        }
        //        customDependencySet.Add(chosenCustomType); 
        //    }

        //    customType.SetDependencies(customDependencySet);

        //    //orig.ConvertAll(x => new TargetType { SomeValue = x.SomeValue });
        //    //TODO exclude system, unityengine types
        //    //foreach (Type type in dependencySet) {
        //    //    Debug.Log("HASHSET TYPES: " + type); //System.Int32
        //    //    Debug.Log("Type name: " + type.Name + ", namespace: " + type.Namespace); //CustomType, namespace: 
        //    //    //Debug.LogWarning(type.GetMethod("LogTest",BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
        //    //}
        //}

        Debug.LogWarning("Datapath is: " + Application.dataPath); 
        DirectoryInfo assetDir = new System.IO.DirectoryInfo(Application.dataPath);
        FileInfo[] csFiles = assetDir.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);

        HashSet<string> typesAsStrings = new HashSet<string>();

        Debug.LogWarning("Iterating all .cs files in Assets");
        foreach (FileInfo f in csFiles) {
            Debug.LogWarning("f: " + f + ", name: " + f.Name + ", fullName: " + f.FullName + ", length: " + f.Length);
            //output: f: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, name: ClassC.cs, fullName: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, length: 276

            //typesAsStrings.UnionWith(GetTypeStringInFile(f));

            CustomFile customFile = new CustomFile(f);

            List<CustomType> typesInFile = ExtractTypesFromFile(customFile);
            customFile.SetTypesInFile(typesInFile);
        }

        foreach (string typeString in typesAsStrings) {
            //TODO figure out what return types from libgit2sharp diff/log are
        }

        //CustomFile cf = customTypeLookup["ClassB"].file;
        //int lineNum = 142;
        //CustomType ct = cf.GetDeepestNestedTypeAtLineNum(customTypeLookup["ClassB"], lineNum);
        //Debug.LogError("***Type at line " + lineNum + ": " + ct.type.Name);

        //DEBUGSTART
        //CustomType ct = customTypeLookup["ClassB"];
        //int lineNum = 71;
        //CustomType deepestType = ct.GetDeepestNestedTypeAtLineNum(lineNum);
        //Debug.LogError("***Type at line " + lineNum + ": " + deepestType.type.FullName);
        //CustomMethod deepestMethod = deepestType.GetMethodAtLineNum(lineNum);
        //if (deepestMethod == null) {
        //    Debug.LogError("  MethodThere: " + "NULL");
        //}
        //else {
        //    Debug.LogError("  MethodThere: " + deepestMethod.info.Name);
        //}

        //CustomType typeAtLine = ct.file.GetTypeByLineNumber(lineNum);
        //Debug.Log(" Type at line: " + typeAtLine);
        //CustomMethod methodAtLine = null;
        //if (typeAtLine != null) {
        //    methodAtLine = typeAtLine.GetMethodAtLineNum(lineNum);
        //    if (methodAtLine != null) {
        //        Debug.Log(" Method at line: " + methodAtLine.info.Name);
        //    }
        //    else {
        //        Debug.Log("Method null");
        //    }
        //}
        //GOODDEBUGEND

        //foreach (CustomType cts in cf.GetTypesInFile()) {
        //    Debug.Log("  TYpes in file: " + cts.type.FullName);

        //    CustomType[] nestedCt = cts.GetNestedCustomTypes();
        //    foreach(CustomType ncts in nestedCt) {
        //        Debug.LogWarning("    " + ncts.type.FullName + ", nested in " + cts);
        //    }
        //    Debug.Log("Length of nestedCts: " + nestedCt.Length);
        //}


        //GitDiffFile();

        //NodeEditorFramework.NodeCanvas asdf = new NodeEditorFramework.NodeCanvas();

    }

    private void GitDiffFile() {
        //////////////////////////////////////
        //// LIBGIT2SHARP INTEGRATION
        //////////////////////////////////////
        //// TODO generalize this repo path, like maybe FileInfo, backtrack til find .git
        //"D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts"
        //    "D:\User\Documents\CMPSC\600\RegexTesting\RegexTesting\Assets\Scripts"
        //Repository repo = new Repository("D:/User/Documents/CMPSC/600/RegexTesting/RegexTesting/");
        //Repository repo = new Repository("D:/User/Documents/CMPSC/600/SeniorThesisPrototype/");
        
        //TODO allow user to select git path
        DirectoryInfo gitDir = new DirectoryInfo(Path.GetFullPath("."));
        bool isFound = false;
        while (!isFound) {
            DirectoryInfo[] dirs = gitDir.GetDirectories(".git");

            if (dirs.Length == 0) {
                if(gitDir.Parent == null) {
                    Debug.LogError("!!Could not find Git directory working backwards, abort!");
                }
                else {
                    gitDir = gitDir.Parent;
                }
            }
            else {
                isFound = true;
                gitDir = dirs[0];
            }
        }
        Debug.Log("This is the git dir: " + gitDir.FullName);
        Repository repo = new Repository(gitDir.FullName);

        CustomFile chosenFile = customTypeLookup["ClassB"].file;

        //TODO UI dropdown select commit, then use here
        List<Commit> commitList = new List<Commit>();
        foreach(Commit c in repo.Commits) {
            commitList.Add(c);
        }
        //LibGit2Sharp.Tree chosenTree = commitList[0].Tree; //most recent commit?
        LibGit2Sharp.Tree recentCommit = repo.Head.Tip.Tree;
        LibGit2Sharp.Tree chosenTree2 = commitList[6].Tree;

        CompareOptions compareOptions = new CompareOptions();
        compareOptions.ContextLines = 0; // if 0, only include modified lines in diff (has + or - at start of line). 3 is default
        compareOptions.IncludeUnmodified = true; 
        compareOptions.InterhunkLines = 0;  // if Int.MAX then all changes are in ONE hunk

        ExplicitPathsOptions pathOptions = new ExplicitPathsOptions();

        //TODO include all files that branch from root selected class
        List<string> chosenFilePaths = new List<string>() { chosenFile.relPath };

        Patch patch = repo.Diff.Compare<Patch>(chosenTree2, DiffTargets.WorkingDirectory, new List<string> { chosenFile.relPath }, pathOptions, compareOptions);

        //TODO iterate through each fil
        PatchEntryChanges changes = patch[chosenFile.relPath]; //WORKS if correct relpath
        string patchText = changes.Patch; // If no changes b/w commits, then "", or if notIncludeModified, null changes
        Debug.LogError("***THEPATCH: " + patchText);

        /*
         * @@ -47,3 +63,26 @@ public class ClassB : MonoBehaviour {//exampe
         * @@ -44 +55,5 @@ public class ClassB : MonoBehaviour {
            -    
            +    System.Text.StringBuilder HasParameters(string someString, ClassC someClass, int someInt, System.Text.StringBuilder someBuilder) {
            +        System.Text.StringBuilder testBuilder = new System.Text.StringBuilder("");
            +
            +        return testBuilder;
            +    }
         */

        //string diffPattern = @"\@\@\s\-(?:\d)+\s\+(?:\d)+\s\@\@";
        //TODO do i need to remove comments/strings here too?

        // WORKS  all hunk in one long string
        string diffPattern = @"\@\@\s\-(?:\d+)(?:,\d+)??\s\+(?<start>\d+)(?:,\d+)??\s\@\@
                            (?:[^\n]*?\n)(?<changes>.*?)(?=\n^@ | $\Z)
                            ";

        //TODO figure out way to sep chunks via regex
        //string diffPattern = @"\@\@\s\-(?:\d+)(?:,\d+)??\s\+(?<start>\d+)(?:,\d+)??\s\@\@
        //                    (?:[^\n]*?\n)(?<changes>^[\+]|[\-].*?$)+(?=\n^@ | $\Z)
        //                    ";
        Regex diffRegex = new Regex(diffPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline);

        foreach(Match m in diffRegex.Matches(patchText)) {
            Debug.LogError("Found match: " + m);
            Debug.LogError("   Hunk is:-" + m.Groups["changes"].Captures[0].Value + "-");
            Debug.LogError("     LineNum is: " + m.Groups["start"].Captures[0].Value);
            string hunk = m.Groups["changes"].Captures[0].Value;
            int lineNum = Int32.Parse(m.Groups["start"].Captures[0].Value);

            System.Text.StringBuilder fileString = new System.Text.StringBuilder("");
            StringReader reader = new StringReader(hunk);
            string line;
            while ((line = reader.ReadLine()) != null) {

                CustomType typeAtLine = chosenFile.GetTypeByLineNumber(lineNum);
                Debug.Log(" Type at line: " + typeAtLine);
                CustomMethod methodAtLine = null;
                if (typeAtLine != null) {
                    methodAtLine = typeAtLine.GetMethodAtLineNum(lineNum);
                    if (methodAtLine != null) {
                        Debug.Log(" Method at line: " + methodAtLine.info.Name);
                    }
                }
                
                switch (line[0]) {
                    case '+':
                        if (typeAtLine != null) {
                            if (methodAtLine != null) {
                                typeAtLine.additionsInMethods++;
                                methodAtLine.additions++;
                            }
                            else {
                                typeAtLine.additionsOutsideMethods++;
                            }
                        }
                        lineNum++;
                        break;
                    case '-':
                        if (typeAtLine != null) {
                            if (methodAtLine != null) {
                                typeAtLine.deletionsInMethods++;
                                methodAtLine.deletions++;
                            }
                            else {
                                typeAtLine.deletionsOutsideMethods++;
                            }
                        }
                        break;
                    default:
                        Debug.LogError("Unknown hunk format for: " + hunk);
                        break;
                }

            }
        } // End iterate hunk matches

        
        List<CustomType> typesInFile = chosenFile.GetTypesInFile();

        foreach(CustomType t in typesInFile) {
            Debug.Log("Total changes in " + t + ": " + t + ", inMethods: " + t.totalChangesInMethods + ", outMethods: " + t.totalChangesOutsideMethods);

            foreach(CustomMethod m in t.methods) {
                Debug.LogWarning("   Total changes in " + m + ": " + (m.additions + m.deletions) + "(Adds: " + m.additions + ", Dels: " + m.deletions + ")");
            }
        }

        //TODO why supernestedmethod had no recorded changes, should have +++. Look back at TopLevelClasses, see if need IsNested elsewhere instead of other thing



        //Debug.LogError("***REPO***: " + repo.Head.CanonicalName);
        //foreach (var a in repo.Head.Commits) { // First element is MOST RECENT, last is the initial commit
        //    Debug.LogWarning("Commit message: " + a.Message); //Switched back to .NET 4.6 to allow use of LibGit2Sharp package (previous GitSharp package did not offer enough functionality)
        //    Debug.LogWarning("Author" + a.Author); // Michael Camara <michaeljcamara@gmail.com>
        //}
        //Debug.LogError("--------------");

        ////repo.Head.Tip.Parents.SingleOrDefault<GitObject>(); // singleOrDefault<T> return default init of T, eg null. Single can throw exception
        //foreach (Commit commit in repo.Commits) {
        //    foreach (var parent in commit.Pents) {
        //        Debug.LogWarning(commit.Sha + " : " + commit.MessageShort);
        //        foreach (TreeEntryChanges change in repo.Diff.Compare<TreeChanges>(parent.Tree,
        //        commit.Tree)) {
        //            Debug.LogWarning(changege.Status + " : " + change.Path);
        //        }
        //    }
        //}

        //Debug.LogError("--------------");

        //string result = "-nothing-";
        ////Path relative to where local .git directory stored, e.g. "." is D:/User/Documents/CMPSC/600/SeniorThesisPrototype
        //string relPath = "Prototype/Assets/Scripts/DependencyAnalyzer.cs"; // Get path from individual file as iterating? FileInfo
        //Debug.LogError("Full path: " + Path.GetFullPath(".")); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype
        //List<Commit> CommitList = new List<Commit>();
        //foreach (LogEntry entry in repo.Commits.QueryBy(relPath).ToList())
        //    CommitList.Add(entry.Commit);
        ////CommitList.Add(null); // Added to show correct initial add

        //// Commits in which this file was modified
        //Debug.LogError("CommitList Count: " + CommitList.Count);
        //foreach (Commit c in CommitList) {
        //    Debug.Log("Commit where changed: " + c.Message);
        //}

        //// General diff all paths
        //TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>();
        //Patch patch = repo.Diff.Compare<Patch>();

        //// THIS narrows to SPECIFIC file (or, possibly, directory)
        //List<string> stringPaths = new List<string>();
        //stringPaths.Add(relPath);
        ////TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>(stringPaths);
        ////Patch patch = repo.Diff.Compare<Patch>(stringPaths);

        ////GitObject a1, b1;
        ////ContentChanges cc = repo.Diff.Compare(a1, b1);
        ////ContentChanges ccc = repo.Diff.Compare((Blob) repo.Head.Tip.Tree[""].Target, (Blob) repo.Head.Tip.Tree[""].Target);
        ////ccc.Patch; // STRING, maybe the whole diff comparison with +++/---???

        //Debug.LogError("TARGET BLOB: " + CommitList[0].Tree[relPath].Target);  // 3a8a48js8dh8a489ha48h9
        //Debug.LogError("TARGET BLOB: " + CommitList[1].Tree[relPath].Target);  // 3a8a48js8dh8a489ha48h9    

        //Blob firstBlob = (Blob)CommitList[0].Tree[relPath].Target;
        //Blob secondBlob = (Blob)CommitList[1].Tree[relPath].Target;

        ////ContentChanges changes = repo.Diff.Compare(firstBlob, secondBlob);

        //CompareOptions compOptions = new CompareOptions();
        ////compOptions.Algorithm = DiffAlgorithm.Patience;  
        //ContentChanges changes = repo.Diff.Compare(firstBlob, secondBlob, compOptions);

        //////TRYING BLAME!!///
        //////BlameOptions blameOpt; blameOpt.StartingAt; // indicate boundaries for class here, and commit reach
        ////string fullPath = "D:/User/Documents/CMPSC/600/SeniorThesisPrototype/Prototype/Assets/Scripts/DependencyAnalyzer.cs";
        ////BlameHunkCollection hunkCol = repo.Blame(relPath);
        ////foreach(BlameHunk hunk in hunkCol) {
        ////    //hunk.FinalStartLineNumber
        ////    Debug.LogError("BLAME HUNK: " + hunk.ToString() + ", SIG: " + hunk.FinalSignature.ToString()); //Michael Camara, <michaeljcam..>

        ////}

        //string stringPatch = changes.Patch;
        //Debug.LogError("THE PATCH: " + stringPatch);

        //// Get working tree
        ////Repo path: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\.git\, WorkingDir: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\
        //Debug.LogError("Repo path: " + repo.Info.Path + ", WorkingDir: " + repo.Info.WorkingDirectory);

        ////TODO how to change hunks, just solid block of text would be easier??? instead of @@-93,+34@@
        ////NOTE ORDER of blobs makes no difference
        ////ContentChanges reverseChanges = repo.Diff.Compare(secondBlob, firstBlob);
        ////string reverseStringPatch = changes.Patch;
        ////Debug.LogError("REVERSE PATCH: " + reverseStringPatch);
        ///*
        // * THE PATCH: @@ -6,12 +6,12 @@ using UnityEngine;
        // using System.Runtime.CompilerServices;
        // using System.IO;
        // using System.Text.RegularExpressions;
        //-//using GitSharp; //TODO Rem to remove this dependency
        //+using GitSharp;
        // * 
        // */

        //foreach (TreeEntryChanges change in treeChanges) {
        //    Debug.Log("TREE Change: " + change.Status + " : " + change.Path + " : " + change);
        //}

        //foreach (PatchEntryChanges change in patch) {
        //    Debug.Log("PATCH Change: " + change.Status + " : " + change.Path + " : " + change.LinesAdded + ";" + change.LinesDeleted + " : " + change);
        //}
    }

    private string RemoveCommentsAndStrings(CustomFile file) {

        /* Goals for parsing files
         * Isolate CLASSES (or broadly Types) within file, so we can link files in Git with types in Assembly
         *      By removing comments and string, then searches for "class SomeName" are easier to perform
         * Find line boundaries for classes (and methods??) within file
         *      
         * Considerations
         *      If replacing text, make sure don't remove newlines
         *      
         */

        //note ReadAllLines returns string[]
        string text = File.ReadAllText(file.info.FullName);

        string lineCommentPattern = @"(?<=^.*)(//.*?$)";
        //string lineCommentPattern = @"(?<=^.*)(//.*)(?!.*\*/$)"; //TODO Exclude if */ at end
        Regex lineCommentRegex = new Regex(lineCommentPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        //string blockCommentPattern = @"/\* #start with /*
        //                    (.*? #follow with any number of chars (NOT \n)
        //                    (?<newline>\n)*) #then any num consecutive \n 
        //                    *? #then repeat finding these occurences within /**/ boundary
        //                    \*/ #end with */
        //                    "; //works, 1000* faster with .*?
        //                       //Consider a way for lookahead, performance saver
        //string blockCommentPattern = @"(?<=/\*) #start with /*
        //                    (.*? #follow with any number of chars (NOT \n)
        //                    (?<newline>\n)*?) #then any num consecutive \n 
        //                    *? #then repeat finding these occurences within /**/ boundary
        //                    (?=\*/) #end with */
        //                    "; //works, 1000* faster with .*?
        //                       //Consider a way for lookahead, performance saver


        //string blockCommentPattern = @"/\*.*?\*/";
        string blockCommentPattern = @"/\*.*?\*/"; // TODO trying to exclude escaped backslash

        //TODO MAKE SIMPLE NEW ONE< THEN GET MATCH< GET NUM NEWLINES!!!
        //Regex blockCommentRegex = new Regex(blockCommentPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);// |RegexOptions.Singleline);
        Regex blockCommentRegex = new Regex(blockCommentPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);

        //TODO include literal @ strings
        string stringPattern = @"(?<=^.*)(?<!\\)(""[^""]*?"")"; //YES, each match = string, replace w/"". IGNORE ESCAPED quotes
        //string testStringPattern = @"^((?<start>[^""]*?)(?<stringliteral>""[^""]*?"")(?<end>[^""]*?))*?$";  //Separate captures into named groups, so can access string literals
        Regex stringRegex = new Regex(stringPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        //TEST LINE COMMENT
        string testLineComment = "string a = something;//then other stuff*/\nOn next line";
        testLineComment = lineCommentRegex.Replace(testLineComment, "");
        //Debug.LogWarning("TEST LINE: " + testLineComment);

        //TEST BLOCK COMMENT
        string testBlockText = "TestEscape\\nThis is a /* first testEscape\\n \nblock \n\n\n " +
            "comment \nlast */ then \nnothing";
        foreach (Match m in blockCommentRegex.Matches(testBlockText)) {
            //Debug.Log("**Num newlines: " + m.Groups["newline"].Captures.Count);
        }
        testBlockText = blockCommentRegex.Replace(testBlockText, "-REPLACED-");
        //Debug.LogWarning("BLOCK TEST: " + testBlockText);

        //TEST STRING
        string testStringText = "TestEscape\\\"Again\\\"OUT1\"First \"    " + "OUT2\"second \"" + "OUT3\"third.\"___OUT55";
        testStringText = stringRegex.Replace(testStringText, "");
        //Debug.LogWarning(testStringText);

        //REPLACE FILE CONTENTS

        text = stringRegex.Replace(text, "");

        //text = blockCommentRegex.Match(text, );
        text = blockCommentRegex.Replace(text, delegate (Match m) {

            //Debug.Log("Curr match = " + m.Value);

            //TODO any way to avoid "using system.linq" for just this one extension method? other way to do
            int numNewlines = m.Value.Count(x => x == '\n');
            //Debug.Log("**Num newlines: " + numNewlines);
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
            for (int i = 0; i < numNewlines; i++) {
                stringBuilder.Append("\n");
            }

            return stringBuilder.ToString();
        });

        text = lineCommentRegex.Replace(text, "");

        //Debug.LogWarning("TEXT: " + text);

        //TODO Revisit if MORE EFFICIENT to append line numbers to help with line number calculation later in regexs...if dont need backtrack, more performant?
        //System.Text.StringBuilder fileString = new System.Text.StringBuilder("");
        //using (StringReader reader = new StringReader(text)) {
        //    string line;
        //    int lineNum = 1;
        //    while ((line = reader.ReadLine()) != null) {
        //        //line.Insert(0, lineNum.ToString());
        //        fileString.Append(lineNum.ToString() + line + "\n");
        //        lineNum++;
        //    }
        //}
        //Debug.LogWarning("NEW TEXT: " + fileString.ToString());

        return text;
        //return fileString.ToString();
    }

    

    private List<CustomType> ExtractTypesFromFile(CustomFile file) {
        //TEMP
        //if (!file.name.Equals("ClassB.cs")) {
        //    return null;
        //}

        Debug.Log("FILE NAME IN EXTRACT IS:" + file.name);
        //TODO GENERALIZE THIS!!! //ideally exclude files in ../WhatHappened/etc
        if (!file.name.Equals("ClassB.cs") && !file.name.Equals("ClassA.cs") && !file.name.Equals("ClassC.cs") && !file.name.Equals("ClassD.cs")) {
            //Debug.Log("Equals ClassB.cs?: " + (file.name.Equals("ClassB.cs")) + ", " + (file.name == "ClassB.cs"));
            return null;
        }
        Debug.Log("Extracting from :" + file.info.FullName);

        List<CustomType> typesInFile = new List<CustomType>();

        //if (file.Name.Equals("CustomType.cs") || file.Name.Equals("CustomMethod.cs") || file.Name.Equals("DependencyAnalyzer.cs")) {
        //    return;
        //}

        Debug.LogError("**STARTING FILE: " + file.name);

        //string text = File.ReadAllText(file.FullName);
        string text = RemoveCommentsAndStrings(file);
        
        string classPattern = @"\b(?:class|interface)(?:\s+?)(?<class>[_\w]+?\b)(?=[^{]*?{)"; // WORKS!!
        Regex classRegex = new Regex(classPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

        int currentLineNum = 1; // +1 since the leading text does not capture the \n from the current line
        int currentIndex = 0; // Index of text string where current match begins
        CustomType previousType = null; // Keep track of the previous match's type

        foreach (Match classMatch in classRegex.Matches(text)) {
            //Debug.Log("Current match: " + classMatch);
            //Debug.LogError("INDEX: " + classMatch.Index);
            
            currentLineNum += text.Substring(currentIndex, classMatch.Index - currentIndex).Count(x => x == '\n');
            currentIndex = classMatch.Index;

            //TODO Handle namespace conflicts
            //string classString = classMatch.Groups["class"].Captures[0].Value;

            //CustomType targetType;
            //if (!customTypeLookup.TryGetValue(classString.ToString(), out targetType)) {
            //    Debug.LogError("Could not find " + classString + " in customTypeLookup");
            //}

            //targetType.startLineNum = currentLineNum;

            int startLineNum = currentLineNum, endLineNum = 0;

            //RENABLE
            for (int i = classMatch.Index, numOpenBraces = 0, numClosedBraces = 0, numNewLines = 0; i < text.Length; i++) {
                switch (text[i]) {
                    case '{':
                        numOpenBraces++;
                        break;
                    case '}':
                        numClosedBraces++;
                        break;
                    case '\n':
                        numNewLines++;
                        break;
                }

                if (numOpenBraces == numClosedBraces && numOpenBraces != 0) {
                    endLineNum = currentLineNum + numNewLines; // +1 since array is zero-indexed but line numbers are 1-indexed
                    break;
                }
            }

            /* RENABLE
            if(targetType.endLineNum == 0) {
                Debug.LogError("Could not find end line number for " + classString);
            }*/

            // Get the name of the class as it appears in the text
            System.Text.StringBuilder classString = new System.Text.StringBuilder("");
            classString.Append(classMatch.Groups["class"].Captures[0].Value);

            // Handle nested types (e.g. private classes nested in other classes, potentially many nested levels)
            //TODO clean this up with newer helper methods for nested classes
            bool foundParent = false;
            bool isNested = false;
            do {
                // No outer type detected, i.e. match is not nested
                if (previousType == null) {
                    foundParent = true;
                    isNested = false;
                }
                //Check if match is nested in previous matched type
                else if (startLineNum >= previousType.startLineNum && endLineNum <= previousType.endLineNum) {
                    Type parent = previousType.type;
                    while (parent.IsNested) {
                        classString.Insert(0, parent.Name + "+");
                        parent = parent.DeclaringType;
                    }
                    classString.Insert(0, parent.Name + "+");

                    Debug.LogError(" NestedTypeName = " + classString.ToString());
                    foundParent = true;
                    isNested = true;
                }
                //Not nested in previous matched type. Check if nested in that type's parent (if it exists)
                else {
                    //TODO need to normalize fullname to account for namespaces
                    if (previousType.type.DeclaringType != null) {
                        customTypeLookup.TryGetValue(SimplifyTypeFullName(previousType.type.DeclaringType), out previousType);
                    }
                    else {
                        previousType = null;
                    }
                }
            } while (!foundParent);

            // Get the CustomType associated with the string found in text (modified if type was nested to match desired format of key)
            CustomType currentType;
            if (!customTypeLookup.TryGetValue(classString.ToString(), out currentType)) {
                Debug.LogError("Could not find " + classString + " in customTypeLookup");
            }

            typesInFile.Add(currentType);
            currentType.SetFile(file);

            previousType = currentType;
            currentType.startLineNum = startLineNum;
            currentType.endLineNum = endLineNum;

            Debug.LogError("Class matched: " + classString + ", StartLineNum = " + currentType.startLineNum + ", EndLineNum = " + currentType.endLineNum);

            int numMatchesFound = 0;

            foreach (CustomMethod method in currentType.GetMethods()) {
                //Debug.Log("Looking for method: " + method.info);
                Regex methodRegex = method.regex;
                //Debug.Log("Regex is: " + methodRegex.ToString());

                Match methodMatch = methodRegex.Match(text);
                if (methodMatch.Success) {
                    //Debug.Log("Found match: " + methodMatch);
                    //Debug.LogWarning("    Method Signature: " + methodMatch.Groups["methodSig"].Captures[0].Value);
                    numMatchesFound++;
                }
                else {
                    Debug.LogError("!!DID NOT FIND: " + method.info.Name);
                }

                method.SetSimplifiedMethodSignature(methodMatch.Groups["methodSig"].Value);

                currentLineNum = currentType.startLineNum + text.Substring(classMatch.Index, methodMatch.Groups["methodSig"].Index - classMatch.Index).Count(x => x == '\n');
                //TODO consider recording index, as well as line, in CustomType/CustomMethod
                //currentIndex = methodMatch.Index;

                method.startLineNum = currentLineNum;

                for (int i = methodMatch.Groups["methodSig"].Index, numOpenBraces = 0, numClosedBraces = 0, numNewLines = 0; i < text.Length; i++) {
                    switch (text[i]) {
                        case '{':
                            numOpenBraces++;
                            break;
                        case '}':
                            numClosedBraces++;
                            break;
                        case '\n':
                            numNewLines++;
                            break;
                    }

                    if (numOpenBraces == numClosedBraces && numOpenBraces != 0) {
                        method.endLineNum = currentLineNum + numNewLines;
                        break;
                    }
                } // end method brace matching

                //RENABLE
                if (method.endLineNum == 0) {
                    Debug.LogError("Could not find end line number for " + method.info.Name);
                }
                else {
                    //Debug.LogError("Start/End Line Nums for " + method.info.Name + ": " + method.startLineNum + ", " + method.endLineNum);
                    Debug.LogError("    Method matched: " + method.info.Name + ", StartLineNum = " + method.startLineNum + ", EndLineNum = " + method.endLineNum);
                }


            } // end CustomMethod iteration

            if (numMatchesFound == currentType.methods.Count) {
                Debug.LogError("**FOUND ALL METHODS IN " + currentType + ": " + numMatchesFound + " out of " + currentType.methods.Count);
            }
            else {
                Debug.LogError("!!DID NOT FIND ALL METHODS IN " + currentType + ": " + numMatchesFound + " out of " + currentType.methods.Count);
            }

            // Reset linenum/index back to the currentType before moving onto next type
            currentLineNum = currentType.startLineNum;
            currentIndex = classMatch.Index;
        }

        return typesInFile;
    }



    void OnEnable() {
        Debug.LogError("ENABLING!!");
    }

    void OnDisable() {
        Debug.LogError("DISABLING!");
    }

    void OnReset() {
        Debug.LogError("ON RESET");
    }

    void Reset() {
        Debug.LogError("Reset");
    }

    private string SimplifyTypeFullName(Type t) {
        return t.FullName.Replace(t.Namespace + ".", "");
    }
}