using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.RegularExpressions;
//using GitSharp; //TODO Rem to remove this dependency
using System.Linq;
using LibGit2Sharp;

public class DependencyAnalyzer {
     
    //Comment here
    /* Multiline comment here*/
    /* Multilinne
     *      comment
     *                  here */
    //*/*/
    string o = "Comment after this"; //comment here, newline erased! so immediately after comes "string a..."
    //string p = "Comment after this";//comment here
    string a = "//Not a comment";
    string b = "/* not a comment */";
    string c = "/*" +
        "not a commenet"
        + "*/";

    //Dictionary<Type, LinkedList<Type>> dependencyTable;
    public Dictionary<Type, HashSet<Type>> dependencyTable;

    //REM Use CustomType to hold info
    //TODO how to do key/val for file/type lookup, how am i gonna use
    //public Dictionary<

    HashSet<Type> GetFieldDependencies(Type type) {

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
        HashSet<Type> types = new HashSet<Type>();

        foreach (FieldInfo field in fields) {
            types.Add(field.FieldType);
            Debug.Log("In GetFieldDeps, cur field = " + field + ", fieldType = " + field.FieldType);
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
                types.Add(parameter.Member.MemberType.GetType());
            }

            // Return type dependencies
            if (method.ReturnType != typeof(void)) {
                types.Add(method.ReturnType);
            }

            // Local dependencies
            IList<LocalVariableInfo> localVars = methods[0].GetMethodBody().LocalVariables;
            foreach (LocalVariableInfo local in localVars) {
                types.Add(local.LocalType);
            }
        }

