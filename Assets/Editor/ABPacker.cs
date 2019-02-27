using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace FGameCore
{
    // jojohello 2019-2-27
    // ABPacker主要是提供打包ab的方法，根据项目不同，目录的路径也会不同.
    // 具体的设置以及界面、工具栏之类的东西，请在外部编写.
    // 另，此代码也是改自网络文章，非原创.
    //  作者：漫漫之间n
    //  来源：CSDN
    //  原文：https://blog.csdn.net/u012740992/article/details/79371986 
    public class ABPacker
    {
        static Dictionary<string, AssetInfo> assetInfoDict = new Dictionary<string, AssetInfo>();

        private static string curRootAsset = string.Empty;
        private static float curProgress = 0f;
        private static string rootFolderPath = string.Empty;
        private static string rootFlag = "";    // assetbundle的那么，从这个字符串之后开始.

        public static int pieceThreshold = 2;   // 控制颗粒度，被依赖次数大于等于这个值才会单独打包.
        public delegate string ProduceBundleNameFun(string inputPath);
        static public ProduceBundleNameFun produceBundleName = null;    // 生成bundleName的函数，会影响到打包路径，会影响到访问路径

        // 在要进行打包之前 可以设置一些参数
        public static void Prepare(int newThreshold = 2, ProduceBundleNameFun fun = null)
        {
            pieceThreshold = newThreshold;
            produceBundleName = fun;
        }

        public static bool Pack(string packFolderPath, string targetFolderPath, BuildTarget platform)
        {
            if(string.IsNullOrEmpty(packFolderPath))
            {
                Debug.LogError("打包目录为空，请选择需要打包的目录");
                return false;
            }

            if(Directory.Exists(packFolderPath) == false)
            {
                Debug.LogError("打包目录不存在，请选择正确的打包目录");
                return false;
            }

            if (string.IsNullOrEmpty(targetFolderPath))
            {
                Debug.LogError("目标目录为空，请选择目标的目录");
                return false;
            }

            if (Directory.Exists(targetFolderPath) == false)
            {
                Directory.CreateDirectory(targetFolderPath);
            }

            rootFolderPath = packFolderPath;
            assetInfoDict.Clear();
            CleaarAllABNames();
            GetAllAssets(packFolderPath);

            BuildPipeline.BuildAssetBundles(targetFolderPath, BuildAssetBundleOptions.ChunkBasedCompression, platform);
            AssetDatabase.Refresh();
            return true;
        }

        //[MenuItem("AssetBundleMgr / SetAssetbundleName")]

        //[MenuItem("AssetBundleMgr / ClearAllAssetbundelname")]
        public static void CleaarAllABNames()
        {
            string[] abnames = AssetDatabase.GetAllAssetBundleNames();
            foreach (var n in abnames)
            {
                AssetDatabase.RemoveAssetBundleName(n, true);
            }
        }
        
        public static void GetAllAssets(string rootDir)
        {
            assetInfoDict.Clear();

            DirectoryInfo dirinfo = new DirectoryInfo(rootDir);
            FileInfo[] fs = dirinfo.GetFiles("*.*", SearchOption.AllDirectories);
            int ind = 0;
            foreach (var f in fs)
            {
                curProgress = (float)ind / (float)fs.Length;
                curRootAsset = "正在分析依赖：" + f.Name;
                EditorUtility.DisplayProgressBar(curRootAsset, curRootAsset, curProgress);
                ind++;
                int index = f.FullName.IndexOf("Assets");
                if (index != -1)
                {
                    string assetPath = f.FullName.Substring(index);
                    Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    string upath = AssetDatabase.GetAssetPath(asset);
                    if ( asset != null
                        && assetInfoDict.ContainsKey(assetPath) == false
                        && assetPath.StartsWith("Assets")
                        && !(asset is MonoScript)
                        && !(asset is LightingDataAsset)
                        )
                    {
                        AssetInfo info = new AssetInfo(upath, rootFlag, true);
                        //标记一下是文件夹下根资源
                        CreateDeps(info);
                    }
                    EditorUtility.UnloadUnusedAssetsImmediate();
                }
                EditorUtility.UnloadUnusedAssetsImmediate();
            }
            EditorUtility.ClearProgressBar();

            int setIndex = 0;
            foreach (KeyValuePair<string, AssetInfo> kv in assetInfoDict)
            {
                EditorUtility.DisplayProgressBar("正在设置ABName", kv.Key, (float)setIndex / (float)assetInfoDict.Count);
                setIndex++;
                AssetInfo a = kv.Value;
                SetAssetBundleName(a);
            }
            EditorUtility.ClearProgressBar();
            EditorUtility.UnloadUnusedAssetsImmediate();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 递归分析每个所被依赖到的资源
        /// </summary>
        /// <param name="self"></param>
        /// <param name="parent"></param>
        static void CreateDeps(AssetInfo self, AssetInfo parent = null)
        {
            if (self.HasParent(parent))
                return;
            if (assetInfoDict.ContainsKey(self.AssetPath) == false)
            {
                assetInfoDict.Add(self.AssetPath, self);
            }
            self.AddParent(parent);

            Object[] deps = EditorUtility.CollectDependencies(new Object[] { self.GetAsset() });
            for (int i = 0; i < deps.Length; i++)
            {
                Object o = deps[i];
                if (o is MonoScript || o is LightingDataAsset)
                    continue;
                string path = AssetDatabase.GetAssetPath(o);
                if (path == self.AssetPath)
                    continue;
                if (path.StartsWith("Assets") == false)
                    continue;
                AssetInfo info = null;
                if (assetInfoDict.ContainsKey(path))
                {
                    info = assetInfoDict[path];
                }
                else
                {
                    info = new AssetInfo(path);
                    assetInfoDict.Add(path, info);
                }
                EditorUtility.DisplayProgressBar(curRootAsset, path, curProgress);
                CreateDeps(info, self);
            }
            EditorUtility.UnloadUnusedAssetsImmediate();
        }

        private static void SetAssetBundleName(AssetInfo info)
        {
            AssetImporter ai = AssetImporter.GetAtPath(info.AssetPath);
            //针对UGUI图集的处理 jojohello 2019-2-27 这坨东西应该扔出去处理.
            if (ai is TextureImporter)
            {
                TextureImporter tai = ai as TextureImporter;
                if (!string.IsNullOrEmpty(tai.spritePackingTag))
                {
                    //AssetBundleName和spritePackingTag保持一致
                    tai.SetAssetBundleNameAndVariant(tai.spritePackingTag, null);
                }
            }
            else
            {
                string abname = "";
                if (produceBundleName != null)
                    abname = produceBundleName(info.AssetPath);
                else
                    abname = info.AssetPath;

                //不是图集，而且大于阀值
                if (info.parentSet.Count >= pieceThreshold)
                {
                    ai.SetAssetBundleNameAndVariant(abname, string.Empty);
                }
                //根节点
                else if (info.parentSet.Count == 0)
                {
                    ai.SetAssetBundleNameAndVariant(abname, string.Empty);
                }
                else if (info.isRootAsset)
                {
                    ai.SetAssetBundleNameAndVariant(abname, string.Empty);
                }
                else
                {
                    //其余的子资源
                    ai.SetAssetBundleNameAndVariant(string.Empty, string.Empty);
                }
            }
        }
    }
}
