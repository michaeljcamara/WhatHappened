using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassB : MonoBehaviour {

    public int publicInt;
    public float publicFloat;
    public ClassC publicCLassC;
    public double publicDouble;
    public string publicString;
    //public ClassC publicCLassC;

    private int privateInt;
    private float privateFloat;
    private double privateDouble;
    private string privateString;
    private ClassC privateClassC;

    protected int protectedInt;
    protected float protectedFloat;
    protected double protectedDouble;
    protected string protectedString;
    protected ClassC protectedClassC;

    static public int staticPublicInt;
    static public float staticPublicFloat;
    static public double staticPublicDouble;
    static public string staticPublicString;
    static public ClassC staticPublicCLassC;

    static private int staticPrivateInt;
    static private float staticPrivateFloat;
    static private double staticPrivateDouble;
    static private string staticPrivateString;
    static private ClassC staticPrivateClassC;

    static protected int staticProtectedInt;
    static protected float staticProtectedFloat;
    static protected double staticProtectedDouble;
    static protected string staticProtectedString;
    static protected ClassC staticProtectedClassC;

    


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
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
