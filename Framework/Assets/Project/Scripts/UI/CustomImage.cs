using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomImage : Image
{
    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        Color32 color32 = color;
        toFill.Clear();
        // 这里我用5对GameObject的坐标来与该Image对象的五个顶点绑定起来
        // AddVert的最后一个参数是UV值
        Vector2 size = rectTransform.sizeDelta;
        toFill.AddVert(new Vector3(0f, 0f, 0f), color32, new Vector2(0f, 0f));
        toFill.AddVert(new Vector3(size.x, 0f, 0f), color32, new Vector2(1f, 0f));
        toFill.AddVert(new Vector3(0f, size.y, 0f), color32, new Vector2(0f, 1f));
        toFill.AddVert(new Vector3(size.x, size.y, 0f), color32, new Vector2(1f, 1f));

        toFill.AddTriangle(0, 1, 2);
        //toFill.AddTriangle(1, 2, 3);



        //base.OnPopulateMesh(toFill);
    }
}
