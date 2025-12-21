using System.Collections.Generic;

namespace UuIiView
{
    /// <summary>
    /// 複数のPresenterをグループ化して管理するインターフェース
    /// 関連するパネル群への一括操作や、グループ単位でのイベント処理を実現
    /// </summary>
    /// <remarks>
    /// タブ切り替えUI、ウィザード形式のフロー、関連パネルの連携制御などに使用
    /// UIGroupPresenterクラスを継承することで基本実装を取得可能
    /// </remarks>
    public interface IGroupPresenter
    {
        /// <summary>
        /// グループに所属するPresenterのリスト
        /// </summary>
        List<IPresenter> presenters { get; set; }

        /// <summary>
        /// Presenterをグループに追加する
        /// </summary>
        /// <param name="presenter">追加するPresenter</param>
        void AddPresenter(IPresenter presenter);

        /// <summary>
        /// グループレベルのイベントを処理する
        /// 必要に応じて所属Presenterにイベントを委譲する
        /// </summary>
        /// <param name="commandLink">処理するコマンド情報</param>
        void OnEvent(CommandLink commandLink);
    }
}
