namespace UuIiView
{
    /// <summary>
    /// アプリケーションデータを管理するModelのインターフェース
    /// MVPパターンにおいてビジネスロジックとデータ状態を保持する
    /// Presenterを通じてViewと通信し、直接的なUI依存を持たない
    /// </summary>
    /// <remarks>
    /// 実装クラスはModelクラスを継承することを推奨
    /// データ変更時はPresenterに通知し、ViewModelを経由してUIを更新する
    /// </remarks>
    public interface IModel
    {
    }
}
