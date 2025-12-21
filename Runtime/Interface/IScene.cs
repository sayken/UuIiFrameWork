namespace UuIiView
{
    /// <summary>
    /// シーンレベルのイベント処理を行うインターフェース
    /// 複数のPresenterを横断するイベントや、シーン全体に関わる処理を担当
    /// </summary>
    /// <remarks>
    /// Router.CurrentSceneに設定することで、RouteToScene経由でイベントを受信する
    /// シーン遷移、グローバルなUI制御、アプリケーション状態管理などに使用
    /// </remarks>
    public interface IScene
    {
        /// <summary>
        /// シーンレベルのイベントを処理する
        /// </summary>
        /// <param name="command">処理するコマンド情報</param>
        void OnEvent(CommandLink command);
    }
}
