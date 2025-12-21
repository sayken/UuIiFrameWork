using System;
using UnityEngine;

namespace UuIiView
{
    /// <summary>
    /// UIパネルのPresenter基底クラス
    /// パネルのライフサイクル管理とイベントルーティングを担当する
    /// </summary>
    /// <remarks>
    /// MVPパターンにおけるPresenterとして、ViewとModelの仲介役を担う
    /// パネルのOpen/Close、イベント処理、データ取得を統括する
    /// </remarks>
    public abstract class UIPresenter : IPresenter
    {
        /// <summary>イベントルーター</summary>
        private readonly Router router;

        /// <summary>関連付けられたパネル名</summary>
        protected string PanelName;

        /// <summary>管理対象のUIPanelインスタンス</summary>
        protected UIPanel uiPanel;

        /// <summary>モデルコンテナ</summary>
        protected Model model;

        /// <summary>パネルオープン時のコールバック</summary>
        protected Action onOpen;

        /// <summary>パネルクローズ時のコールバック</summary>
        protected Action onClose;

        /// <summary>
        /// UIPresenterを初期化する
        /// </summary>
        /// <param name="router">イベントルーター</param>
        /// <param name="panelName">管理するパネル名</param>
        /// <param name="model">モデルコンテナ</param>
        public UIPresenter(Router router, string panelName, Model model)
        {
            this.router = router;
            PanelName = panelName;
            this.model = model;
        }

        /// <summary>
        /// パネルを開く
        /// </summary>
        /// <param name="onPanelOpen">パネルオープン完了時のコールバック</param>
        /// <param name="onPanelClose">パネルクローズ完了時のコールバック</param>
        /// <returns>開いたUIPanelインスタンス（失敗時はnull）</returns>
        protected virtual UIPanel Open(Action onPanelOpen = null, Action onPanelClose = null)
        {
            if (UILayer.Inst == null)
            {
                Debug.LogError($"[UIPresenter] UILayer.Inst is not initialized: {PanelName}");
                return null;
            }

            if (uiPanel == null)
            {
                uiPanel = UILayer.Inst.AddPanel(PanelName);
                if (uiPanel == null)
                {
                    Debug.LogError($"[UIPresenter] Failed to add panel: {PanelName}");
                    return null;
                }
            }

            uiPanel.OnOpen = onPanelOpen;
            uiPanel.OnClose = onPanelClose;

            onOpen?.Invoke();
            return uiPanel.Open(PassToRouter);
        }

        /// <summary>
        /// パネルを閉じる
        /// </summary>
        protected virtual void Close()
        {
            onClose?.Invoke();
            uiPanel?.Close();
        }

        /// <summary>
        /// コマンドリンク文字列をルーターに渡す
        /// </summary>
        /// <param name="path">コマンドリンク文字列</param>
        private void PassToRouter(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            try
            {
                PassToRouter(new CommandLink(path));
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[UIPresenter] Invalid command link format: {ex.Message}");
            }
        }

        /// <summary>
        /// コマンドをルーターに渡す
        /// </summary>
        /// <param name="cmd">ルーティングするコマンド</param>
        protected void PassToRouter(CommandLink cmd)
        {
            if (router == null || cmd == null)
            {
                return;
            }
            router.Routing(cmd);
        }

        /// <summary>
        /// 指定した名前のPresenterを取得する
        /// </summary>
        /// <param name="name">Presenter名</param>
        /// <returns>対応するIPresenter（見つからない場合はnull）</returns>
        protected IPresenter GetPresenter(string name)
        {
            return router?.GetPresenter(name);
        }

        /// <summary>
        /// シーン遷移用のコマンドをルーターに渡す
        /// </summary>
        /// <param name="cmd">シーン遷移コマンド</param>
        protected void PassToScene(CommandLink cmd)
        {
            if (router == null || cmd == null)
            {
                return;
            }
            router.RouteToScene(cmd);
        }

        /// <summary>
        /// コマンドリンク文字列からイベントを処理する
        /// </summary>
        /// <param name="commandLink">コマンドリンク文字列</param>
        public virtual void OnEvent(string commandLink)
        {
            if (string.IsNullOrEmpty(commandLink))
            {
                return;
            }

            try
            {
                OnEvent(new CommandLink(commandLink));
            }
            catch (ArgumentException ex)
            {
                Debug.LogError($"[UIPresenter] Invalid command link format: {ex.Message}");
            }
        }

        /// <summary>
        /// コマンドリンクからイベントを処理する
        /// Open/Close等のアクションタイプに応じた処理を実行する
        /// </summary>
        /// <param name="commandLink">処理するコマンドリンク</param>
        public virtual void OnEvent(CommandLink commandLink)
        {
            if (commandLink == null)
            {
                return;
            }

            switch (commandLink.ActionType)
            {
                case ActionType.Open:
                    var panel = Open();
                    if (panel != null)
                    {
                        GetInitData(commandLink, (json) =>
                        {
                            uiPanel?.UpdateData(json);
                        });
                    }
                    break;
                case ActionType.Close:
                    Close();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// パネル初期化用のデータを取得する
        /// サブクラスでオーバーライドしてAPI呼び出し等を実装する
        /// </summary>
        /// <param name="commandLink">初期化のトリガーとなったコマンド</param>
        /// <param name="onCompleted">データ取得完了時のコールバック（JSON文字列を渡す）</param>
        protected virtual void GetInitData(CommandLink commandLink, Action<string> onCompleted)
        {
            onCompleted?.Invoke("{}");
        }
    }
}
