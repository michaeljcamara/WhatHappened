using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text.RegularExpressions;
using GitSharp;

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

        //GitSharp.Git.Status(new GitSharp.Commands.StatusCommand());

        GitSharp.Commands.StatusCommand status = new GitSharp.Commands.StatusCommand();
        //Repository repo = new Repository(status.ActualDirectory);
        //Repository repo = new Repository(status.GitDirectory);

        //Repository repo = new Repository(GitSharp.Commands.ConfigCommand.FindGitDirectory(".", true, false));
        Repository repo = new Repository(GitSharp.Repository.FindRepository("."));

        Debug.LogWarning("Repo: " + repo.Directory + ", " + repo.WorkingDirectory);

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
        Debug.LogWarning("Iterating all files in Assets");
        foreach (FileInfo f in csFiles) {
            Debug.LogWarning("f: " + f + ", name: " + f.Name + ", fullName: " + f.FullName + ", length: " + f.Length);
            //output: f: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, name: ClassC.cs, fullName: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, length: 276

            Type[] typesInFile = GetTypesInFile(f);
        }
    }

    private Type[] GetTypesInFile(FileInfo file) {

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
        
         //string blockComments = @"/\*(.*?)\*/"; // another variant
        string lineComments = @"//(.*?)\r?\n";
        string strings = @"""((\\[^\n]|[^""\n])*)""";
        string verbatimStrings = @"@(""[^""]*"")+";
        //string blockComments = @"/\*

        //                            \*/";

        //string blockComments = @"/\*((.*)(?:\n*))*\*/";
        //string blockComments = @"/\*((.*?)(?:\n*))\*/";
        //string blockComments = @"/\*((.*?)(\n*))\*/";
        //string blockComments = @"/\*((.*?)(comment)(.*?))\*/";
        //string blockComments = @"/\*((.*?)(?:comment)(.*?))\*/";
        string blockComments = @"/\*((.*?)(?:\bcomment\b)(.*?))\*/";


        //string combinedPattern = blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings;
        string combinedPattern = blockComments + "|" + lineComments + "|^" + strings + "|^" + verbatimStrings;
        
        //**TRY SOMETHING LIKE regest.isMatch(blockComments) && !regex.isMatch(string) // So dont include strings

        Regex regex = new Regex(combinedPattern, RegexOptions.Singleline); // CAN use regexoptions, ignorecase
                                                                           //MatchEvaluator evaluator = new MatchEvaluator(CheckMatch)
        //text = regex.Replace(text, "");
        Regex lineCommentRegex = new Regex(lineComments, RegexOptions.Singleline); // CAN use regexoptions, ignorecase
                                                                                   //text = lineCommentRegex.Replace(text, "\n"); //WORKS!

        //Line comments, do something like: for each match, count \n, replace with "\n\n\n" based on num \n

        Regex blockCommentRegex = new Regex(blockComments, RegexOptions.Singleline); // Umm, multiline?
        //MatchCollection blockCommentCollection = regex.Matches(text);
        //foreach (Match m in blockCommentCollection) {
        //    Debug.Log("Found match: " + m.Value);
        //}

        //Debug.LogWarning("NumMatches: " + blockCommentRegex.Matches(text).Count);
        //foreach(Match m in blockCommentRegex.Matches(text)) {
        //    foreach(Group g in m.Groups) {

        //        Debug.Log("Group : " + g);

        //    }

        //    foreach(Capture c in m.Captures) {
        //        Debug.Log("Capture: " + c);
        //    }
        //}

        text = blockCommentRegex.Replace(text, "");
        

        Debug.LogWarning(text); 

        //string testText = "This is a /* first block \n\n\n comment last */ then nothing";
        string testText = "This is a /* first \nblock \n\n\n comment \nlast */ then \nnothing";
        //string testPattern = @"/\*(?:.*?)\*/";
        //string testPattern = @"(?:/\*(?:.*?)\*/)";
        //string testPattern = @"/\*(.*?)\*/";
        //string testPattern = @"/\*(.*?)(\n*)(.*?)\*/";
        //string testPattern = @"/\*.*(\n)*.*\*/"; 
        //string testPattern = @"/\*.*(\n*).*\*/";
        //string testPattern = @"/\*([^\n]*)(\n*)([^\n]*)\*/"; //works, only newlines in middle
        //string testPattern = @"/\*[^\n]*(\n*)[^\n]*\*/"; //works, only newlines in middle
        //string testPattern = @"/\*[^\n]*(\n)*[^\n]*\*/"; //works, only newlines in middle
        //string testPattern = @"/\*[^\n]*(?<newline>\n)*[^\n]*\*/"; //works, only newlines in middle

        //string testPattern = @"/\*([^\n]*(?<newline>\n)*)*?\*/"; //works
        //string testPattern = @"/\*(.*?(?<newline>\n)*)*?\*/"; //works, 1000* faster with .*?
        string testPattern = @"/\* #start with /*
                            (.*? #follow with any number of chars (NOT \n)
                            (?<newline>\n)*) #then any num consecutive \n 
                            *? #then repeat finding these occurences within /**/ boundary
                            \*/ #end with */
                            "; //works, 1000* faster with .*?
                            //Consider a way for lookahead, performance saver

        Regex testRegex = new Regex(testPattern, RegexOptions.IgnorePatternWhitespace);
        //Rem COMPILE option, faster? need different api compatibility?




        foreach (Match m in testRegex.Matches(testText)) {
            Debug.Log("**Num newlines: " + m.Groups["newline"].Captures.Count);

            //foreach (Group g in m.Groups) {

            //    Debug.Log("Group : " + g + ", " + g.Value.Length);

            //    foreach (Capture c in g.Captures) {
            //        Debug.Log("Capture WITHIN g: " + c + ", " + c.Length);
            //    }

            //}

            //foreach (Capture c in m.Captures) {
            //    Debug.Log("Capture: " + c);
            //}
        }



        testText = testRegex.Replace(testText, "-REPLACED-");
        Debug.LogWarning(testText);
        
        //Debug.LogWarning("NumMatches: " + blockCommentRegex.Matches();

        //    string noComments = Regex.Replace(text,
        //blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
        //me => {
        //    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
        //        return me.Value.StartsWith("//") ? Environment.NewLine : "";
        //    // Keep the literal strings
        //    return me.Value;
        //},
        //RegexOptions.Singleline);

        //    Debug.LogWarning(noComments);

        return null;
    }

    private string ReplaceBlockComments(Match m) {

        foreach(Group g in m.Groups) {
            string name = g.Value;
            Debug.Log("Group value = " + g.Value + ", Name = " + g.ToString());
        }

        return "";
    }

    

    public static void TESTTEST() {
        Debug.LogWarning("TESTING TESTING TESTING");
    }

    //public static void LogTest(string text,
    //                    [CallerFilePath] string file = "",
    //                    [CallerMemberName] string member = "",
    //                    [CallerLineNumber] int line = 0) {
    //    //Debug.LogWarning("TYPE: " + text + ", PATH: " + System.IO.Path.GetFileName(file)); // DependencyAnalyzer.cs
    //    Debug.LogWarning("TYPE: " + text + ", PATH: " + System.IO.Path.GetFullPath(file)); // D:/users/docs/... etc

    //}

    void TestingReflection() {

        //Debug.Log(Assembly.GetCallingAssembly().GetFiles().Length);

        UnityEngine.Object[] objs = Resources.FindObjectsOfTypeAll<MonoBehaviour>() as UnityEngine.Object[];

        ClassB a;

        for (int i = 0; i < objs.Length; i++) {
            Debug.LogWarning(objs[i].name + ", " + objs[i].GetType());
            //Debug.Log(objs[i].GetType().FullName);
            //Debug.Log(objs[i].GetType());
            //Debug.Log(objs[i].GetType().AssemblyQualifiedName);
        }

        UnityEngine.Object chosenObject = objs[3];
        Debug.Log(chosenObject.name + ", " + chosenObject.GetType());
        Type chosenType = chosenObject.GetType();
        MethodInfo methodInfo = chosenType.GetMethod("PrivateMethod1", BindingFlags.NonPublic | BindingFlags.Instance);
        FieldInfo fieldInfo = chosenType.GetField("testClass", BindingFlags.Public | BindingFlags.Instance);



        Debug.Log(fieldInfo);

        //System.Diagnostics.Process p;
        //p.Start();
        //Debug.Log(testa.FullName);


        Debug.Log(methodInfo);
        MethodBody methodBody = methodInfo.GetMethodBody();
        IList<LocalVariableInfo> localList = methodBody.LocalVariables;
        for (int i = 0; i < localList.Count; i++) {
            //Debug.Log(localList[i].ToString());
            Debug.Log("Local Type: " + localList[i].LocalType);
        }

        Debug.LogWarning("NOW doing getFIELDS");

        // This ignores PRIVATE, but includes STATIC
        //FieldInfo[] fields = chosenType.GetFields();
        //foreach(FieldInfo field in fields) {
        //    Debug.Log(field);
        //}


        FieldInfo[] fields2 = chosenType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
                                                                                                                                                //FieldInfo[] fields2 = chosenType.GetFields(BindingFlags.Instance | BindingFlags.Public); // Does not include private, protected, or static
                                                                                                                                                //FieldInfo[] fields2 = chosenType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic); // Does not include static

        // In lexical order
        foreach (FieldInfo field in fields2) {
            Debug.Log(field);
        }

        Debug.LogWarning("NOW doing getMETHODS");

        //MethodInfo[] methods = chosenType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?)
        MethodInfo[] methods = chosenType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly); // ONLY base, target class (if derived, does not include inherited methods!!)
        //TODO: Consider how to handle inherited methods, BUT exclude System, Unity, etc packages
        //TODO: how to consider generic parameters?
        foreach (MethodInfo method in methods) {
            Debug.Log(method);
            Debug.Log("Declaring type: " + method.DeclaringType);
            //method.GetBaseDefinition(); // IF derived, overridden method, need to consider base implementation (if using base.Super() //lookup my lotb overides);
            //method.GetGenericArguments(); // HOW to handle generics?
            method.GetParameters();
        }



        //MAKE DERIVED / DERIVED / DERIVED CLASS...should changes be restricted just to the derived class? or, also base?


        //Debug.Log(ScoreManager.score);
        //Type[] uniqueObjects = (UnityEngine.Object[]) objs.Distinct();
        //             Asdasd d = objs.Distinct();
        //             for (int i = 0; i < uniqueObjects.Length; i++) {
        //                 Debug.LogWarning(uniqueObjects[i].name);
        //             }
        // 
        //             Debug.LogWarning(objs.Length);
        //             Debug.LogWarning("OMG!!");

        //         // Start the child process.
        //         System.Diagnostics.Process p = new System.Diagnostics.Process();
        //         // Redirect the output stream of the child process.
        //         p.StartInfo.UseShellExecute = false;
        //         p.StartInfo.RedirectStandardOutput = true;
        //         //p.StartInfo.FileName = Config.GitExectuable;
        //         //p.StartInfo.Arguments = command;
        //         p.StartInfo.Arguments = "git log";
        //         p.Start();
        //         // Read the output stream first and then wait.
        //         string output = p.StandardOutput.ReadToEnd();
        //         p.WaitForExit();

        //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "version", true));
        //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "log --follow --format=full --stat --since 01-01-01 -p Source/My2DGame/Private/Animator.cpp", true));
        //handle / to \
        //Cannot be outside repository
        //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "log --follow --format=full --stat --since 01-01-01 -p D:/Unreal/MannequinProject/UnrealPractice/Source/My2DGame/Private/Animator.cpp", true));

        //WORKS
        //Debug.Log(Execute("C:/Program Files/Git/bin/git.exe", "log", true));

        //var output = RunProcess(string.Format(" --git-dir={0}/.git --work-tree={1} log --name-status", path.Replace("\\", "/"), path.Replace("\\", "/")));
    }

    public static string Execute(string executable,
        string arguments,
        bool standardOutput = false,
        bool standardError = false,
        bool throwOnError = false) {
        // This will be out return string
        string standardOutputString = "standard output string";
        string standardErrorString = "standard error string";

        // Use process
        System.Diagnostics.Process process;

        try {
            // Setup our process with the executable and it's arguments
            process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo(executable, arguments);

            // To get IO streams set use shell to false
            process.StartInfo.UseShellExecute = false;

            // If we want to return the output then redirect standard output
            if (standardOutput) process.StartInfo.RedirectStandardOutput = true;

            // If we std error or to throw on error then redirect error
            if (standardError || throwOnError) process.StartInfo.RedirectStandardError = true;

            // Run the process
            process.Start();

            // Get the standard error
            if (standardError || throwOnError) standardErrorString = process.StandardError.ReadToEnd();

            //             // If we want to throw on error and there is an error
            //             if (throwOnError & amp; &amp; !string.IsNullOrEmpty(standardErrorString))
            //                 throw new Exception(
            //                     string.Format("Error in ConsoleCommand while executing {0} with arguments {1}.",
            //                     executable, arguments, Environment.NewLine, standardErrorString));

            // If we want to return the output then get it
            if (standardOutput) standardOutputString = process.StandardOutput.ReadToEnd();

            // If we want standard error then append it to our output string
            if (standardError) standardOutputString += standardErrorString;

            // Wait for the process to finish
            process.WaitForExit();
        }
        catch (Exception e) {
            // Encapsulate and throw
            throw new Exception(
                string.Format("Error in ConsoleCommand while executing {0} with arguments {1}.", executable, arguments), e);
        }

        // Return the output string
        return standardOutputString;
    }

    public void Parser() {
        //System.IO.TextReader readFile = new StreamReader("C:\\csharp.net-informations.txt");
        //while (true) {
        //    line = readFile.ReadLine();
        //    if (line != null) {
        //        MessageBox.Show(line);
        //    }
        //}
        //readFile.Close();
        //readFile = null;
    }



}
