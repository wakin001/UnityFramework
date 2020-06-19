using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SqliteTest : MonoBehaviour
{
    public SqliteDatabase sqlDB;

    // Use this for initialization
    void Start()
    {
        //打开测试数据库
        sqlDB = new SqliteDatabase("my_table.db");
        //sqlDB = new SqliteDatabase("test_db.db");
        //sqlDB.ExecuteNonQuery("CREATE TABLE 'chara_level' ( 'id' INTEGER PRIMARY KEY AUTOINCREMENT, 'necessary_exp' INTEGER, 'total_exp' INTEGER)");
        ////添加数据
        //sqlDB.ExecuteNonQuery("insert into chara_level (necessary_exp,total_exp) values(-123, 10)");
        //print("添加数据完毕");

        PrintAllData();

        //根据id删除最后添加的数据
        //sqlDB.ExecuteNonQuery("DELETE FROM example WHERE id = (SELECT MAX(id) FROM example)");
        //print("删除数据完毕");

        //PrintAllData();
    }

    //控制台打印数据
    void PrintAllData()
    {
        //读取example表中所有数据
        DataTable dt = sqlDB.ExecuteQuery("SELECT * FROM chara_level");
        //提取每行数据，遍历打印
        int id;
        int necessary_exp;
        int total_exp;
        foreach (DataRow dr in dt.Rows)
        {
            id = (int)dr["id"];
            necessary_exp = (int)dr["necessary_exp"];
            total_exp = (int)dr["total_exp"];
            print("id:" + id);
            print("necessary_exp:" + necessary_exp);
            print("total_exp:" + total_exp);
            //print("id:" + (int)dr["id"]);
        }
    }
}