        return types;
    }

    public Assembly assembly; // TODO REMOVE THIS EVENTUALLY

    //sealed class ProvideSourceLocation : Attribute {
    //    public readonly string File;
    //    public readonly string Member;
    //    public readonly int Line;
    //    public ProvideSourceLocation
    //        (
    //        [CallerFilePath] string file = "",
    //        [CallerMemberName] string member = "",
    //        [CallerLineNumber] int line = 0) {
    //        File = file;
    //        Member = member;
    //        Line = line;
    //    }

    //    public override string ToString() { return File + "(" + Line + "):" + Member; }
    //}


    // Use this for initialization
    public DependencyAnalyzer() {

        /*
        //GitSharp.Git.Status(new GitSharp.Commands.StatusCommand());
        GitSharp.Commands.StatusCommand status = new GitSharp.Commands.StatusCommand();
        //Repository repo = new Repository(status.ActualDirectory);
        //Repository repo = new Repository(status.GitDirectory);

        //Repository repo = new Repository(GitSharp.Commands.ConfigCommand.FindGitDirectory(".", true, false));
        Repository repo = new Repository(GitSharp.Repository.FindRepository("."));

        Debug.LogWarning("Repo: " + repo.Directory + ", " + repo.WorkingDirectory);

        //What object do i get from log/diff, find changed files, check my early word doc

        //GitSharp.Commands.DiffCommand diffC {
        ////    dda = stringf;
        ////};
        //GitSharp.Commands.LogCommand logC = new GitSharp.Commands.LogCommand {
        //    Dirstat = "files"
        //};
        //GitSharp.Git.Status(;
        //GitSharp.Commands.StatusCommand k; k.Repository.

        
        //Debug.LogWarning(new Commit(repo, "HEAD^").Message); // 2 commits back?
        //Debug.LogWarning(repo.CurrentBranch.CurrentCommit.Message);
        foreach (Change c in repo.CurrentBranch.CurrentCommit.Changes) {
            //Output: DependencyAnalyzer.cs, Modified, Blob[879a2c02], Prototype/Assets/Scripts/DependencyAnalyzer.cs
            Debug.LogWarning(c.Name + ", " + c.ChangeType + ", " + c.ChangedObject + ", " + c.Path);
            
            //TODO if c.name .endIn(".cs"), then...
            //TODO Get all file changes between commit1 -> commit20...what if file edited multiple times, which one to analyze?
                //Easier to have user CHOOSE a commit ,compare against current files / working tree
            //TODO what to do with branches  
        }

        //Debug.LogWarning("**GIT VERSION: " + GitSharp.Git.Version);
        //GitSharp.Git.Status(new GitSharp.Commands.StatusCommand());
        //Debug.LogWarning("StatusResults: " + new GitSharp.Commands.StatusCommand().ActualDirectory); //StatusResults: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\.git
        */

        dependencyTable = new Dictionary<Type, HashSet<Type>>();
        //Assembly ass = Assembly.LoadFrom("D:/User/Documents/CMPSC/600/SeniorThesisPrototype/Prototype/Assets/Scripts/DependencyAnalyzer.cs");
        //Type[] types2 = ass.GetTypes();
        //Debug.LogWarning("NUM TYPES IN DEP ANALYSZER: " + types2.Length);
        //Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        //Debug.LogWarning("NUM ASSEMBLIES: " + assemblies.Length);
        //foreach(Assembly a in assemblies) {
        //    //Debug.Log("Assembly: " + a.GetName() + ";;; " + a);
        //    Debug.Log(a.Location);
        //}

        

        //Debug.Log(Assembly.GetEntryAssembly().GetFiles());
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles().Length);
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles()[0].Name); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Library\ScriptAssemblies\Assembly-CSharp.dll

        //Assembly assembly = Assembly.GetExecutingAssembly();
        assembly = Assembly.GetExecutingAssembly();

        //TODO check if recursive file traversal
        System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo("D:/User/Documents/CMPSC/600/SeniorThesisPrototype/Prototype/Assets/Scripts/");
        foreach (System.IO.FileInfo fileInfo in directoryInfo.GetFiles()) {

            Debug.Log("FileInfo: " + fileInfo + ";; FullName: " + fileInfo.FullName + " ;; Name + " + fileInfo.Name + " ;; ");

            //assembly.GetFile(fileInfo.FullName); // Cannot find, expecting dll?
            
            
            //assembly.GetFile(fileInfo.FullName);
            //foreach (Type type in Assembly.LoadFile(fileInfo.FullName).GetTypes()) {
            //Debug.Log("FileInfo: " + fileInfo.Name + "; Type Name: " + type.Name);
            //}
        }

        // Getting folder from project view as UE object
        //UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath("Assets/MyFolder", typeof(UnityEngine.Object));
        //UnityEngine.Object[] selection = new UnityEngine.Object[1];
        //selection[0] = obj;
        //Selection.objects = selection;
        ////////////

        //TODO Check if TypeCode != object

        /////
        //string path = "D:/User/Documents/CMPSC/600/SeniorThesisPrototype/Prototype/Assets/Scripts/DependencyAnalyzer.cs";

        //var asd = Assembly.LoadFile(path);
        //foreach (var type in asd.GetTypes()) {
        //    Debug.Log("TYPES IN DEP ANALYZER: " + type.Name);

        //    // do check for type here, depending on how you wish to query
        //}
        /////////

        //assembly.GetExportedTypes();
        //assembly.GetManifestResourceNames();
        //assembly.GetLoadedModules();
        //assembly.GetModules();

        Debug.Log("TYPES: " + assembly.GetTypes());
        Debug.Log("TYPES LENGTH: " + assembly.GetTypes().Length);

        Type[] types = assembly.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type currentType = types[i];
            Debug.Log("CurrentType: " + currentType);

            HashSet<Type> dependencySet = new HashSet<Type>();
            //dependencySet.Add(currentType);

            dependencySet.UnionWith(GetFieldDependencies(currentType));
            dependencySet.UnionWith(GetMethodDependencies(currentType));
            //dependencySet.UnionWith(GetInheritanceDependencies());

            foreach (Type type in dependencySet) {
                Debug.Log("HASHSET TYPES: " + type);
                //Debug.LogWarning(type.GetCustomAttribute<ProvideSourceLocation>(true));
                //LogTest(type.Name);
                Debug.LogWarning("BBBBBBB ");

                //Debug.Log("Path: " + Application.dataPath);

                //Debug.LogWarning(type.GetMethod("LogTest",BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
                //Debug.LogWarning(type.GetMethod("TESTTEST", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
                //Debug.LogWarning(GetType().GetMethod("TESTTEST", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
                //Debug.LogWarning(GetType().GetMethod("TESTTEST", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Invoke(null, null));
                //Debug.LogWarning(type.GetType().GetMethod("TESTTEST", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance).Invoke(null, null));
            } 

            dependencyTable.Add(currentType, dependencySet);
        }
        Debug.LogWarning("Datapath is: " + Application.dataPath);
        DirectoryInfo assetDir = new System.IO.DirectoryInfo(Application.dataPath);
        FileInfo[] csFiles = assetDir.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);


        //TODO consider EnumerateFiles, allows access before all array populated
        HashSet<string> typesAsStrings = new HashSet<string>();

        Debug.LogWarning("Iterating all files in Assets");
        foreach (FileInfo f in csFiles) {
            Debug.LogWarning("f: " + f + ", name: " + f.Name + ", fullName: " + f.FullName + ", length: " + f.Length);
            //output: f: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, name: ClassC.cs, fullName: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, length: 276

            typesAsStrings.UnionWith(GetTypeStringInFile(f));
        }

        foreach(string typeString in typesAsStrings) {
            //TODO figure out what return types from libgit2sharp diff/log are
        }

        //using (var repo = new Repository("../../..")) {
        
        using (var repo = new Repository("D:/User/Documents/CMPSC/600/SeniorThesisPrototype")) {
            Debug.LogError("***REPO***: " + repo.Head.CanonicalName);
            //var RFC2822Format = "ddd dd MMM HH:mm:ss yyyy K";

            //foreach (Commit c in repo.Commits.Take(15)) {
            //    Console.WriteLine(string.Format("commit {0}", c.Id));

            //    if (c.Parents.Count() > 1) {
            //        Console.WriteLine("Merge: {0}",
            //            string.Join(" ", c.Parents.Select(p => p.Id.Sha.Substring(0, 7)).ToArray()));
            //    }

            //    Console.WriteLine(string.Format("Author: {0} <{1}>", c.Author.Name, c.Author.Email));
            //    //Console.WriteLine("Date:   {0}", c.Author.When.ToString(RFC2822Format, CultureInfo.InvariantCulture));
            //    Console.WriteLine();
            //    Console.WriteLine(c.Message);
            //    Console.WriteLine();
            //}
        }



    }

    private List<string> GetTypeStringInFile(FileInfo file) {

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
        string text = File.ReadAllText(file.FullName);

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
        Debug.LogError("TEST LINE: " + testLineComment);

        //TEST BLOCK COMMENT
        string testBlockText = "TestEscape\\nThis is a /* first testEscape\\n \nblock \n\n\n " +
            "comment \nlast */ then \nnothing";
        foreach (Match m in blockCommentRegex.Matches(testBlockText)) {
            Debug.Log("**Num newlines: " + m.Groups["newline"].Captures.Count);
        }
        testBlockText = blockCommentRegex.Replace(testBlockText, "-REPLACED-");
        Debug.LogError("BLOCK TEST: " + testBlockText);

        //TEST STRING
        string testStringText = "TestEscape\\\"Again\\\"OUT1\"First \"    " + "OUT2\"second \"" + "OUT3\"third.\"___OUT55";     
        testStringText = stringRegex.Replace(testStringText, "");
        Debug.LogError(testStringText);

        //REPLACE FILE CONTENTS

        text = stringRegex.Replace(text, "");

        //text = blockCommentRegex.Match(text, );
        text = blockCommentRegex.Replace(text, delegate(Match m) {

            Debug.Log("Curr match = " + m.Value);
            int numNewlines = m.Value.Count(x => x == '\n');
            Debug.Log("**Num newlines: " + numNewlines);
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
            for (int i = 0; i < numNewlines; i++) {
                stringBuilder.Append("\n");
            }

            return stringBuilder.ToString();
        });

        //foreach (Match m in blockCommentRegex.Matches(text)) {


        //    //int numNewlines = m.Groups["newline"].Captures.Count;
        //    Debug.Log("Curr match = " + m.Value);
        //    int numNewlines = m.Value.Count(x => x == '\n');
        //    Debug.Log("**Num newlines: " + numNewlines);
        //    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
        //    for (int i = 0; i < numNewlines; i++) {
        //        stringBuilder.Append("\n");
        //    }
        //    m.Result(stringBuilder.ToString());
        //}

        text = lineCommentRegex.Replace(text, "");

        Debug.LogError("TEXT: " + text);

        System.Text.StringBuilder fileString = new System.Text.StringBuilder("");
        using (StringReader reader = new StringReader(text)) {
            string line;
            int lineNum = 1;
            while ((line = reader.ReadLine()) != null) {
                //line.Insert(0, lineNum.ToString());
                fileString.Append(lineNum.ToString() + line + "\n");
                lineNum++;
            }
        }

        Debug.LogError("NEW TEXT: " + fileString.ToString());


        //How to handle if / used in both // and /**/
        // Like ///*** IMPORTANT "////*"

        //Regex order: string, block comments, line comments

        //Do I need to do alternation construct?? matching /**/
        // something then block /*

        //Try negatrive lookbehind?? look behind the same line

        //**TODO something
        /*
         * //*/ //hjlhjl */

        // /*   //THIS DOES NOT START A BLOCK COMMENT

        /* // */  // THIS *DOES-* END A BLOCK COMMENT // rem doesnt need to compile after
        List<string> classStrings = new List<string>();
        string classPattern = @"(?<=\b class .*?)(?<class>\w+) (?=\s*?{)";
        Regex classRegex = new Regex(classPattern, RegexOptions.IgnorePatternWhitespace);
        string testClassString = "something class SomeClass {}\n something class SomeClass2{}";
        //testClassString = classRegex.Replace(testClassString, "-REPLACED-");
        Debug.LogError("TEST CLASS: " + testClassString);

        // Rem might need to trim classes
        foreach(Match m in classRegex.Matches(testClassString)) {
            foreach(Capture c in m.Groups["class"].Captures) {
                Debug.Log("Captured class: " + c);
                classStrings.Add(c.Value);
            }
        }
        return classStrings;
    }

    private string ReplaceBlockComments(Match m) {

        foreach(Group g in m.Groups) {
            string name = g.Value;
            Debug.Log("Group value = " + g.Value + ", Name = " + g.ToString());
        }

        return "";
    }
    
    //void TestingReflection() {

    //    //Debug.Log(Assembly.GetCallingAssembly().GetFiles().Length);

    //    UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll<MonoBehaviour>() as UnityEngine.Object[];

    //    ClassB a;

    //    for (int i = 0; i < objs.Length; i++) {
    //        Debug.LogWarning(objs[i].name + ", " + objs[i].GetType());
    //        //Debug.Log(objs[i].GetType().FullName);
    //        //Debug.Log(objs[i].GetType());
    //        //Debug.Log(objs[i].GetType().AssemblyQualifiedName);
    //    }

    //    UnityEngine.Object chosenObject = objs[3];
    //    Debug.Log(chosenObject.name + ", " + chosenObject.GetType());
    //    Type chosenType = chosenObject.GetType();
    //    MethodInfo methodInfo = chosenType.GetMethod("PrivateMethod1", BindingFlags.NonPublic | BindingFlags.Instance);
    //    FieldInfo fieldInfo = chosenType.GetField("testClass", BindingFlags.Public | BindingFlags.Instance);



    //    Debug.Log(fieldInfo);

    //    //System.Diagnostics.Process p;
    //    //p.Start();
    //    //Debug.Log(testa.FullName);


    //    Debug.Log(methodInfo);
    //    MethodBody methodBody = methodInfo.GetMethodBody();
    //    IList<LocalVariableInfo> localList = methodBody.LocalVariables;
    //    for (int i = 0; i < localList.Count; i++) {
    //        //Debug.Log(localList[i].ToString());
    //        Debug.Log("Local Type: " + localList[i].LocalType);
    //    }

    //    Debug.LogWarning("NOW doing getFIELDS");

    //    // This ignores PRIVATE, but includes STATIC
    //    //FieldInfo[] fields = chosenType.GetFields();
    //    //foreach(FieldInfo field in fields) {
    //    //    Debug.Log(field);
    //    //}


    //    FieldInfo[] fields2 = chosenType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
    //                                                                                                                                            //FieldInfo[] fields2 = chosenType.GetFields(BindingFlags.Instance | BindingFlags.Public); // Does not include private, protected, or static
    //                                                                                                                                            //FieldInfo[] fields2 = chosenType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // Does not include static

    //    // In lexical order
    //    foreach (FieldInfo field in fields2) {
    //        Debug.Log(field);
    //    }

    //    Debug.LogWarning("NOW doing getMETHODS");

    //    //MethodInfo[] methods = chosenType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?)
    //    MethodInfo[] methods = chosenType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly); // ONLY base, target class (if derived, does not include inherited methods!!)
    //    //TODO: Consider how to handle inherited methods, BUT exclude System, Unity, etc packages
    //    //TODO: how to consider generic parameters?
    //    foreach (MethodInfo method in methods) {
    //        Debug.Log(method);
    //        Debug.Log("Declaring type: " + method.DeclaringType);
    //        //method.GetBaseDefinition(); // IF derived, overridden method, need to consider base implementation (if using base.Super() //lookup my lotb overides);
    //        //method.GetGenericArguments(); // HOW to handle generics?
    //        method.GetParameters();
    //    }



    //    //MAKE DERIVED / DERIVED / DERIVED CLASS...should changes be restricted just to the derived class? or, also base?


    //    //Debug.Log(ScoreManager.score);
    //    //Type[] uniqueObjects = (UnityEngine.Object[]) objs.Distinct();
    //    //             Asdasd d = objs.Distinct();
    //    //             for (int i = 0; i < uniqueObjects.Length; i++) {
    //    //                 Debug.LogWarning(uniqueObjects[i].name);
    //    //             }
    //    // 
    //    //             Debug.LogWarning(objs.Length);
    //    //             Debug.LogWarning("OMG!!");

    //    //         // Start the child process.
    //    //         System.Diagnostics.Process p = new System.Diagnostics.Process();
    //    //         // Redirect the output stream of the child process.
    //    //         p.StartInfo.UseShellExecute = false;
    //    //         p.StartInfo.RedirectStandardOutput = true;
    //    //         //p.StartInfo.FileName = Config.GitExectuable;
    //    //         //p.StartInfo.Arguments = command;
    //    //         p.StartInfo.Arguments = "git log";
    //    //         p.Start();
    //    //         // Read the output stream first and then wait.
    //    //         string output = p.StandardOutput.ReadToEnd();
    //    //         p.WaitForExit();

    //    //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "version", true));
    //    //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "log --follow --format=full --stat --since 01-01-01 -p Source/My2DGame/Private/Animator.cpp", true));
    //    //handle / to \
    //    //Cannot be outside repository
    //    //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "log --follow --format=full --stat --since 01-01-01 -p D:/Unreal/MannequinProject/UnrealPractice/Source/My2DGame/Private/Animator.cpp", true));

    //    //WORKS
    //    //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "log", true));

    //    //var output = RunProcess(string.Format(" --git-dir={0}/.git --work-tree={1} log --name-status", path.Replace("\\", "/"), path.Replace("\\", "/")));
    //}

    //public static string Execute(string executable,
    //    string arguments,
    //    bool standardOutput = false,
    //    bool standardError = false,
    //    bool throwOnError = false) {
    //    // This will be out return string
    //    string standardOutputString = "standard output string";
    //    string standardErrorString = "standard error string";

    //    // Use process
    //    System.Diagnostics.Process process;

    //    try {
    //        // Setup our process with the executable and it's arguments
    //        process = new System.Diagnostics.Process();
    //        process.StartInfo = new System.Diagnostics.ProcessStartInfo(executable, arguments);

    //        // To get IO streams set use shell to false
    //        process.StartInfo.UseShellExecute = false;

    //        // If we want to return the output then redirect standard output
    //        if (standardOutput) process.StartInfo.RedirectStandardOutput = true;

    //        // If we std error or to throw on error then redirect error
    //        if (standardError || throwOnError) process.StartInfo.RedirectStandardError = true;

    //        // Run the process
    //        process.Start();

    //        // Get the standard error
    //        if (standardError || throwOnError) standardErrorString = process.StandardError.ReadToEnd();

    //        //             // If we want to throw on error and there is an error
    //        //             if (throwOnError & amp; &amp; !string.IsNullOrEmpty(standardErrorString))
    //        //                 throw new Exception(
    //        //                     string.Format("Error in ConsoleCommand while executing {0} with arguments {1}.",
    //        //                     executable, arguments, Environment.NewLine, standardErrorString));

    //        // If we want to return the output then get it
    //        if (standardOutput) standardOutputString = process.StandardOutput.ReadToEnd();

    //        // If we want standard error then append it to our output string
    //        if (standardError) standardOutputString += standardErrorString;

    //        // Wait for the process to finish
    //        process.WaitForExit();
    //    }
    //    catch (Exception e) {
    //        // Encapsulate and throw
    //        throw new Exception(
    //            string.Format("Error in ConsoleCommand while executing {0} with arguments {1}.", executable, arguments), e);
    //    }

    //    // Return the output string
    //    return standardOutputString;
    //}
}
