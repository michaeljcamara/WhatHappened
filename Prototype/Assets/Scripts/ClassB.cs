using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClassB : MonoBehaviour {
    public ClassA classA;
    public ClassB classB;
    System.Text.StringBuilder GetBuilder(int[] nums, List<string> names) { return null; }

    //TODO WHAT ABOUT INTERFACES?? (clas | interfac) during regex?
    public int publicInt;
    public float publicFloat;
    public ClassC publicCLassC;
    public double publicDouble;
    public string publicString;
    //public ClassC publicCLassC;

    public List<List<List<ClassD>>> nestedGenericD;

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
    //TODO rem spacing after brackets braces optional
    Dictionary<Dictionary<System.Text.StringBuilder, System.Text.StringBuilder[,,,]>, Dictionary<System.Text.StringBuilder[][][], List<System.Text.StringBuilder[][]>>> CrazyMethod(Dictionary<Dictionary<System.Text.StringBuilder, System.Text.StringBuilder[,,,]>, Dictionary<System.Text.StringBuilder[][][], List<System.Text.StringBuilder[][]>>> Arguments) { return null; }
    int[][] TwoDArrayInts(int[][] someInts) { return null; }
    int[,] TwoDArrayInts2(int[,] someInts) { return null; }
    int[][][] ThreeDArrayInts(int[][][] someInts) { return null; }
    int[,,] ThreeDArrayInts2(int[,,] someInts) { return null; }
    List<int> ListOfInts2(List<int> someInts2) { return null; }
    Dictionary<List<int>, Dictionary<List<int>, List<int>>> DictOfLists() { return null; }
    List<List<int>> ListOfLists(List<List<int>> someInts) { return null; }
    List<List<System.Text.StringBuilder>> ListOfListsBuilders(List<List<System.Text.StringBuilder>> someBuilders) { return null; }

    System.Text.StringBuilder HasParameters(string someString, ClassC someClass, int someInt, System.Text.StringBuilder someBuilder) {
        System.Text.StringBuilder testBuilder = new System.Text.StringBuilder("");

        return testBuilder;
    }

    //*TEST WITH ARRAYS< LISTS< GENERICS< ETC

    List<int> ListOfInts(List<int> someInts) { return null; }
    List<System.Text.StringBuilder> ListOfStringBuilders(List<System.Text.StringBuilder> someStringBuilders) { return null; }
    int[] ArrayOfInts(int[] someInts) { return null; }
    System.Text.StringBuilder[] ArrayOfStringBuilders(System.Text.StringBuilder[] someStringBuilders) { return null; }

    private class NestedClass1 {
        private class NestedClass1_1 {
            private class NestedClass1_1_1 {
                void SuperNestedMethod() {

                }
            }
        }
        private class NestedClass1_2{
            private class NestedClass1_2_1 {
                void SuperNestedMethod() {

                }
            }
        }
    }


    // Use this for initialization
    void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    //todo overloaded methods

    public void PublicMethod1() {
        int localInt;
        float localFloat;
    }

    private void PrivateMethod1() {
        int localInt;
        float localFloat;
    }
    
    private class anotherClass1 
        
        {
        private int nestedInt;
        private class NestedInAnotherClass {

        }
    }

    private class

        anotherClass2 {

    }

    static

    void

        someMethod

        (
        
        ) 
        
        {
        int a;if
            (true
            ) {

        }
    }     
}

public class OutsideClassB {

}
