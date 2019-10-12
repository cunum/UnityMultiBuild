using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEditor;

namespace MultiBuild {

    public class Settings : ScriptableObject {

        public string outputFolder;
        public bool useProductName;
        public string overrideName;
        public bool developmentBuild;
        public bool compress;
        public List<BuildTarget> targets;

        public void Reset() {
            outputFolder = Directory.GetParent(Application.dataPath).FullName;
            useProductName = true;
            overrideName = string.Empty;
            developmentBuild = false;
            compress = true;
            targets = new List<BuildTarget>();
        }
    }
}
