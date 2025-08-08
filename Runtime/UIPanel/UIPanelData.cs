using System.Collections.Generic;
using UnityEngine;
using System;

namespace UuIiView
{
    public enum BlindType : int
    {
        None,
        NoAction,
        Close,
        Custom
    };


    [CreateAssetMenu(menuName ="UuIiView/UIPanelData")]
    public class UIPanelData : ScriptableObject
    {
        public string uiPanelPath;
        public GameObject canvasRoot;
        public List<PanelInfo> panels;
    }

    [Serializable]
    public class PanelInfo
    {
        public string name;
        public GameObject prefab;
        public int layerTypeIdx;
        public BlindType blindType;
        public bool cache;
    }
}