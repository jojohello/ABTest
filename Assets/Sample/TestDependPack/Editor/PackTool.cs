using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FGameCore;

public class PackTool {

    private const string dataPath = "Assets/Sample/TestDependPack/Data";
    private const string targetPath = "Assets/Sample/TestDependPack/ABData";

    [MenuItem("FGame/PackData")]
    private static void PackData()
    {
        ABPacker.Prepare(2, ProduceABName);
        ABPacker.Pack(dataPath, targetPath, BuildTarget.Android);
    }

    // abName产生规则：从rootName之后开始，去掉后缀，并全小写.
    // 可以根据个人喜好去修改
    // 注意，如果使用了
    private static string ProduceABName(string path)
    {
        //StringBuilder strBuilder = new StringBuilder(path);
        //int index = 0;
        //if (string.IsNullOrEmpty(rootName) == false)
        //{
        //    index = strBuilder.
        //}
        string ret = path.ToLower();
        string rootName = "/testdependpack/";

        int index = 0;
        if (string.IsNullOrEmpty(rootName) == false)
        {
            index = ret.IndexOf(rootName);
            if (index > 0)
            {
                index += rootName.Length;
                ret = ret.Remove(0, index);
            }
        }

        //index = ret.LastIndexOf(".");
        //ret = ret.Remove(index, ret.Length - index);

        //ret = ret.Replace('/', '.');
        //ret = ret.Replace('\\', '.');
        //

        return ret;
    }
}
