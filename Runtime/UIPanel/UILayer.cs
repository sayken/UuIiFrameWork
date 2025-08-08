using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace UuIiView
{
    public class UILayer : MonoBehaviour
    {
        UIPanelData uiPanelData;
        public List<string> layerType = new List<string>();
        Dictionary<string, RectTransform> layerContent = new Dictionary<string, RectTransform>();
        Dictionary<string,int> layerCount = new Dictionary<string,int>();
        GameObject canvasRoot;

        GameObject blind;
        GameObject tapLock;
        bool reservedSort = false;

        Dictionary<string, UIPanel> panelCaches = new Dictionary<string, UIPanel>();

        public Router Router { get; private set; }

        /// <summary>　Panelを閉じたとき、所属レイヤーが全て閉じられたら呼ばれる </summary>
        public Action<string> OnAllClosed;
        public Action<string> OnFirstOpened;

        public void Initialize(UIPanelData uiPanelData)
        {
            this.uiPanelData = uiPanelData;

            layerType.Clear();
            layerContent.Clear();

            var cr = transform.Find("CanvasRoot");
            canvasRoot = cr == null ? Instantiate(uiPanelData.canvasRoot, transform) : cr.gameObject;
            canvasRoot.name = "CanvasRoot";

            foreach ( Transform ts in canvasRoot.transform )
            {
                var content = ts.Find("Content");
                if ( content == null )
                {
                    continue;
                }
                layerContent[ts.name] = content.GetComponent<RectTransform>();
                layerCount[ts.name] = 0;
                layerType.Add(ts.name);
            }

            tapLock = canvasRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.gameObject.name == "TapLock").gameObject;
            blind = canvasRoot.GetComponentsInChildren<Transform>(true).FirstOrDefault(_ => _.gameObject.name == "Blind").gameObject;
            if (blind == null)
            {
                Debug.LogError("CanvasRoot has no Blind gameobject");
            }
            if (tapLock == null)
            {
                Debug.LogError("CanvasRoot has no TapLock gameobject");
                return;
            }

            TapLock(false);

            Router = GetComponent<Router>();
            if ( Router == null ) Router = gameObject.AddComponent<Router>();

            var eventSystem = GetComponent<EventSystem>();
            if ( eventSystem == null ) gameObject.AddComponent<EventSystem>();

            var inputModule = GetComponent<StandaloneInputModule>();
            if ( inputModule == null ) gameObject.AddComponent<StandaloneInputModule>();
        }

        public IEnumerable<string> GetPanelNames()
        {
            return uiPanelData.panels.Select(panel => panel.name);
        }

        public UIPanel AddPanel(string panelName)
        {
            if ( panelCaches.ContainsKey(panelName) )
            {
                panelCaches[panelName].gameObject.SetActive(true);
                return panelCaches[panelName];
            }

            return Add(panelName).GetComponent<UIPanel>();
        }

        GameObject Add(string panelName)
        {
            var data = uiPanelData.panels.FirstOrDefault(_ => _.name == panelName);

            if (data == null)
            {
                throw new KeyNotFoundException("not found : panelName = " + panelName);
            }
            var go = Instantiate(data.prefab, layerContent[layerType[data.layerTypeIdx]]);
            go.name = panelName;

            if( data.cache )
            {
                panelCaches[panelName] = go.GetComponent<UIPanel>();
            }
            return go;
        }

        public void SortPanel(bool isOpen, string panelName = "")
        {
            if (!reservedSort && gameObject.activeSelf )
            {
                StartCoroutine(SortPanelInternal(isOpen, panelName));
                reservedSort = true;
            }
        }

        IEnumerator SortPanelInternal(bool isOpen, string panelName)
        {
            yield return new WaitForEndOfFrame();

            blind?.SetActive(false);

            for ( int i=0 ; i<layerType.Count ; i++ )
            {
                int idx = 0;
                var panels = layerContent[layerType[i]].GetComponentsInChildren<UIPanel>().OrderBy(_ => _.transform.GetSiblingIndex());
                layerCount[layerType[i]] = panels.Count();
                foreach ( var panel in  panels)
                {
                    var info = uiPanelData.panels.FirstOrDefault(_ => _.name == panel.name);
                    if ( info.blindType != BlindType.None )
                    {
                        blind.transform.SetParent(panel.transform.parent);
                        var btn = blind.GetComponent<Button>();

                        btn.onClick.RemoveAllListeners();
                        if (info.blindType == BlindType.Close)
                        {
                            btn.onClick.AddListener(()=>panel.Close());
                        }
                        else if ( info.blindType == BlindType.Custom )
                        {
                            btn.onClick.AddListener(panel.OnTapBlind);
                        }

                        blind.SetActive(true);
                        blind.transform.SetSiblingIndex(idx);
                        idx++;
                    }
                    panel.transform.SetSiblingIndex(idx);
                    idx++;
                }
            }

            reservedSort = false;

            if ( !string.IsNullOrEmpty(panelName) )
            {
                CheckLayer(isOpen, panelName);
            }
        }

        public bool Close(string panelName, bool forceDestroy = false)
        {
            if ( panelCaches.ContainsKey(panelName) )
            {
                var panel = panelCaches[panelName];
                if (forceDestroy)
                {
                    panelCaches.Remove(panelName);
                    return true;
                }
                else
                {
                    panel.gameObject.SetActive(false);
                }
                return false;
            }
            return true;
        }

        public void CloseByLayer(params string[] layerNames)
        {
            StartCoroutine(CloseByLayerInternal(null, layerNames));
        }
        public void CloseByLayer(Action onCompleted, params string[] layerNames)
        {
            StartCoroutine(CloseByLayerInternal(onCompleted, layerNames));
        }

        public void CloseAllLayers(Action onCompleted=null)
        {
            StartCoroutine(CloseByLayerInternal(onCompleted, layerType.ToArray()));
        }

        private IEnumerator CloseByLayerInternal(Action onCompleted, params string[] layerNames)
        {
            yield return null;
            foreach ( var layerName in layerNames )
            {
                var panels = layerContent[layerName].GetComponentsInChildren<UIPanel>(true);
                foreach ( var panel in panels)
                {
                    panel.Close();
                }
            }

            if ( onCompleted != null )
            {
                yield return new WaitWhile(()=>IsLayerClosedAll(layerNames));
                onCompleted.Invoke();
            }
        }

        public bool IsLayerClosedAll(params string[] layerNames)
        {
            return layerCount.Any(x=>layerNames.Contains(x.Key) && x.Value>0);
        }

        void CheckLayer(bool isOpen, string closedPanel)
        {
            var info = uiPanelData.panels.FirstOrDefault(_ => _.name == closedPanel);
            if (info == null)
            {
                return;
            }

            var layerName = layerType[info.layerTypeIdx];
            if ( !isOpen && layerCount[layerName] == 0 )
            {
                OnAllClosed?.Invoke(layerName);
            }
            else if ( isOpen && layerCount[layerName] == 1 )
            {
                OnFirstOpened?.Invoke(layerName);
            }
        }

        public bool IsTapLock => tapLock.activeSelf;
        public void TapLock(bool isLock) => tapLock.SetActive(isLock);


        // ======== Singleton ===========================================================================================
        static UILayer _instance;

        public static UILayer Inst
        {
            get
            {
                if (_instance == null)
                {
                    var previous = FindObjectOfType(typeof(UILayer));
                    if (previous)
                    {
                        Debug.LogWarning("Initialized twice. Don't use LayerController in the scene hierarchy.");
                        _instance = (UILayer)previous;
                    }
                    else
                    {
                        var go = new GameObject("LayerController");
                        _instance = go.AddComponent<UILayer>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
    }
}