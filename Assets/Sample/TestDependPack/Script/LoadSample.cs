using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class LoadSample : MonoBehaviour {

    const string abRoot = "/Sample/TestDependPack/";   // 如果用AssetStreaming文件夹，就不用这个东西
    const string abFolderName = "ABData";
    const string dataRootPath = "/Data/"; 

    const string ab1Path = "Object1.prefab";
    const string ab2Path = "FolderFor2/Object2.prefab";

    const string manifestPath = "ABData.manifest";

    private List<AssetBundle> tempBundleList = new List<AssetBundle>();

    private void Start()
    {
        Object object1 = LoadAsset("Data/" + ab1Path);
        if (object1 != null)
            Instantiate(object1 as GameObject, new Vector3(1, 0, 0), Quaternion.identity);


        Object object2 = LoadAsset("Data/" + ab2Path);
        if (object2 != null)
            Instantiate(object1 as GameObject, new Vector3(0, 0, 0), Quaternion.identity);
    }
    #region 最重要的部分在这里.
    private AssetBundleManifest manifest = null;
    private Object LoadAsset(string path)
    {
        AssetBundle assetBundle = LoadBundles(path);
        if (assetBundle != null)
        {
            Object ret = assetBundle.LoadAsset<Object>(GetFileName(path));
            foreach (AssetBundle bundle in tempBundleList)
                bundle.Unload(false);
            tempBundleList.Clear();
            //assetBundle.Unload(true);
            return ret;
        }
        else
            return null;
    }

    private AssetBundle LoadBundles(string path)
    {
        if(manifest == null)
        {
            LoadManiFest();
        }

        if(manifest != null)
        {
            string[] depends = manifest.GetAllDependencies(path);
            int count = depends.Length;
            for (int i=0; i<count; i++)
            {
                LoadBundles(depends[i]);
            }
        }

        string fullPath = Application.dataPath + abRoot + "/" + abFolderName + "/" + path.ToLower();
        AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
        if (bundle != null)
            tempBundleList.Add(bundle);

        return bundle;
    }

    private void LoadManiFest()
    {
        AssetBundle manifestBundle = AssetBundle.LoadFromFile(Application.dataPath + abRoot + abFolderName + "/" + abFolderName);
        if (manifestBundle == null)
            return;

        manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

        // 不能释放的，否则一释放上面的manifest也会变成null，厉害了。。。。。。
        //manifestBundle.Unload(true);
    }
    #endregion

    // 在实际项目中直接使用AssetStreaming文件夹，不需要这么麻烦拿真实路径,也不需要加入拿Sample等一系列的子目录.
    private StringBuilder tempStrBuilder = new StringBuilder();
    private string CreateFullPath(string path)
    {
        tempStrBuilder.Remove(0, tempStrBuilder.Length);
        tempStrBuilder.Append((dataRootPath + path).ToLower());
        tempStrBuilder.Insert(0, "/Sample/TestDependPack/ABData");
        tempStrBuilder.Insert(0, Application.dataPath);

        return tempStrBuilder.ToString();
    }

    private string GetFileName(string path)
    {
        int index = path.LastIndexOf('/');
        if (index < 0)
            return path;
        else if (index == path.Length - 1)
            return "";
        else
            return path.Substring(index + 1, path.Length - index - 1);
    }
}

