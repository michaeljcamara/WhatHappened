using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq; //TODO any way to avoid "using system.linq" for just this one extension method? other way to do
//using LibGit2Sharp;

public class DependencyAnalyzer {

    public Dictionary<Type, HashSet<Type>> dependencyTable;
    public Assembly assembly; // TODO REMOVE THIS EVENTUALLY

    //TODO IMPORTANT: Check if public/private classes WITHIN a class are captured by the GetALlTypes method
    //YES: CurrentType: ClassB+anotherClass1 //TODO need to handle this corrrectly, indicate private class within ClassB
    //TODO: Need to consider datastructures of types, like List<SomeType> or SomeType[]
    //List: System.Collections.Generic.List`1[CustomMethod]

    HashSet<Type> GetFieldDependencies(Type type) {

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static); // Includes all?
        HashSet<Type> types = new HashSet<Type>();

        foreach (FieldInfo field in fields) {
            types.Add(field.FieldType);
            Debug.Log("In GetFieldDeps, cur field = " + field + ", name = " + field.Name + ", fieldType = " + field.FieldType); //In GetFieldDeps, cur field = System.String authorOfChange, fieldType = System.String
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
        customTypeLookup = new Dictionary<string, CustomType>(); //NEWW

        Debug.Log(Assembly.GetExecutingAssembly().GetFiles().Length);
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles()[0].Name); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Library\ScriptAssemblies\Assembly-CSharp.dll
        Debug.Log("TYPES: " + assembly.GetTypes());
        Debug.Log("TYPES LENGTH: " + assembly.GetTypes().Length);

        Type[] types = assembly.GetTypes();
        for (int i = 0; i < types.Length; i++) {
            Type currentType = types[i];
            Debug.Log("CurrentType: " + currentType);

            HashSet<Type> dependencySet = new HashSet<Type>();

            //currentType.GetNestedTypes();

            dependencySet.UnionWith(GetFieldDependencies(currentType));
            dependencySet.UnionWith(GetMethodDependencies(currentType));
            //dependencySet.UnionWith(GetInheritanceDependencies()); //TODO

            dependencyTable.Add(currentType, dependencySet);

            //TODO exclude system, unityengine types
            //foreach (Type type in dependencySet) {
            //    Debug.Log("HASHSET TYPES: " + type); //System.Int32
            //    Debug.Log("Type name: " + type.Name + ", namespace: " + type.Namespace); //CustomType, namespace: 
            //    //Debug.LogWarning(type.GetMethod("LogTest",BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance));
            //}
            Debug.LogError("Type name: " + currentType.Name + ", fullname: " + currentType.FullName + ", isNested(has+): " + currentType.IsNested + ", namespace: " + currentType.Namespace); //Type name: anotherClass1, fullname: ClassB+anotherClass1, isNested(has+): True, namespace: // NEWWW
            
            customTypeLookup.Add(currentType.Name, new CustomType(currentType)); //NEWW

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

            //typesAsStrings.UnionWith(GetTypeStringInFile(f));

            AnalyzeFile(f);
        }

        foreach (string typeString in typesAsStrings) {
            //TODO figure out what return types from libgit2sharp diff/log are
        }

        //////////////////////////////////////
        //// LIBGIT2SHARP INTEGRATION
        //////////////////////////////////////
        //// TODO generalize this repo path
        //using (var repo = new Repository("D:/User/Documents/CMPSC/600/SeniorThesisPrototype")) {
        //    Debug.LogError("***REPO***: " + repo.Head.CanonicalName);
        //    foreach (var a in repo.Head.Commits) { // First element is MOST RECENT, last is the initial commit
        //        Debug.LogWarning("Commit message: " + a.Message); //Switched back to .NET 4.6 to allow use of LibGit2Sharp package (previous GitSharp package did not offer enough functionality)
        //        Debug.LogWarning("Author" + a.Author); // Michael Camara <michaeljcamara@gmail.com>
        //    }
        //    Debug.LogError("--------------");

        //    //repo.Head.Tip.Parents.SingleOrDefault<GitObject>(); // singleOrDefault<T> return default init of T, eg null. Single can throw exception
        //    foreach (Commit commit in repo.Commits) {
        //        foreach (var parent in commit.Parents) {
        //            Debug.LogWarning(commit.Sha + " : " + commit.MessageShort);
        //            foreach (TreeEntryChanges change in repo.Diff.Compare<TreeChanges>(parent.Tree,
        //            commit.Tree)) {
        //                Debug.LogWarning(change.Status + " : " + change.Path);
        //            }
        //        }
        //    }

        //    Debug.LogError("--------------");

