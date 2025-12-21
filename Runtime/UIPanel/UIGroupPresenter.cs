using System.Collections.Generic;

namespace UuIiView
{
    /// <summary>
    /// 複数のPresenterをグループ化して管理する基底クラス
    /// 関連するパネル群への一括操作や、グループ単位でのイベント処理を実現
    /// </summary>
    /// <remarks>
    /// タブ切り替えUI、ウィザード形式のフロー、関連パネルの連携制御などに使用
    /// IGroupPresenterインターフェースを実装し、グループ共通の処理を提供
    /// </remarks>
    public abstract class UIGroupPresenter : IGroupPresenter
    {
        /// <summary>グループに所属するPresenterのリスト</summary>
        public List<IPresenter> presenters { get; set; } = new();

        /// <summary>イベントルーター</summary>
        private readonly Router router;

        /// <summary>グループで共有するモデルコンテナ</summary>
        protected Model model { get; private set; }

        /// <summary>
        /// UIGroupPresenterを初期化する
        /// </summary>
        /// <param name="router">イベントルーター</param>
        /// <param name="name">グループ名</param>
        /// <param name="model">グループで共有するモデルコンテナ</param>
        public UIGroupPresenter(Router router, string name, Model model)
        {
            this.router = router;
            this.model = model;
        }

        /// <summary>
        /// Presenterをグループに追加する
        /// </summary>
        /// <param name="presenter">追加するPresenter（nullの場合は無視）</param>
        public void AddPresenter(IPresenter presenter)
        {
            if (presenter == null)
            {
                return;
            }
            presenters.Add(presenter);
        }

        /// <summary>
        /// グループレベルのイベントを処理する
        /// Open/Closeアクションの場合は全所属Presenterにイベントを委譲
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
                case ActionType.Close:
                    foreach (var presenter in presenters)
                    {
                        presenter?.OnEvent(commandLink);
                    }
                    break;
                default:
                    break;
            }
        }

        // ========================================================================
        // Pass CommandLink to Router
        // ========================================================================

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
        /// 指定した名前のPresenterを取得する
        /// </summary>
        /// <param name="name">Presenter名</param>
        /// <returns>対応するIPresenter（見つからない場合はnull）</returns>
        protected IPresenter GetPresenter(string name)
        {
            return router?.GetPresenter(name);
        }
    }
}
