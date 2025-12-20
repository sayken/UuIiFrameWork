using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace UuIiView
{
    /// <summary>
    /// UI表示階層を管理するシングルトンコンポーネント
    /// パネルの生成、キャッシュ、レイヤーソートを担当する
    /// </summary>
    public class UILayer : MonoBehaviour
    {
        private UIPanelData uiPanelData;

        /// <summary>レイヤー名のリスト</summary>
        public List<string> layerType = new List<string>();

        private Dictionary<string, RectTransform> layerContent = new Dictionary<string, RectTransform>();
        private Dictionary<string, int> layerCount = new Dictionary<string, int>();
        private GameObject canvasRoot;
        private GameObject blind;
        private GameObject tapLock;
        private bool reservedSort = false;
        private Dictionary<string, UIPanel> panelCaches = new Dictionary<string, UIPanel>();

        /// <summary>イベントルーティングを管理するRouterインスタンス</summary>
        public Router Router { get; private set; }

        /// <summary>パネルを閉じた時、所属レイヤーが全て閉じられたら呼ばれるコールバック</summary>
        public Action<string> OnAllClosed;

        /// <summary>レイヤーで最初のパネルが開かれた時に呼ばれるコールバック</summary>
        public Action<string> OnFirstOpened;

        /// <summary>
        /// UILayerを初期化する
        /// </summary>
        /// <param name="uiPanelData">パネル設定データ</param>
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

            var tapLockTransform = canvasRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(_ => _.gameObject.name == "TapLock");
            var blindTransform = canvasRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(_ => _.gameObject.name == "Blind");

            if (blindTransform == null)
            {
                Debug.LogError("CanvasRoot has no Blind gameobject");
            }
            else
            {
                blind = blindTransform.gameObject;
            }

            if (tapLockTransform == null)
            {
                Debug.LogError("CanvasRoot has no TapLock gameobject");
                return;
            }
            tapLock = tapLockTransform.gameObject;

            TapLock(false);

            Router = GetComponent<Router>();
            if ( Router == null ) Router = gameObject.AddComponent<Router>();

            var eventSystem = GetComponent<EventSystem>();
            if ( eventSystem == null ) gameObject.AddComponent<EventSystem>();

            var inputModule = GetComponent<StandaloneInputModule>();
            if ( inputModule == null ) gameObject.AddComponent<StandaloneInputModule>();
        }

        /// <summary>
        /// 登録されている全パネル名を取得する
        /// </summary>
        /// <returns>パネル名のコレクション</returns>
        public IEnumerable<string> GetPanelNames()
        {
            return uiPanelData.panels.Select(panel => panel.name);
        }

        /// <summary>
        /// パネルを追加する（キャッシュがあればそれを使用）
        /// </summary>
        /// <param name="panelName">パネル名</param>
        /// <returns>追加されたUIPanel</returns>
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

        /// <summary>
        /// パネルの表示順をソートする
        /// </summary>
        /// <param name="isOpen">パネルが開かれたかどうか</param>
        /// <param name="panelName">対象パネル名</param>
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

            // パネル情報を辞書化してO(1)アクセスに改善
            var panelInfoDict = uiPanelData.panels.ToDictionary(p => p.name);

            for (int i = 0; i < layerType.Count; i++)
            {
                int idx = 0;
                var panels = layerContent[layerType[i]]
                    .GetComponentsInChildren<UIPanel>()
                    .OrderBy(_ => _.transform.GetSiblingIndex())
                    .ToList();
                layerCount[layerType[i]] = panels.Count;

                foreach (var panel in panels)
                {
                    if (!panelInfoDict.TryGetValue(panel.name, out var info))
                    {
                        continue;
                    }

                    if (info.blindType != BlindType.None)
                    {
                        blind.transform.SetParent(panel.transform.parent);
                        var btn = blind.GetComponent<Button>();

                        btn.onClick.RemoveAllListeners();
                        if (info.blindType == BlindType.Close)
                        {
                            btn.onClick.AddListener(() => panel.Close());
                        }
                        else if (info.blindType == BlindType.Custom)
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

            if (!string.IsNullOrEmpty(panelName))
            {
                CheckLayer(isOpen, panelName);
            }
        }

        /// <summary>
        /// パネルを閉じる
        /// </summary>
        /// <param name="panelName">パネル名</param>
        /// <param name="forceDestroy">強制的に破棄するか</param>
        /// <returns>破棄が必要な場合はtrue</returns>
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

        /// <summary>
        /// 指定されたレイヤーの全パネルを閉じる
        /// </summary>
        /// <param name="layerNames">閉じるレイヤー名</param>
        public void CloseByLayer(params string[] layerNames)
        {
            StartCoroutine(CloseByLayerInternal(null, layerNames));
        }

        /// <summary>
        /// 指定されたレイヤーの全パネルを閉じる（完了コールバック付き）
        /// </summary>
        /// <param name="onCompleted">完了時のコールバック</param>
        /// <param name="layerNames">閉じるレイヤー名</param>
        public void CloseByLayer(Action onCompleted, params string[] layerNames)
        {
            StartCoroutine(CloseByLayerInternal(onCompleted, layerNames));
        }

        /// <summary>
        /// 全レイヤーの全パネルを閉じる
        /// </summary>
        /// <param name="onCompleted">完了時のコールバック</param>
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
                yield return new WaitWhile(() => HasOpenPanelInLayers(layerNames));
                onCompleted.Invoke();
            }
        }

        /// <summary>
        /// 指定されたレイヤーに開いているパネルが存在するかどうか
        /// </summary>
        public bool HasOpenPanelInLayers(params string[] layerNames)
        {
            return layerCount.Any(x => layerNames.Contains(x.Key) && x.Value > 0);
        }

        /// <summary>
        /// 後方互換性のため残す（非推奨）
        /// </summary>
        [System.Obsolete("Use HasOpenPanelInLayers instead. Note: return value logic is inverted from method name.")]
        public bool IsLayerClosedAll(params string[] layerNames)
        {
            return HasOpenPanelInLayers(layerNames);
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

        /// <summary>タップロックが有効かどうか</summary>
        public bool IsTapLock => tapLock.activeSelf;

        /// <summary>
        /// タップロックの状態を設定する
        /// </summary>
        /// <param name="isLock">ロックする場合はtrue</param>
        public void TapLock(bool isLock) => tapLock.SetActive(isLock);


        // ======== Singleton ===========================================================================================
        private static UILayer _instance;

        /// <summary>
        /// UILayerのシングルトンインスタンス
        /// </summary>
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