        //    string result = "-nothing-";
        //    //Path relative to where local .git directory stored, e.g. "." is D:/User/Documents/CMPSC/600/SeniorThesisPrototype
        //    string relPath = "Prototype/Assets/Scripts/DependencyAnalyzer.cs"; // Get path from individual file as iterating? FileInfo
        //    Debug.LogError("Full path: " + Path.GetFullPath(".")); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype
        //    List<Commit> CommitList = new List<Commit>();
        //    foreach (LogEntry entry in repo.Commits.QueryBy(relPath).ToList())
        //        CommitList.Add(entry.Commit);
        //    //CommitList.Add(null); // Added to show correct initial add

        //    // Commits in which this file was modified
        //    Debug.LogError("CommitList Count: " + CommitList.Count);
        //    foreach (Commit c in CommitList) {
        //        Debug.Log("Commit where changed: " + c.Message);
        //    }

        //    // General diff all paths
        //    TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>();
        //    Patch patch = repo.Diff.Compare<Patch>();

        //    // THIS narrows to SPECIFIC file (or, possibly, directory)
        //    List<string> stringPaths = new List<string>();
        //    stringPaths.Add(relPath);
        //    //TreeChanges treeChanges = repo.Diff.Compare<TreeChanges>(stringPaths);
        //    //Patch patch = repo.Diff.Compare<Patch>(stringPaths);

        //    //GitObject a1, b1;
        //    //ContentChanges cc = repo.Diff.Compare(a1, b1);
        //    //ContentChanges ccc = repo.Diff.Compare((Blob) repo.Head.Tip.Tree[""].Target, (Blob) repo.Head.Tip.Tree[""].Target);
        //    //ccc.Patch; // STRING, maybe the whole diff comparison with +++/---???

        //    Debug.LogError("TARGET BLOB: " + CommitList[0].Tree[relPath].Target);  // 3a8a48js8dh8a489ha48h9
        //    Debug.LogError("TARGET BLOB: " + CommitList[1].Tree[relPath].Target);  // 3a8a48js8dh8a489ha48h9    

        //    Blob firstBlob = (Blob)CommitList[0].Tree[relPath].Target;
        //    Blob secondBlob = (Blob)CommitList[1].Tree[relPath].Target;

        //    //ContentChanges changes = repo.Diff.Compare(firstBlob, secondBlob);

        //    CompareOptions compOptions = new CompareOptions();
        //    //compOptions.Algorithm = DiffAlgorithm.Patience;  
        //    ContentChanges changes = repo.Diff.Compare(firstBlob, secondBlob, compOptions);

        //    ////TRYING BLAME!!///
        //    ////BlameOptions blameOpt; blameOpt.StartingAt; // indicate boundaries for class here, and commit reach
        //    //string fullPath = "D:/User/Documents/CMPSC/600/SeniorThesisPrototype/Prototype/Assets/Scripts/DependencyAnalyzer.cs";
        //    //BlameHunkCollection hunkCol = repo.Blame(relPath);
        //    //foreach(BlameHunk hunk in hunkCol) {
        //    //    //hunk.FinalStartLineNumber
        //    //    Debug.LogError("BLAME HUNK: " + hunk.ToString() + ", SIG: " + hunk.FinalSignature.ToString()); //Michael Camara, <michaeljcam..>

        //    //}

        //    string stringPatch = changes.Patch;
        //    Debug.LogError("THE PATCH: " + stringPatch);

        //    // Get working tree
        //    //Repo path: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\.git\, WorkingDir: D:\User\Documents\CMPSC\600\SeniorThesisPrototype\
        //    Debug.LogError("Repo path: " + repo.Info.Path + ", WorkingDir: " + repo.Info.WorkingDirectory);

        //    //TODO how to change hunks, just solid block of text would be easier??? instead of @@-93,+34@@
        //    //NOTE ORDER of blobs makes no difference
        //    //ContentChanges reverseChanges = repo.Diff.Compare(secondBlob, firstBlob);
        //    //string reverseStringPatch = changes.Patch;
        //    //Debug.LogError("REVERSE PATCH: " + reverseStringPatch);
        //    /*
        //     * THE PATCH: @@ -6,12 +6,12 @@ using UnityEngine;
        //     using System.Runtime.CompilerServices;
        //     using System.IO;
        //     using System.Text.RegularExpressions;
        //    -//using GitSharp; //TODO Rem to remove this dependency
        //    +using GitSharp;
        //     * 
        //     */

        //    foreach (TreeEntryChanges change in treeChanges) {
        //        Debug.Log("TREE Change: " + change.Status + " : " + change.Path + " : " + change);
        //    }

        //    foreach (PatchEntryChanges change in patch) {
        //        Debug.Log("PATCH Change: " + change.Status + " : " + change.Path + " : " + change.LinesAdded + ";" + change.LinesDeleted + " : " + change);
        //    }
        //}
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

