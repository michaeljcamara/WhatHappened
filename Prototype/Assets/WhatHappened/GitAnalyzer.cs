// Author: Michael Camara
// Repository: https://github.com/michaeljcamara/WhatHappened

using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WhatHappened {
    public class GitAnalyzer {
         
        private Repository repo { get; }
        public List<Commit> commitList { get; }
        private CompareOptions compareOptions { get; }
        private ExplicitPathsOptions pathOptions { get; }
        private Regex hunkRegex { get; }

        public GitAnalyzer() {
            
            repo = FindGitRepo();
            if(repo == null) {
                return;
            }

            commitList = new List<Commit>();
            foreach (Commit c in repo.Commits) {
                commitList.Add(c);
            }

            // Cofigure diff options so patch only includes modified lines
            compareOptions = new CompareOptions();
            compareOptions.ContextLines = 0; // if 0, only include modified lines in diff (has + or - at start of line). 3 is default
            compareOptions.IncludeUnmodified = true;
            compareOptions.InterhunkLines = 0;  // if Int.MAX then all changes are in ONE hunk
            pathOptions = new ExplicitPathsOptions();

            // Create Regex to match each hunk in the diff patch
            string diffPattern = @"\@\@\s\-(?:\d+)(?:,\d+)??\s\+(?<start>\d+)(?:,\d+)??\s\@\@
                            (?:[^\n]*?\n)(?<changes>.*?)(?=\n^@ | $\Z)
                            ";
            hunkRegex = new Regex(diffPattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Multiline);
            //TODO figure out way to sep chunks via regex
        }

        /// <summary>
        /// Find the Git repository associated with this project
        /// TODO Abort if no repo found
        /// </summary>
        private Repository FindGitRepo() {
            DirectoryInfo gitDir = new DirectoryInfo(Path.GetFullPath("."));
            bool isFound = false;
            while (!isFound) {
                DirectoryInfo[] dirs = gitDir.GetDirectories(".git");

                if (dirs.Length == 0) {
                    if (gitDir.Parent == null) {
                        Debug.LogError("!!Could not find Git directory working backwards, abort!");
                        return null;
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

            return new Repository(gitDir.FullName);
        }

        /// <summary>
        /// Use LibGit2Sharp to perform a Git diff procedure on the given file, comparing the current working directory
        /// against the file version at the given commit index.  Returns the total number of lines changed.
        /// </summary>
        public int DiffFile(CustomFile file, int commitIndex) {

            // Return if the given file was null or if user selected "NO SELECTION" index from DependencyWindow drop-down
            if (file == null || commitIndex == -1) {
                return 0;
            }
            file.ClearPreviousChanges();

            // Select the Git tree and file path for the given CustomFile
            LibGit2Sharp.Tree chosenTree = commitList[commitIndex].Tree;
            List<string> chosenFilePath = new List<string>() { file.relPath };

            // Perform the Git diff and create a string for the resulting patch
            Patch patch = repo.Diff.Compare<Patch>(chosenTree, DiffTargets.WorkingDirectory, chosenFilePath, pathOptions, compareOptions);
            PatchEntryChanges changes = patch[file.relPath];
            string patchText = changes.Patch; // If no changes b/w commits, then "", or if notIncludeModified, null changes

            // Update the CustomFile with this patchText for future direct access
            file.diffPatchText = patchText;

            // Parse the patch to localize additions and deletions to the appropriate CustomTypes and CustomMethods
            int totalFileChanges = ParseDiffPatch(patchText, file);

            //Debugging output for checking where changes were found in file
            //List<CustomType> typesInFile = file.types;
            //foreach (CustomType t in typesInFile) {
            //    Debug.LogError("Total changes in " + t + ": " + t + ", inMethods: " + t.totalChangesInMethods + ", outMethods: " + t.totalChangesOutsideMethods);
            //    foreach (CustomMethod m in t.methods) {
            //        Debug.LogWarning("   Total changes in " + m + ": " + (m.additions + m.deletions) + "(Adds: " + m.additions + ", Dels: " + m.deletions + ")");
            //    }
            //}

            return totalFileChanges;
        }

        /// <summary>
        /// Match each hunk in the diff patch, then localize each change in the hunk to the appropriate CustomMethod
        /// and/or CustomType.  Returns the total number of changes detected in the file
        /// </summary>
        private int ParseDiffPatch(string patchText, CustomFile file) {

            int totalFileChanges = 0;

            foreach (Match m in hunkRegex.Matches(patchText)) {

                string hunk = m.Groups["changes"].Captures[0].Value;
                int lineNum = Int32.Parse(m.Groups["start"].Captures[0].Value);

                // Read the patch line-by-line
                System.Text.StringBuilder fileString = new System.Text.StringBuilder("");
                StringReader reader = new StringReader(hunk);
                string line;
                while ((line = reader.ReadLine()) != null) {

                    // Determine what method and/or type is at the matched line number
                    CustomType typeAtLine = file.GetTypeByLineNumber(lineNum);
                    CustomMethod methodAtLine = null;
                    if (typeAtLine != null) {
                        methodAtLine = typeAtLine.GetMethodAtLineNum(lineNum);
                    }

                    // Update additions and deletions for the determined CustomMethod and/or CustomType
                    //Note: changes outside the bounds of any type are not recorded
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
                                totalFileChanges++;
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
                                totalFileChanges++;
                            }
                            break;
                        default:
                            Debug.LogError("Unknown hunk format for: " + hunk);
                            break;
                    }

                }

            //Debugging output for checking hunk matches
            //Debug.LogError("Found match: " + m);
            //Debug.LogError("   Hunk is:-" + m.Groups["changes"].Captures[0].Value + "-");
            //Debug.LogError("     LineNum is: " + m.Groups["start"].Captures[0].Value);
        }

        return totalFileChanges;
        }
    }
}