using System.Reflection;
using UnityEngine;

public class SqliteTest
{
    SqliteDatabase sqlDB;

    class CharaLevelSqliteData
    {
        public int id;
        public int necessary_exp;
        public int total_exp;
    }

    public void ExecuteQuery()
    {
        sqlDB = new SqliteDatabase("my_table.db");
        DataTable dt = sqlDB.ExecuteQuery("SELECT * FROM chara_level");
        //提取每行数据，遍历打印
        int id;
        int necessary_exp;
        int total_exp;
        foreach (DataRow dr in dt.Rows)
        {
            CharaLevelSqliteData charaLevelItem = new CharaLevelSqliteData();

            id = (int)dr["id"];
            necessary_exp = (int)dr["necessary_exp"];
            total_exp = (int)dr["total_exp"];
            Debug.Log("id:" + id);
            Debug.Log("necessary_exp:" + necessary_exp);
            Debug.Log("total_exp:" + total_exp);
        }

        ExecuteQueryToClass();
    }

    public void ExecuteQueryToClass()
    {
        Debug.Log("ExecuteQueryToClass");
        if (sqlDB == null)
        {
            sqlDB = new SqliteDatabase("my_table.db");
        }
        DataTable dt = sqlDB.ExecuteQuery("SELECT * FROM chara_level");
        //提取每行数据，遍历打印
        foreach (DataRow dr in dt.Rows)
        {
            CharaLevelSqliteData charaLevelItem = new CharaLevelSqliteData();
            MethodInfo[] infoes = charaLevelItem.GetType().GetMethods();
            foreach (var info in infoes)
            {
                Debug.Log("info: " + info.Name + ", membertype: " + info.MemberType);
                if (info.Name == "id")
                {
                    charaLevelItem.id = (int)dr[info.Name];
                }
                else if (info.Name == "necessary_exp")
                {
                    charaLevelItem.necessary_exp = (int)dr[info.Name];
                }
                else if (info.Name == "total_exp")
                {
                    charaLevelItem.total_exp = (int)dr[info.Name];
                }

                Debug.Log("charaLevelItem.id:" + charaLevelItem.id);
                Debug.Log("charaLevelItem.necessary_exp:" + charaLevelItem.necessary_exp);
                Debug.Log("charaLevelItem.total_exp:" + charaLevelItem.total_exp);
            }
        }
    }
}