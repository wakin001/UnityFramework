using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

[MessagePackObject]
public class MyClass : MessagePackDataBase
{
    // Key attributes take a serialization index (or string name)
    // The values must be unique and versioning has to be considered as well.
    // Keys are described in later sections in more detail.
    [Key(0)]
    public int Age { get; set; }

    [Key(1)]
    public string FirstName { get; set; }

    [Key(2)]
    public string LastName { get; set; }

    // All fields or properties that should not be serialized must be annotated with [IgnoreMember].
    [IgnoreMember]
    public string FullName { get { return FirstName + LastName; } }
}


public class MessagePackTest
{
    public void Test()
    {
        var mc = new MyClass
        {
            Age = 99,
            FirstName = "hoge",
            LastName = "huga",
        };

        // Call Serialize/Deserialize, that's all.
        byte[] bytes = MessagePackSerializer.Serialize(mc);
        Debug.Log("bytes.length: " + bytes.Length);
        MyClass mc2 = MessagePackSerializer.Deserialize<MyClass>(bytes);
        Debug.Log("mc2: " + mc2.ToString() );
        Debug.Log("after Deserialize, age: " + mc2.Age + ", first name: " + mc2.FirstName);

        // You can dump MessagePack binary blobs to human readable json.
        // Using indexed keys (as opposed to string keys) will serialize to MessagePack arrays,
        // hence property names are not available.
        // [99,"hoge","huga"]
        //var json = MessagePackSerializer.ConvertToJson(bytes);
        //Debug.Log(json);
    }
}
