using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MessagePack;

[MessagePackObject]
public class ProtobufTest
{
    public void Test()
    {
        Debug.Log("ProtobufTest...");

        ILRuntime_mmopb.Initlize();

        var c = new mmopb.m_login_c();
        c.account = new mmopb.p_account_c();
        c.account.account = "abc";
        c.account.snapshots.Add(new mmopb.p_avatar_snapshot());
        c.account.snapshots[0].avatar = new mmopb.p_entity_basis();
        c.account.snapshots[0].avatar.account = "def";

        byte[] bytes;
        using (var stream = new System.IO.MemoryStream())
        {
            ProtoBuf.Serializer.Serialize(stream, c);
            Debug.Log("stream.Length: " + stream.Length);
            bytes = stream.ToArray();
        }

        using (var readStream = new System.IO.MemoryStream(bytes))
        {
            var t = ProtoBuf.Serializer.Deserialize(typeof(mmopb.m_login_c), readStream) as mmopb.m_login_c;
            Debug.Log("avatar.account: " + t.account.snapshots[0].avatar.account);
        }

        //chara data
        //ILRuntime_CharaDataPB.Initlize();
        //var baseAttr = new BaseAttrDict();
        //baseAttr.charLv = 99;
        //baseAttr.skNo = 12;

        //byte[] newbytes;
        //using (var stream = new System.IO.MemoryStream())
        //{
        //    ProtoBuf.Serializer.Serialize(stream, baseAttr);
        //    Debug.Log("baseAttr stream.Length: " + stream.Length);
        //    newbytes = stream.ToArray();
        //}

        //using (var readStream = new System.IO.MemoryStream(newbytes))
        //{
        //    var t = ProtoBuf.Serializer.Deserialize(typeof(BaseAttrDict), readStream) as BaseAttrDict;
        //    Debug.Log("BaseAttrDict.charalv: " + t.charLv + ", skNo: " + t.skNo);
        //}
    }
}
