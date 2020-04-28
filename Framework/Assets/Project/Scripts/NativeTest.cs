using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class NativeTest : MonoBehaviour
{
    const string dll = "NativeTest";

    [DllImport(dll)]
    private static extern void SeedRandomizer();

    [DllImport(dll)]
    private static extern int DieRoll(int sides);

    [DllImport(dll)]
    private static extern float Add(float a, float b);

    [DllImport(dll)]
    private static extern float Random();

    /// <summary>
    /// char* in c++ => IntPtr in C#
    /// </summary>
    /// <returns></returns>
    [DllImport(dll)]
    private static extern IntPtr HelloWorld();

    private void Start()
    {
        SeedRandomizer();

        Debug.Log(Add(3.2f, 4.5f).ToString());
        Debug.Log(DieRoll(20).ToString());
        Debug.Log(Random().ToString());
        Debug.Log(Marshal.PtrToStringAnsi(HelloWorld()));
    }
}