        AnalyzeFile(file);
        return classStrings;
    }

    // ALL BELOW IS NEW
    //REM:
    /*
     * Add CustomTypeLookup to other, plus populating before
     * 
     * */
    Dictionary<string, CustomType> customTypeLookup;

    private void AnalyzeFile(FileInfo file) {

        //TODO add this to instance vars in original class


        //TEMP
        //if (!file.Name.Equals("ClassB.cs")) {
        //    return;
        //}

        if (file.Name.Equals("CustomType.cs") || file.Name.Equals("CustomMethod.cs") || file.Name.Equals("DependencyAnalyzer.cs")) {
            return;
        }

        Debug.LogError("**STARTING FILE: " + file.Name);

        string text = File.ReadAllText(file.FullName);
        string[] textByLine = File.ReadAllLines(file.FullName);
        //Debug.LogError("Num lines in _" + file.Name + "= " + textByLine.Length);

        List<string> classStrings = new List<string>();
        string classPattern = @"(?<leading>.*?)(?<=\bclass\s+)(?<class>\w+\b) (?=.*{)"; // TODO OR with interface
        //string classPattern = @"(?<leading>.*?)(?<=\bclass\s+)(?<class>\w+\b) (?=.*{)(?<therest>.*$)"; // just grabs first class, since everything included in match
        Regex classRegex = new Regex(classPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        string testClassString = "something class SomeClass {}\n something class SomeClass2{}";
        //testClassString = classRegex.Replace(testClassString, "-REPLACED-");
        //Debug.LogWarning("TEST CLASS: " + testClassString);

        //TODO AFTER WAKEUP 3-24. Include private classes, or no? extents of additions needed to do this...
        // Also, recursive inclusion of generic arguments in custommethod
        //TODO ALSO what about 2d,3d,4d array?? [,] [,,] [][][][][][]

        int currentLineNum = 1; // +1 since the leading text does not capture the \n from the current line
        //Debug.LogError("Now iterating class matches");
        foreach (Match classMatch in classRegex.Matches(text)) {
            Debug.Log("Current match: " + classMatch);
            Debug.Log("LEADING CAPTURE: " + classMatch.Groups["leading"].Captures[0]);
            currentLineNum += classMatch.Groups["leading"].Captures[0].Value.Count(x => x == '\n');

            //TODO Handle namespace conflicts
            string classString = classMatch.Groups["class"].Captures[0].Value;
            Debug.Log("Captured class: " + classString);
            CustomType targetType;
            if(!customTypeLookup.TryGetValue(classString, out targetType)) {
                Debug.LogError("Could not find " + classString + " in customTypeLookup");
            }

            //TODO how to handle private classes, with names as ClassB+anotherClass1
            targetType.startLineNum = currentLineNum;

            for(int i = targetType.startLineNum - 1, numOpenBraces = 0, numClosedBraces = 0; i < textByLine.Length && targetType.endLineNum == 0; i++) {
                foreach(char c in textByLine[i]) {
                    if(c == '{') {
                        numOpenBraces++;
                    }
                    else if(c == '}') {
                        numClosedBraces++;
                    }

                    if (numOpenBraces == numClosedBraces && numOpenBraces != 0) {
                        targetType.endLineNum = i + 1; // +1 since array is zero-indexed but line numbers are 1-indexed
                        break;
                    }
                }
            }

            /* RENABLE
            if(targetType.endLineNum == 0) {
                Debug.LogError("Could not find end line number for " + classString);
            }*/

            Debug.Log("StartLineNum = " + targetType.startLineNum + ", EndLineNum = " + targetType.endLineNum);
            Debug.Log("How many methods in " + targetType + ": " + targetType.methods.Count);
            //Now find start/end of each method in each class using regex patterns
            int numMatchesFound = 0;
            foreach (CustomMethod method in targetType.GetMethods()) {
                Debug.Log("Looking for method: " + method.info);
                Regex methodRegex = method.regex;
                Debug.Log("Regex is: " + methodRegex.ToString());


                //TODO should only need one match

                //foreach (Match methodMatch in methodRegex.Matches(text)) {
                Match methodMatch = methodRegex.Match(text);
                if(methodMatch.Success) {
                    Debug.Log("Found match: " + methodMatch);

                    Debug.LogWarning("    Method Signature: " + methodMatch.Groups["methodSig"].Captures[0].Value);
                    numMatchesFound++;
                }
                else {
                    Debug.LogError("!!DID NOT FIND: " + method.info.Name);
                }

                Debug.LogWarning("Leading: " + methodMatch.Groups["leading"].Captures[0].Value);

                //Now get start and end method line numbers
                //TODO instead of going back to start of class declaration each time, start at end of previous method signature match
                //currentLineNum += methodMatch.Groups["leading"].Captures[0].Value.Count(x => x == '\n');
                currentLineNum = targetType.startLineNum + methodMatch.Groups["leading"].Captures[0].Value.Count(x => x == '\n');

                method.startLineNum = currentLineNum;

                for (int i = method.startLineNum - 1, numOpenBraces = 0, numClosedBraces = 0; i < textByLine.Length && method.endLineNum == 0; i++) {
                    foreach (char c in textByLine[i]) {
                        if (c == '{') {
                            numOpenBraces++;
                        }
                        else if (c == '}') {
                            numClosedBraces++;
                        }

                        if (numOpenBraces == numClosedBraces && numOpenBraces != 0) {
                            method.endLineNum = i + 1; // +1 since array is zero-indexed but line numbers are 1-indexed
                            break;
                        }
                    }
                }

                /* RENABLE!
                if (method.endLineNum == 0) {
                    Debug.LogError("Could not find end line number for " + method.info.Name);
                }
                else {
                    Debug.LogError("Start/End Line Nums for " + method.info.Name + ": " + method.startLineNum + ", " + method.endLineNum);
                }*/


            }

            if (numMatchesFound == targetType.methods.Count) {
                Debug.LogError("**FOUND ALL METHODS IN " + targetType + ": " + numMatchesFound + " out of " + targetType.methods.Count);

            }
            else {
                Debug.LogError("!!DID NOT FIND ALL METHODS IN " + targetType + ": " + numMatchesFound + " out of " + targetType.methods.Count);
            }


        }

        ////string classPattern = @"(?<leading>.*?)(?<=\bclass\s+)(?<class>\w+\b) (?=.*{)(?<therest>.*$)";
        ////                [^{}]*(((?'Open'{)[^{}]*)+((?'Close-Open'})[^{}]*)+)*(?(Open)(?!))"; // This kinda works, no leading, still includes OuterClass in match (separate capture)

        ////string pattern = "^[^{}]*" +
        ////               "(" +
        ////               "((?'Open'{)[^{}]*)+" +
        ////               "((?'Close-Open'})[^{}]*)+" +
        ////               ")*" +
        ////               "(?(Open)(?!))$";
        //Regex bracesRegex = new Regex(pattern, RegexOptions.IgnorePatternWhitespace);

        //Debug.LogError("Now iterating open-close matches");
        //foreach (Match m in bracesRegex.Matches(text)) {
        //    Debug.LogWarning("FOUND MATCH!!: " + m);
        //    //Debug.Log("TRYING TO GET CLOSEOPEN: " + m.Groups["Close"].Captures[0]);

        //    Debug.Log("Iterating OPEN captures: ");
        //    foreach(Capture c in m.Groups["Open"].Captures) {
        //        Debug.LogWarning("       " + c);
        //    }

        //    Debug.Log("Iterating CLOSE captures: ");
        //    foreach (Capture c in m.Groups["Close"].Captures) {
        //        Debug.LogWarning("       " + c);
        //    }

        //    Debug.Log("Iterating CLOSE-OPEN"); // Captured ClassB
        //    foreach(Capture c in m.Groups["Close-Open"].Captures) {
        //        Debug.LogWarning("       " + c);
        //    }
        //}


        ////string testPattern = @"(?<=start)(?<middle>\s*middle\s*)(?<theend>(?=end))";
        //string testPattern = @"(?<=start)(?<middle>\s*middle\s*)(?=end)";
        //Regex testRegex = new Regex(testPattern);
        //string testString = "start middle end\nstart middle end";

        //Debug.LogError("Testing for matches startend");
        //foreach(Match m in testRegex.Matches(testString)) {
        //    Debug.Log("Match = " + m);

        //    foreach(Group g in m.Groups) {
        //        Debug.Log("   Group = " + g);

        //        foreach(Capture c in g.Captures) {
        //            Debug.Log("      Capture: " + c);
        //        }
        //    }

        //    //foreach(Capture c in m.Groups["theend"].Captures) {
        //    //    Debug.Log("     Capture: " + c);
        //    //}
        //}



        //text = classRegex.Replace(text, delegate (Match m) {

        //    Debug.Log("Curr match = " + m.Value);
        //    int numNewlines = m.Value.Count(x => x == '\n');
        //    Debug.Log("**Num newlines: " + numNewlines);
        //    System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
        //    for (int i = 0; i < numNewlines; i++) {
        //        stringBuilder.Append("\n");
        //    }

        //    return stringBuilder.ToString();
        //});



        // Rem might need to trim classes
        //foreach (Match m in classRegex.Matches(testClassString)) {
        foreach (Match m in classRegex.Matches(text)) {
            foreach (Capture c in m.Groups["class"].Captures) {
                Debug.Log("Captured class: " + c);
                classStrings.Add(c.Value);
            }
        }

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
}