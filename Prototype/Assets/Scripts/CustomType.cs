using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class CustomType {

    //TODO change to readonly properties

    public Type type;
    public bool bHasChanged;
    public FileStream file;
    public string[] changedLines;
    public int numChangedLines;
    public int[] changedLineNums;

    public CustomType[] dependsOn, usedBy;
    public CustomType[] parents, children;

    public DateTime dateChanged;
    public string authorOfChange;
    public float impactStrength;

	public CustomType(Type type) {
        this.type = type;
    }
}
