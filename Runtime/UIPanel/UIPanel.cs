using System;
using UnityEngine;

namespace UuIiView
{
    /// <summary>
    /// UIパネルの表示・非表示を制御するコンポーネント
    /// トランジションアニメーションとライフサイクル管理を担当する
    /// </summary>
    [RequireComponent(typeof(UIViewRoot))]
    public class UIPanel : MonoBehaviour
    {
        UIViewRoot vm;

        /// <summary>UIViewRootコンポーネントへの参照</summary>
        public UIViewRoot ViewRoot => vm ??= GetComponent<UIViewRoot>();
        ITransition transition;

        /// <summary>パネルが開いた時に呼び出されるコールバック</summary>
        public Action OnOpen;
        /// <summary>パネルが閉じた時に呼び出されるコールバック</summary>
        public Action OnClose;

        /// <summary>
        /// トランジションアニメーションを設定する
        /// </summary>
        /// <param name="transition">使用するトランジション</param>
        public void SetTransition(ITransition transition) => this.transition = transition;

        /// <summary>パネルが開いているかどうか</summary>
        public bool isOpened { get; private set; } = false;

        /// <summary>
        /// データを指定してパネルを開く
        /// </summary>
        /// <param name="d">パネルに渡すデータ</param>
        /// <returns>このパネルインスタンス</returns>
        public UIPanel Open(object d)
        {
            Open(d, null);
            return this;
        }

        /// <summary>
        /// イベントハンドラを指定してパネルを開く
        /// </summary>
        /// <param name="onEvent">イベント発生時のコールバック</param>
        /// <returns>このパネルインスタンス</returns>
        public UIPanel Open(Action<string> onEvent)
        {
            Open(null, onEvent);
            return this;
        }

        /// <summary>
        /// データとイベントハンドラを指定してパネルを開く
        /// </summary>
        /// <param name="d">パネルに渡すデータ</param>
        /// <param name="onEvent">イベント発生時のコールバック</param>
        /// <returns>このパネルインスタンス</returns>
        public UIPanel Open(object d, Action<string> onEvent)
        {
            isOpened = false;

            UILayer.Inst.TapLock(true);

            ViewRoot.SetData(d);
            if (onEvent != null) ViewRoot.SetReceiver(onEvent);

            UILayer.Inst.SortPanel(true, gameObject.name);
            transition = GetComponent<ITransition>();
            if (transition != null)
            {
                transition.TransitionIn(() => OpenCompleted());
            }
            else
            {
                OpenCompleted();
            }
            return this;
        }

        /// <summary>
        /// パネルオープン完了時の処理
        /// </summary>
        void OpenCompleted()
        {
            OnOpen?.Invoke();
            UILayer.Inst.TapLock(false);
            isOpened = true;
        }

        /// <summary>
        /// パネルのデータを更新する
        /// </summary>
        /// <param name="o">新しいデータ</param>
        public void UpdateData(object o) => ViewRoot.SetData(o);

        /// <summary>
        /// パネルを閉じる
        /// </summary>
        /// <param name="forceDestroy">trueの場合、キャッシュを無視して強制的に破棄する</param>
        public void Close(bool forceDestroy = false)
        {
            UILayer.Inst.TapLock(true);

            //var transition = GetComponent<Transition>();
            if (transition != null)
            {
                transition.TransitionOut( () => CloseCompleted(forceDestroy) );
            }
            else
            {
                CloseCompleted(forceDestroy);
            }
        }

        /// <summary>
        /// パネルクローズ完了時の処理
        /// </summary>
        /// <param name="forceDestroy">強制破棄フラグ</param>
        void CloseCompleted(bool forceDestroy)
        {
            OnClose?.Invoke();
            UILayer.Inst.TapLock(false);

            bool needDestroy = UILayer.Inst.Close(gameObject.name, forceDestroy);
            if (needDestroy)
            {
                Destroy(gameObject);
            }
            else
            {
                UILayer.Inst.SortPanel(false, gameObject.name);
            }
        }

        /// <summary>
        /// GameObjectが破棄される時の処理
        /// </summary>
        void OnDestroy()
        {
            UILayer.Inst.SortPanel(false, gameObject.name);
        }

        /// <summary>
        /// 戻る操作のハンドラ（未実装）
        /// </summary>
        public void Back()
        {

        }

        Action onTapBlind;

        /// <summary>
        /// ブラインド（背景）タップ時の処理
        /// パネルを閉じる
        /// </summary>
        public void OnTapBlind()
        {
            onTapBlind?.Invoke();
            Close();
        }
    }
}
