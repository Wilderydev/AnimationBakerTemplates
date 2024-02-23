﻿using UnityEditor;
using UnityEngine;

namespace AnimationBakerTemplates.Editor
{
    public class Tex2dToPng
    {
        [MenuItem("Custom/tex2png")]
        public static void SaveSelection()
        {
            var tex = (Texture2D)Selection.activeObject;
            if (tex == null)
                return;
            var pngData = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + "/tex.png", pngData);
        }
    }
}
