using System.Collections.Generic;
using UnityEngine;
using System;

namespace UuIiView
{
    /// <summary>
    /// イベントルーティングを管理するコンポーネント
    /// UIViewから発生したイベントを適切なPresenterに振り分ける
    /// </summary>
    [RequireComponent(typeof(UILayer))]
    public class Router : MonoBehaviour
    {
        /// <summary>登録されている全Presenterのインスタンス</summary>
        public Dictionary<string, IPresenter> presenters { get; private set; } = new();

        /// <summary>登録されている全GroupPresenterのインスタンス</summary>
        public Dictionary<string, IGroupPresenter> groupPresenters { get; private set; } = new();

        /// <summary>
        /// Presenterをセットする
        /// </summary>
        /// <param name="panelName">パネル名</param>
        /// <param name="type">Presenterの型</param>
        /// <param name="model">共有するModelインスタンス</param>
        public void SetPresenter(string panelName, Type type, Model model)
        {
            if (string.IsNullOrEmpty(panelName))
            {
                Debug.LogError("[Router] panelName cannot be null or empty");
                return;
            }

            if (type == null)
            {
                Debug.LogError("[Router] type cannot be null");
                return;
            }

            if (!typeof(IPresenter).IsAssignableFrom(type))
            {
                Debug.LogError($"[Router] Type {type.Name} does not implement IPresenter");
                return;
            }

            if (UILayer.Inst == null)
            {
                Debug.LogError("[Router] UILayer.Inst is not initialized");
                return;
            }

            try
            {
                IPresenter obj = (IPresenter)Activator.CreateInstance(type, UILayer.Inst.Router, panelName, model);
                if (obj == null)
                {
                    Debug.LogError($"[Router] Failed to create presenter instance: {type.Name}");
                    return;
                }
                presenters[panelName] = obj;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Router] Failed to create presenter {type.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Presenterを取得する
        /// </summary>
        /// <param name="panelName">取得したpresenterの名前</param>
        /// <returns>指定したpresenter（見つからない場合はnull）</returns>
        public IPresenter GetPresenter(string panelName)
        {
            return presenters.ContainsKey(panelName) ? presenters[panelName] : null;
        }

        /// <summary>
        /// GroupPresenterをセットする
        /// </summary>
        /// <param name="type">GroupPresenterの型</param>
        /// <param name="group">UIGroupの設定</param>
        /// <param name="model">共有するModelインスタンス</param>
        public void SetGroupPresenter(Type type, UIGroup group, Model model)
        {
            if (type == null)
            {
                Debug.LogError("[Router] type cannot be null");
                return;
            }

            if (group == null)
            {
                Debug.LogError("[Router] group cannot be null");
                return;
            }

            if (!typeof(IGroupPresenter).IsAssignableFrom(type))
            {
                Debug.LogError($"[Router] Type {type.Name} does not implement IGroupPresenter");
                return;
            }

            if (UILayer.Inst == null)
            {
                Debug.LogError("[Router] UILayer.Inst is not initialized");
                return;
            }

            try
            {
                IGroupPresenter groupPresenter = (IGroupPresenter)Activator.CreateInstance(type, UILayer.Inst.Router, group.name, model);
                if (groupPresenter == null)
                {
                    Debug.LogError($"[Router] Failed to create group presenter instance: {type.Name}");
                    return;
                }

                foreach (var panelName in group.panelNames)
                {
                    if (!presenters.TryGetValue(panelName, out var presenter))
                    {
                        Debug.LogError($"[Router] [{panelName}Presenter] Not found in presenters");
                        return;
                    }
                    groupPresenter.AddPresenter(presenter);
                }
                groupPresenters[group.name] = groupPresenter;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Router] Failed to create group presenter {type.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// 全てのEventを受け取って、処理対象のPresenterに処理を渡す
        /// </summary>
        /// <param name="cmd"></param>
        public void Routing(CommandLink cmd)
        {
            if (cmd == null)
            {
                Debug.LogError("[Router] CommandLink is null");
                return;
            }

            Debug.Log(cmd.Log());

            if (groupPresenters.TryGetValue(cmd.PanelName, out var groupPresenter))
            {
                // GroupPresenterに処理を渡す
                groupPresenter.OnEvent(cmd);
            }
            else if (presenters.TryGetValue(cmd.PanelName, out var presenter))
            {
                // Presenterに処理を渡す
                presenter.OnEvent(cmd);
            }
            else
            {
                Debug.LogError($"[Router] Presenter not found: {cmd.PanelName}");
            }
        }

        /// <summary>現在アクティブなシーン</summary>
        public IScene CurrentScene;

        /// <summary>
        /// シーンにイベントをルーティングする
        /// </summary>
        /// <param name="cmd">ルーティングするコマンド</param>
        public void RouteToScene(CommandLink cmd)
        {
            Debug.Log(cmd.Log(true));

            CurrentScene?.OnEvent(cmd);
        }

        /// <summary>
        /// デバッグ用：登録されているGroupPresenterの情報をログ出力する
        /// </summary>
        public void Log()
        {
            foreach ( var a in groupPresenters)
            {
                Debug.Log($"groupPresenterName = {a.Key} : count = {a.Value.presenters.Count}");
            }

        }
    }
}