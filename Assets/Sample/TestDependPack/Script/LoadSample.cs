using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadSample : MonoBehaviour {


    const string ab1Path = "";
    const string ab2Path = "";
    private void Start()
    {
        AssetBundle bundle = AssetBundle.LoadFromFile(Application.dataPath + "/Sample/");
    }
}
