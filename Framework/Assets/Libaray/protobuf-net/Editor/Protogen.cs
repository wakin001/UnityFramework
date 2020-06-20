//#if UNITY_2018_1_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProtoBuf;
using System.IO;
using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;

public class Protogen {
    [MenuItem("Protobuf/Generate Protocs")]
    public static void GenerateProtobufCS(){
		Generate(Application.dataPath + "/Project/Resources/Protos/", new string[]{"CharData.proto"},Application.dataPath + "/Project/Resources/Protos/");
    }
    static void Generate(string inpath,string[] inprotos,string outpath){

		var set = new FileDescriptorSet();

		set.AddImportPath(inpath);
		foreach (var inproto in inprotos) {
			System.IO.TextReader readFile = new StreamReader(inpath + inproto);
			set.Add (inproto, true, readFile);
		}

		set.Process();
		var errors = set.GetErrors();
		CSharpCodeGenerator.ClearTypeNames ();
		var files = CSharpCodeGenerator.Default.Generate(set);

        int idx = 1;
		try
        {
			foreach (var file in files)
			{
				EditorUtility.DisplayProgressBar("Generate", file.Name, idx / (1.0f * inprotos.Length));
				var path = Path.Combine(outpath, file.Name);
				File.WriteAllText(path, file.Text);

				Debug.Log($"generated: {path}");
			}
		}
		catch (System.Exception e)
        {
			Debug.Log(e);
        }
		finally
        {
			EditorUtility.ClearProgressBar();
		}
    }
}
//#endif