// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WhatHappened {
    public class CustomMethod {

        public MethodInfo info { get; }

        // Regex for matching this method's entire signature in source code
        public Regex regex { get; }

        // Signature for this method as it appeared in the source code
        private string methodSignature;

        // Regex for separating a type name from its container as it appears in the assembly
        private static readonly Regex containerRegex = new Regex(@"(?<name>[^\[`]+)(?<ending>[\[`\&].*?$)");

        // Keep track of all the changes to this method as detected through GitAnalyzer
        public int additions { get; set; }
        public int deletions { get; set; }
        public int totalChanges { get { return additions + deletions; } }
        public bool hasChanegd { get { return (totalChanges > 0); } }

        public int startLineNum { get; set; }
        public int endLineNum { get; set; }

        public CustomMethod(MethodInfo info) {
            this.info = info;
            regex = CreateMethodRegex();
        }

        /// <summary>
        /// Create the Regex that will be used in FileParser for finding this method's signature in source code
        /// </summary>
        /// <returns></returns>
        private Regex CreateMethodRegex() {

            // Create the pattern for all formal parameters
            System.Text.StringBuilder paramBuilder = new System.Text.StringBuilder("");
            foreach (ParameterInfo param in info.GetParameters()) {
                string simplifiedType = SimplifyTypeName(param.ParameterType);
                paramBuilder.Append(simplifiedType + @"\s*?" + param.Name + @"(\s*?,\s*?)??");
            }

            string simplifiedReturnType = SimplifyTypeName(info.ReturnType);
            string typeSpecifier = (info.DeclaringType.IsInterface) ? "interface" : "class";

            // Concatenate these patterns into full a method signature regex
            string methodPattern = @"(?<=\b" + typeSpecifier + @"\s+?" + info.DeclaringType.Name + @"\b) (?:.*?)(?<methodSig>" + simplifiedReturnType + @"\s+?" + info.Name + @"\s*?\(" + paramBuilder.ToString() + @"\s*?\))(?=\s*?{)";
            Regex methodRegex = new Regex(methodPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);

            return methodRegex;
        }

        /// <summary>
        /// Take a Type and return a pattern for its name as it would be appear in source code.
        /// E.g. convert "System.Collections.Generic.List`1[System.Int32]" to 
        /// "(System.Collections.Generic??List\<int\>"
        /// </summary>
        private string SimplifyTypeName(Type primitive) {

            string simplifiedName = primitive.Name;
            string arrayBrackets = "";

            // Match only the beginning of a type name if it is a container (e.g. "List`1[System.Int32]" to "List", "Int32[,]" to "Int32")
            if (primitive.IsArray || primitive.IsGenericType || primitive.IsByRef) {
                Match nameMatch = containerRegex.Match(primitive.Name);
                simplifiedName = nameMatch.Groups["name"].Captures[0].Value;

                // Capture any array brackets ("[]", "[][]", "[,,]" etc) and replace with escaped brackets
                arrayBrackets = nameMatch.Groups["ending"].Captures[0].Value;
                arrayBrackets = arrayBrackets.Replace("[", @"\[");
                arrayBrackets = arrayBrackets.Replace("]", @"\]");
            }

            //Simplify name if a primitive type (e.g. "System.Int32" to "int")
            simplifiedName = SimplifyPrimitiveTypeName(simplifiedName);

            //Simplify any generic parameters (and recursively include all nested generics)
            if (primitive.IsGenericType) {
                simplifiedName = SimplifyGenericTypeName(primitive);
            }
            //Re-attach the appropriate array brackets ("[]", "[,]", etc) if an array
            else if (primitive.IsArray) {
                simplifiedName += arrayBrackets;
            }

            if (primitive.Namespace == "") {
                return simplifiedName;
            }
            //Prepend name with optional namespace pattern
            else {
                //TODO need to consider partial namespace usage
                return @"(" + primitive.Namespace + @"\.)??" + simplifiedName;
            }

        }

        /// <summary>
        /// Change extended primitive type names to their equivalent names in shorthand source code,
        /// e.g. change "Int32" type from System.Int32 to "int", as it would commonly be written in code.
        /// List of built-in types taken from: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/built-in-types-table
        /// </summary>
        private string SimplifyPrimitiveTypeName(string originalName) {

            string convertedName = "";

            switch (originalName) {
                case "Byte":
                case "SByte":
                case "Char":
                case "Decimal":
                case "Double":
                case "Object":
                case "String":
                case "Void":
                    convertedName = originalName.ToLower();
                    break;

                case "Boolean":
                    convertedName = "bool";
                    break;
                case "Single":
                    convertedName = "float";
                    break;
                case "Int32":
                    convertedName = "int";
                    break;
                case "UInt32":
                    convertedName = "uint";
                    break;
                case "Int64":
                    convertedName = "long";
                    break;
                case "UInt64":
                    convertedName = "ulong";
                    break;
                case "Int16":
                    convertedName = "short";
                    break;
                case "UInt16":
                    convertedName = "ushort";
                    break;
                default:
                    //Debug.Log(primitive + " is not a primitive type.");
                    return originalName;
            }

            return convertedName;
        }

        /// <summary>
        /// Create a pattern for a given generic type to match its appearance in source code, further
        /// including all nested generic types by working recursively.
        /// </summary>
        private string SimplifyGenericTypeName(Type generic) {

            // Find the type name and append with opening angle bracket and optional whitespace
            string simplifiedName = generic.Name.Substring(0, generic.Name.IndexOf('`')) + @"\s*?\<\s*?";
            Type[] genericTypes = generic.GetGenericArguments();

            // Recursively call SimplifyTypeName on any generic parameters
            for (int i = 0; i < genericTypes.Length; i++) {
                simplifiedName += SimplifyTypeName(genericTypes[i]);

                // Either append with closing bracket (if last generic type) or comma (if more generics at this level)
                if (i < genericTypes.Length - 1) {
                    simplifiedName += @"\s*?,\s*?";
                }
                else {
                    simplifiedName += @"\s*?>";
                }
            }

            return simplifiedName;
        }

        /// <summary>
        /// Reset any previously recorded additions and deletions detected in this CustomType via the GitAnalyzer
        /// </summary>
        public void ClearPreviousChanges() {
            additions = deletions = 0;
        }

        /// <summary>
        /// Set method signature based on its matched appearance in the source code (from FileParser)
        /// </summary>
        public void SetSimplifiedMethodSignature(string sig) {
            // Trim excessive whitespace in signature
            methodSignature = Regex.Replace(sig, @"\s+", " "); //TODO assess performance hit
        }

        public string GetSimplifiedMethodSignature() {
            return methodSignature;
        }

        public override string ToString() {
            return info.Name;
        }
    }
}