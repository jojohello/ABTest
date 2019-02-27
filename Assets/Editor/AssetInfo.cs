using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;

namespace FGameCore
{
    // jojohello 2019-2-27
    public class AssetInfo
    {
        //是不是被打包文件夹下的直接资源
        public bool isRootAsset = false;
        private string rootName;

        public string AssetPath { get; private set; }

        // 两个队列共同作用，去除两者互为引用的状态.其实如果两者互相依赖，就应该两者都单独打包.而且这种状态貌似不应该存在.
        private HashSet<AssetInfo> childSet = new HashSet<AssetInfo>();
        public HashSet<AssetInfo> parentSet = new HashSet<AssetInfo>();

        public AssetInfo(string assetPath, string rootName = "", bool isRootAsset = false)
        {
            this.AssetPath = assetPath;
            if (string.IsNullOrEmpty(rootName) == false)
                this.rootName = rootName.ToLower();
        }
        public Object GetAsset()
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(AssetPath);
            return asset;
        }
        /// <summary>
        /// 从这里开始分析构建资源依赖树
        /// </summary>
        /// <param name="parent"></param>
        public void AddParent(AssetInfo parent)
        {
            if (parent == this || IsParentEarlyDep(parent) || parent == null)
                return;

            parentSet.Add(parent);
            parent.AddChild(this);

            parent.RemoveRepeatChildDep(this);
            RemoveRepeatParentDep(parent);
        }
        /// <summary>
        /// 清除我父节点对我子节点的重复引用，保证树形结构
        /// </summary>
        /// <param name="targetParent"></param>
        private void RemoveRepeatChildDep(AssetInfo targetChild)
        {

            List<AssetInfo> infolist = new List<AssetInfo>(parentSet);
            for (int i = 0; i < infolist.Count; i++)
            {
                AssetInfo pinfo = infolist[i];
                pinfo.RemoveChild(targetChild);
                pinfo.RemoveRepeatChildDep(targetChild);
            }
        }
        /// <summary>
        /// 清除我子节点被我父节点的重复引用，保证树形结构
        /// </summary>
        /// <param name="targetChild"></param>
        private void RemoveRepeatParentDep(AssetInfo targetParent)
        {

            List<AssetInfo> infolist = new List<AssetInfo>(childSet);
            for (int i = 0; i < infolist.Count; i++)
            {
                AssetInfo cinfo = infolist[i];
                cinfo.RemoveParent(targetParent);
                cinfo.RemoveRepeatParentDep(targetParent);
            }
        }

        private void RemoveChild(AssetInfo targetChild)
        {
            childSet.Remove(targetChild);
            targetChild.parentSet.Remove(this);
        }
        private void RemoveParent(AssetInfo parent)
        {
            parent.childSet.Remove(this);
            parentSet.Remove(parent);
        }


        private void AddChild(AssetInfo child)
        {
            childSet.Add(child);
        }

        /// <summary>
        /// 如果父节点早已当此父节点为父节点
        /// </summary>
        /// <param name="targetParent"></param>
        /// <returns></returns>
        private bool IsParentEarlyDep(AssetInfo targetParent)
        {
            if (parentSet.Contains(targetParent))
            {
                return true;
            }
            var e = parentSet.GetEnumerator();
            while (e.MoveNext())
            {
                if (e.Current.IsParentEarlyDep(targetParent))
                {
                    return true;
                }
            }
            return false;
        }
        public bool HasParent(AssetInfo p)
        {
            if (parentSet.Contains(p))
                return true;
            return false;
        }
    }
}
