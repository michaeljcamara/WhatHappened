using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq; //TODO any way to avoid "using system.linq" for just this one extension method? other way to do
using LibGit2Sharp;

public class DependencyAnalyzer {

    public Dictionary<Type, HashSet<Type>> dependencyTable;
    public Assembly assembly; // TODO REMOVE THIS EVENTUALLY

    //TODO IMPORTANT: Check if public/private classes WITHIN a class are captured by the GetALlTypes method
    HashSet<Type> GetFieldDependencies(Type type) {

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
        HashSet<Type> types = new HashSet<Type>();

        foreach (FieldInfo field in fields) {
            types.Add(field.FieldType);
            Debug.Log("In GetFieldDeps, cur field = " + field + ", fieldType = " + field.FieldType); //In GetFieldDeps, cur field = System.String authorOfChange, fieldType = System.String
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

    public DependencyAnalyzer() {

        dependencyTable = new Dictionary<Type, HashSet<Type>>();
        assembly = Assembly.GetExecutingAssembly();

        Debug.Log(Assembly.GetExecutingAssembly().GetFiles().Length);
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles()[0].Name); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Library\ScriptAssemblies\Assembly-CSharp.dll
        Debug.Log("TYPES: " + assembly.GetTypes());
        Debug.Log("TYPES LENGTH: " + assembly.GetTypes().Length);

        Type[] types = assembly.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type currentType = types[i];
            Debug.Log("CurrentType: " + currentType);

            HashSet<Type> dependencySet = new HashSet<Type>();

            dependencySet.UnionWith(GetFieldDependencies(currentType));
            dependencySet.UnionWith(GetMethodDependencies(currentType));
            //dependencySet.UnionWith(GetInheritanceDependencies()); //TODO

            dependencyTable.Add(currentType, dependencySet);

            //TODO exclude system, unityengine types
            foreach (Type type in dependencySet) {
                Debug.Log("HASHSET TYPES: " + type); //System.Int32
                //Debug.LogWarning(type.GetMethod("LogTest",BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
            }
        }

        Debug.LogWarning("Datapath is: " + Application.dataPath);
        DirectoryInfo assetDir = new System.IO.DirectoryInfo(Application.dataPath);
        FileInfo[] csFiles = assetDir.GetFiles("*.cs", System.IO.SearchOption.AllDirectories);

        //TODO consider EnumerateFiles, allows access before all array populated
        HashSet<string> typesAsStrings = new HashSet<string>();

        Debug.LogWarning("Iterating all .cs files in Assets");
        foreach (FileInfo f in csFiles) {
            Debug.LogWarning("f: " + f + ", name: " + f.Name + ", fullName: " + f.FullName + ", length: " + f.Length);
            //output: f: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, name: ClassC.cs, fullName: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Assets\Scripts\ClassC.cs, length: 276

            typesAsStrings.UnionWith(GetTypeStringInFile(f));
        }

        foreach (string typeString in typesAsStrings) {
            //TODO figure out what return types from libgit2sharp diff/log are
        }

        ////////////////////////////////////
        // LIBGIT2SHARP INTEGRATION
        ////////////////////////////////////
        // TODO generalize this repo path
        using (var repo = new Repository("D:/User/Documents/CMPSC/600/SeniorThesisPrototype")) {
            Debug.LogError("***REPO***: " + repo.Head.CanonicalName);
            foreach (var a in repo.Head.Commits) { // First element is MOST RECENT, last is the initial commit
                Debug.LogWarning("Commit message: " + a.Message); //Switched back to .NET 4.6 to allow use of LibGit2Sharp package (previous GitSharp package did not offer enough functionality)
                Debug.LogWarning("Author" + a.Author); // Michael Camara <michaeljcamara@gmail.com>
            }
            Debug.LogError("--------------");

            //repo.Head.Tip.Parents.SingleOrDefault<GitObject>(); // singleOrDefault<T> return default init of T, eg null. Single can throw exception
            foreach (Commit commit in repo.Commits) {
                foreach (var parent in commit.Parents) {
                    Debug.LogWarning(commit.Sha + " : " + commit.MessageShort);
                    foreach (TreeEntryChanges change in repo.Diff.Compare<TreeChanges>(parent.Tree,
                    commit.Tree)) {
                        Debug.LogWarning(change.Status + " : " + change.Path);
                    }
                }
            }

            Debug.LogError("--------------");

            string result = "-nothing-";
            //Path relative to where local .git directory stored, e.g. "." is D:/User/Documents/CMPSC/600/SeniorThesisPrototype
            string relPath = "Prototype/Assets/Scripts/DependencyAnalyzer.cs"; // Get path from individual file as iterating? FileInfo
            Debug.LogError("Full path: " + Path.GetFullPath(".")); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype
            List<Commit> CommitList = new List<Commit>();
            foreach (LogEntry entry in repo.Commits.QueryBy(relPath).ToList())
                CommitList.Add(entry.Commit);
            //CommitList.Add(null); // Added to show correct initial add

            // Commits in which this file was modified
            Debug.LogError("CommitList Count: " + CommitList.Count);
            foreach (Commit c in CommitList) {
                Debug.Log("Commit where changed: " + c.Message);
            }

            // General diff all paths
            TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>();
            Patch patch = repo.Diff.Compare<Patch>();

            // THIS narrows to SPECIFIC file (or, possibly, directory)
            List<string> stringPaths = new List<string>();
            stringPaths.Add(relPath);
            //TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>(stringPaths);
            //Patch patch = repo.Diff.Compare<Patch>(stringPaths);

            //GitObject a1, b1;
            //ContentChanges cc = repo.Diff.Compare(a1, b1);
            //ContentChanges ccc = repo.Diff.Compare((Blob) repo.Head.Tip.Tree[""].Target, (Blob) repo.Head.Tip.Tree[""].Target);
            //ccc.Patch; // STRING, maybe the whole diff comparison with +++/---???

            Debug.LogError("TARGET BLOB: " + CommitList[0].Tree[relPath].Target);  // 3a8a48js8dh8a489ha48h9
            Debug.LogError("TARGET BLOB: " + CommitList[1].Tree[relPath].Target);  // 3a8a48js8dh8a489ha48h9    

            Blob firstBlob = (Blob)CommitList[0].Tree[relPath].Target;
            Blob secondBlob = (Blob)CommitList[1].Tree[relPath].Target;

            //ContentChanges changes = repo.Diff.Compare(firstBlob, secondBlob);

            CompareOptions compOptions = new CompareOptions();
            //compOptions.Algorithm = DiffAlgorithm.Patience;  
            ContentChanges changes = repo.Diff.Compare(firstBlob, secondBlob, compOptions);

            ////TRYING BLAME!!///
            ////BlameOptions blameOpt; blameOpt.StartingAt; // indicate boundaries for class here, and commit reach
            //string fullPath = "D:/User/Documents/CMPSC/600/SeniorThesisPrototype/Prototype/Assets/Scripts/DependencyAnalyzer.cs";
            //BlameHunkCollection hunkCol = repo.Blame(relPath);
            //foreach(BlameHunk hunk in hunkCol) {
            //    //hunk.FinalStartLineNumber
            //    Debug.LogError("BLAME HUNK: " + hunk.ToString() + ", SIG: " + hunk.FinalSignature.ToString()); //Michael Camara, <michaeljcam..>

            //}

            string stringPatch = changes.Patch;
            Debug.LogError("THE PATCH: " + stringPatch);

            // Get working tree
            //Repo path: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\.git\, WorkingDir: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\
            Debug.LogError("Repo path: " + repo.Info.Path + ", WorkingDir: " + repo.Info.WorkingDirectory);

            //TODO how to change hunks, just solid block of text would be easier??? instead of @@-93,+34@@
            //NOTE ORDER of blobs makes no difference
            //ContentChanges reverseChanges = repo.Diff.Compare(secondBlob, firstBlob);
            //string reverseStringPatch = changes.Patch;
            //Debug.LogError("REVERSE PATCH: " + reverseStringPatch);
            /*
             * THE PATCH: @@ -6,12 +6,12 @@ using UnityEngine;
             using System.Runtime.CompilerServices;
             using System.IO;
             using System.Text.RegularExpressions;
            -//using GitSharp; //TODO Rem to remove this dependency
            +using GitSharp;
             * 
             */

            foreach (TreeEntryChanges change in treeChanges) {
                Debug.Log("TREE Change: " + change.Status + " : " + change.Path + " : " + change);
            }

            foreach (PatchEntryChanges change in patch) {
                Debug.Log("PATCH Change: " + change.Status + " : " + change.Path + " : " + change.LinesAdded + ";" + change.LinesDeleted + " : " + change);
            }
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
        Debug.LogWarning("TEST LINE: " + testLineComment);

        //TEST BLOCK COMMENT
        string testBlockText = "TestEscape\\nThis is a /* first testEscape\\n \nblock \n\n\n " +
            "comment \nlast */ then \nnothing";
        foreach (Match m in blockCommentRegex.Matches(testBlockText)) {
            Debug.Log("**Num newlines: " + m.Groups["newline"].Captures.Count);
        }
        testBlockText = blockCommentRegex.Replace(testBlockText, "-REPLACED-");
        Debug.LogWarning("BLOCK TEST: " + testBlockText);

        //TEST STRING
        string testStringText = "TestEscape\\\"Again\\\"OUT1\"First \"    " + "OUT2\"second \"" + "OUT3\"third.\"___OUT55";
        testStringText = stringRegex.Replace(testStringText, "");
        Debug.LogWarning(testStringText);

        //REPLACE FILE CONTENTS

        text = stringRegex.Replace(text, "");

        //text = blockCommentRegex.Match(text, );
        text = blockCommentRegex.Replace(text, delegate (Match m) {

            Debug.Log("Curr match = " + m.Value);

            //TODO any way to avoid "using system.linq" for just this one extension method? other way to do
            int numNewlines = m.Value.Count(x => x == '\n');
            Debug.Log("**Num newlines: " + numNewlines);
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
            for (int i = 0; i < numNewlines; i++) {
                stringBuilder.Append("\n");
            }

            return stringBuilder.ToString();
        });

        text = lineCommentRegex.Replace(text, "");

        Debug.LogWarning("TEXT: " + text);

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
        Debug.LogWarning("NEW TEXT: " + fileString.ToString());

        List<string> classStrings = new List<string>();
        string classPattern = @"(?<=\b class .*?)(?<class>\w+) (?=\s*?{)";
        Regex classRegex = new Regex(classPattern, RegexOptions.IgnorePatternWhitespace);
        string testClassString = "something class SomeClass {}\n something class SomeClass2{}";
        //testClassString = classRegex.Replace(testClassString, "-REPLACED-");
        Debug.LogWarning("TEST CLASS: " + testClassString);

        // Rem might need to trim classes
        foreach (Match m in classRegex.Matches(testClassString)) {
            foreach (Capture c in m.Groups["class"].Captures) {
                Debug.Log("Captured class: " + c);
                classStrings.Add(c.Value);
            }
        }
        return classStrings;
    }

}