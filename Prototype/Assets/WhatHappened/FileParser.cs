// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using System.IO;

namespace WhatHappened {
    public class FileParser {

        private Regex lineCommentRegex { get; }
        private Regex blockCommentRegex { get; }
        private Regex stringRegex { get; }
        private Regex typeRegex { get; }

        /// <summary>
        /// Initialize all of the Regex objects that will be used to parse all of the C# files in the project
        /// </summary>
        public FileParser() {

            string lineCommentPattern = @"(?<=^.*)(//.*?$)"; //TODO Exclude if */ at end
            lineCommentRegex = new Regex(lineCommentPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

            string blockCommentPattern = @"/\*.*?\*/"; // TODO exclude escaped backslash
            blockCommentRegex = new Regex(blockCommentPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);

            string stringPattern = @"(?<=^.*)(?<!\\)(""[^""]*?"")"; // TODO exclude escaped quotes, //TODO include literal @ strings
            stringRegex = new Regex(stringPattern, RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

            string typePattern = @"\b(?:class|interface)(?:\s+?)(?<type>[_\w]+?\b)(?=[^{]*?{)";
            typeRegex = new Regex(typePattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
        }


        /// <summary>
        /// Remove all line comments, block comments, and strings from the text in the given CustomFile.
        /// Replaces block comments with newline characters to maintain consistent line numbering
        /// </summary>
        private string RemoveCommentsAndStrings(CustomFile file) {

            string text = File.ReadAllText(file.info.FullName);

            text = stringRegex.Replace(text, "");

            // Replace block comments with the same number of newlines that appear in the comment
            text = blockCommentRegex.Replace(text, delegate (Match m) {
                int numNewlines = m.Value.Count(x => x == '\n');
                System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("");
                for (int i = 0; i < numNewlines; i++) {
                    stringBuilder.Append("\n");
                }
                return stringBuilder.ToString();
            });

            text = lineCommentRegex.Replace(text, "");

            return text;
        }

        public List<CustomType> MatchTypesInFile(CustomFile file) {

            List<CustomType> typesInFile = new List<CustomType>();

            // Remove comments and strings to ensure no false positive matches
            string text = RemoveCommentsAndStrings(file);

            int currentLineNum = 1; 
            int currentIndex = 0; // Index of text string where current match begins
            CustomType previousType = null; // Keep track of the previous match's type

            // Iterate through all class declarations found in the file code
            foreach (Match typeMatch in typeRegex.Matches(text)) {

                //Keep track of current line number by counting newlines since the last match
                currentLineNum += text.Substring(currentIndex, typeMatch.Index - currentIndex).Count(x => x == '\n');
                currentIndex = typeMatch.Index;

                // Find the start and end lines of the matched type
                int startLineNum = currentLineNum;
                int endLineNum = FindEndLineNum(typeMatch.Index, currentLineNum, text);

                // Get the name of the type as it appears in the text
                System.Text.StringBuilder typeString = new System.Text.StringBuilder("");
                typeString.Append(typeMatch.Groups["type"].Captures[0].Value);

                // Prepend type string with any of its parents (e.g. if B is nested in A, then string becomes "A+B") 
                typeString.Insert(0, FindParentTypesInText(previousType, startLineNum, endLineNum));

                // Get the CustomType associated with the string found in text (modified if type was nested to match desired format of key)
                CustomType currentType = DependencyAnalyzer.GetCustomTypeFromString(typeString.ToString());
                if (currentType == null) {
                    Debug.LogError("Could not find " + typeString + " in customTypeLookup");
                }

                // Record the file, start, end line nums in the CustomType
                currentType.file = file;
                currentType.startLineNum = startLineNum;
                currentType.endLineNum = endLineNum;

                // Attempt to match all methods of the current type within the text, simularly recording start and end line nums
                ParseMethodsInFile(currentType, text, typeMatch.Index, currentLineNum);

                // Reset linenum/index back to the currentType before moving onto next type
                currentLineNum = currentType.startLineNum;
                currentIndex = typeMatch.Index;
                previousType = currentType;

                typesInFile.Add(currentType);

                // Debugging to ensure all methods found in text, compared to what is contained in the assembly
                //Debug.LogError("Class matched: " + classString + ", StartLineNum = " + currentType.startLineNum + ", EndLineNum = " + currentType.endLineNum);
                //if (numMatchesFound == currentType.methods.Count) {
                //    Debug.LogError("**FOUND ALL METHODS IN " + currentType + ": " + numMatchesFound + " out of " + currentType.methods.Count);
                //}
                //else {
                //    Debug.LogError("!!DID NOT FIND ALL METHODS IN " + currentType + ": " + numMatchesFound + " out of " + currentType.methods.Count);
                //}
            } // end type match iteration

            return typesInFile;
        }

        /// <summary>
        /// Determine if the matched type has any parents.  If so, create the text that should be prepended
        /// onto the given type to match the format used in the customTypeLookup from DependencyAnalyzer.
        /// E.g. if B is not nested, return an empty string.  If B is nested in A, return "A+".
        /// If C is nested in B which is nested in A, return "A+B+".
        /// </summary>
        private string FindParentTypesInText(CustomType previousType, int startLineNum, int endLineNum) {
            System.Text.StringBuilder typeString = new System.Text.StringBuilder();

            // Handle nested types (e.g. private classes nested in other classes, potentially many nested levels)
            bool foundParent = false;
            do {
                // No outer type detected, i.e. match is not nested
                if (previousType == null) {
                    foundParent = true;
                }
                //Check if match is nested in previous matched type
                else if (startLineNum >= previousType.startLineNum && endLineNum <= previousType.endLineNum) {
                    Type parent = previousType.assemblyType;
                    while (parent.IsNested) {
                        typeString.Insert(0, parent.Name + "+");
                        parent = parent.DeclaringType;
                    }
                    typeString.Insert(0, parent.Name + "+");
                    foundParent = true;
                }
                //Not nested in previous matched type. Check if nested in that type's parent (if it exists)
                else {
                    if (previousType.assemblyType.DeclaringType != null) {
                        previousType = DependencyAnalyzer.GetCustomTypeFromString(CustomType.SimplifyTypeFullName(previousType.assemblyType.DeclaringType));
                    }
                    else {
                        previousType = null;
                    }
                }
            } while (!foundParent);

            return typeString.ToString();
        }

        /// <summary>
        /// Find the endLineNum for a method or type by stepping through text until the matching closing brace is found
        /// </summary>
        private int FindEndLineNum(int startIndex, int currentLineNum, string fileText) {
            int endLineNum = 0;

            for (int i = startIndex, numOpenBraces = 0, numClosedBraces = 0, numNewLines = 0; i < fileText.Length; i++) {
                switch (fileText[i]) {
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
                    endLineNum = currentLineNum + numNewLines;
                    break;
                }
            }

            return endLineNum;
        }

        /// <summary>
        /// Use the Regex objects created in each CustomMethod to attempt to find the method declaration within
        /// the file text.  If found, start and end line numbers are recorded, which are used during diffs with GitAnalyzer.
        /// </summary>
        private void ParseMethodsInFile(CustomType currentType, string fileText, int typeMatchIndex, int currentLineNum) {
            int numMatchesFound = 0;

            foreach (CustomMethod method in currentType.methods) {
                Regex methodRegex = method.regex;

                Match methodMatch = methodRegex.Match(fileText);
                if (methodMatch.Success) {
                    numMatchesFound++;
                }
                else {
                    Debug.LogError("!!DID NOT FIND: " + method.info.Name);
                    continue;
                }

                // Determine what line the match was on by counting newlines between previous match
                currentLineNum = currentType.startLineNum + fileText.Substring(typeMatchIndex, methodMatch.Groups["methodSig"].Index - typeMatchIndex).Count(x => x == '\n');

                // Record the start and end line numbers for this method
                method.startLineNum = currentLineNum;
                method.endLineNum = FindEndLineNum(methodMatch.Groups["methodSig"].Index, currentLineNum, fileText);

                // Record how this method signature appeared in the text (which may differ from assembly representation)
                method.SetSimplifiedMethodSignature(methodMatch.Groups["methodSig"].Value);

                // Debugging to ensure correct line nums recorded
                //if (method.endLineNum == 0) {
                //    Debug.LogError("Could not find end line number for " + method.info.Name);
                //}
                //else {
                //    //Debug.LogError("Start/End Line Nums for " + method.info.Name + ": " + method.startLineNum + ", " + method.endLineNum);
                //    Debug.LogError("    Method matched: " + method.info.Name + ", StartLineNum = " + method.startLineNum + ", EndLineNum = " + method.endLineNum);
                //}


            }
        }
    }
}