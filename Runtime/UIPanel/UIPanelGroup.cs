using System.Collections.Generic;
using UnityEngine;
using System;

namespace UuIiView
{
    [CreateAssetMenu(menuName = "UuIiView/UIPanelGroup")]
    public class UIPanelGroup : ScriptableObject
    {
        public List<UIGroup> groups;
    }

    [Serializable]
    public class UIGroup
    {
        public string name;
        public List<string> panelNames;
    }
}