using System;
using System.Collections.Generic;

namespace UuIiView
{
    /// <summary>
    /// リアクティブなデータバインディングを提供するPresenter基底クラス
    /// ViewModelを使用した双方向データバインディングをサポートする
    /// </summary>
    public abstract class ReactivePresenter : UIPresenter
    {
        /// <summary>データバインディング用のViewModel</summary>
        protected ViewModel viewModel;
        /// <summary>ViewModelへの読み取り専用アクセス</summary>
        public ViewModel ViewModel => viewModel;

        /// <summary>
        /// ReactivePresenterを初期化する
        /// </summary>
        /// <param name="router">イベントルーター</param>
        /// <param name="panelName">管理するパネル名</param>
        /// <param name="model">モデルコンテナ</param>
        public ReactivePresenter(Router router, string panelName, Model model) : base(router, panelName, model)
        {
            viewModel = new ViewModel(Bind);
        }

        /// <summary>
        /// パネルを開く（クローズ時にバインディングをクリアする）
        /// </summary>
        /// <param name="onOpen">パネルオープン完了時のコールバック</param>
        /// <param name="onClose">パネルクローズ完了時のコールバック</param>
        /// <returns>開いたUIPanelインスタンス</returns>
        protected override UIPanel Open(Action onOpen = null, Action onClose = null)
        {
            base.Open(null, ()=>{ClearBind();});
            return uiPanel;
        }

        /// <summary>
        /// パネルを閉じる
        /// </summary>
        protected override void Close()
        {
            base.Close();
        }

        /// <summary>
        /// コマンドリンクからイベントを処理する
        /// Open/Close/DataSync等のアクションタイプに応じた処理を実行する
        /// </summary>
        /// <param name="commandLink">処理するコマンドリンク</param>
        public override void OnEvent(CommandLink commandLink)
        {
            switch( commandLink.ActionType )
            {
                case UuIiView.ActionType.Open:
                    Open();
                    GetInitData(commandLink, (json)=>{
                        viewModel.Init(json);
                    });
                    break;
                case UuIiView.ActionType.Close:
                    Close();
                    break;
                case UuIiView.ActionType.DataSync:
                    DataSync(commandLink);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// UIからのデータ同期イベントを処理する
        /// Slider/Toggle/Inputの値変更を検出してViewModelと同期する
        /// </summary>
        /// <param name="commandLink">同期イベントのコマンドリンク</param>
        void DataSync(CommandLink commandLink)
        {
            if ( commandLink.EventType == EventType.Slider )
            {
                if ( float.TryParse(commandLink.param["Slider"], out float val) )
                {
                    Sync(commandLink, val);
                }
            }
            else if ( commandLink.EventType == EventType.Toggle )
            {
                if ( bool.TryParse(commandLink.param["Toggle"], out bool val) )
                {
                    Sync(commandLink, val);
                }
            }
            else if ( commandLink.EventType == EventType.Input )
            {
                Sync(commandLink, commandLink.param["Input"]);
            }
        }

        /// <summary>
        /// 値をViewModelに同期する
        /// リストアイテムまたは単一プロパティの同期を処理する
        /// </summary>
        /// <param name="commandLink">同期元のコマンドリンク</param>
        /// <param name="val">同期する値</param>
        void Sync(CommandLink commandLink, object val)
        {
            if ( !string.IsNullOrEmpty(commandLink.ParentName) )
            {
                viewModel.SyncListItem(commandLink.ParentName, commandLink.Id, commandLink.EventName, val);
            }
            else if ( string.IsNullOrEmpty(commandLink.Id) )
            {
                viewModel.Sync(commandLink.EventName, val);
            }
        }

        /// <summary>
        /// ViewModelからのデータ変更をUIにバインドする
        /// </summary>
        /// <param name="data">更新されたデータ</param>
        protected void Bind(Dictionary<string,object> data)
        {
            if ( data!=null )
            {
                uiPanel.UpdateData(data);
            }
        }

        /// <summary>
        /// バインディングをクリアする（パネルクローズ時に呼び出される）
        /// </summary>
        protected void ClearBind()
        {
            viewModel.Clear();
        }
    }
}
