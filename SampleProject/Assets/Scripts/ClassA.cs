using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ClassA : MonoBehaviour {

    public int testInt;
    public float testFloat; 
    public ClassB testClass;
    public ClassD d;

    // Use this for initialization
    void Start() {
        testClass.PublicMethod1();
    }

    // Update is called once per frame
    void Update() {

    }

    public void PublicMethod1() {
        int localInt;
        float localFloat;
    }

    private void PrivateMethod1() {
        int localInt;
        float localFloat;
    }
}


