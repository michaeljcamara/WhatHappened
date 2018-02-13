using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DependencyAnalyzer {

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

    // Use this for initialization
    public DependencyAnalyzer() {
        dependencyTable = new Dictionary<Type, HashSet<Type>>();


        //Debug.Log(Assembly.GetEntryAssembly().GetFiles());
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles().Length);
        Debug.Log(Assembly.GetExecutingAssembly().GetFiles()[0].Name); //D:\User\Documents\CMPSC\600\SeniorThesisPrototype\Prototype\Library\ScriptAssemblies\Assembly-CSharp.dll

        Assembly assembly = Assembly.GetExecutingAssembly();

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
            } 

            dependencyTable.Add(currentType, dependencySet);
        }
    }

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





}
