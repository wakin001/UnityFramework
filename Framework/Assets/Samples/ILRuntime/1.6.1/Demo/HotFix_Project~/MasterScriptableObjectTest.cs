using System;
using System.Diagnostics;
using Framework;
using UnityEngine;
using Gluon;
using Gluon.Master;
using UnityEditor;

namespace HotFix_Project
{
    public class MasterScriptableObjectTest
    {
        CharaLevel charaLevel;
        TestData testData;

        public MasterScriptableObjectTest()
        {
            
        }

        public void Execute()
        {
            ScriptableObject so = Resources.Load<ScriptableObject>("Masters/TestData");
            
            UnityEngine.Debug.Assert(testData != null, "testdata is null");
            UnityEngine.Debug.Log("testData: " + testData.f);

            //charaLevel = Resources.Load<CharaLevel>("Masters/CharaLevel");
            //UnityEngine.Debug.Assert(tes != null, "testdata is null");
            //UnityEngine.Debug.Log("testData: " + testData.f);
        }
    }
}
