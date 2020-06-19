using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Linq;
using Gluon.Master;
using LitJson;

namespace Stage
{
    /// <summary>
    /// 
    /// </summary>
    public class CreateScriptableObject
    {
        [MenuItem("Assets/CreateScriptableObject for CharaLevelScriptableObject")]
        public static void Export()
        {
            var instance = ScriptableObject.CreateInstance<CharaLevelScriptableObject>();
            instance.items = new List<CharaLevelElementScriptableObject>();

            string path = "Assets/Project/Resources/Masters/CharaLevel_json.txt";

            //Read the text from directly from the test.txt file
            StreamReader reader = new StreamReader(path);
            string jsonText = reader.ReadToEnd();
            reader.Close();
            
            //var textAsset = AssetDatabase.LoadAssetAtPath("Assets/Project/Resources/Masters/CharaLevel_json", typeof(TextAsset)) as TextAsset;
            var json = JsonMapper.ToObject<CharaLevelJson>(jsonText);
            foreach (var item in json.items)
            {
                CharaLevelElementScriptableObject elem = new CharaLevelElementScriptableObject() { id = item.id, necessary_exp = item.necessary_exp, total_exp = item.total_exp };
                instance.items.Add(elem);
            }
            
            AssetDatabase.CreateAsset(instance, "Assets/Project/Resources/Masters/CharaLevel_so.asset");
            AssetDatabase.SaveAssets();
        }
    }
}


