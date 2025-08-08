using System.Collections.Generic;
using UnityEngine;
using System;

namespace UuIiView
{
    [RequireComponent(typeof(UILayer))]
    public class Router : MonoBehaviour
    {
        // 全Presenterのインスタンスを保持
        public Dictionary<string, IPresenter> presenters { get; private set; }= new ();
        // 全GroupPresenterのインスタンスを保持
        public Dictionary<string, IGroupPresenter> groupPresenters { get; private set;} = new ();

        /// <summary>
        /// Presenterをセットする
        /// </summary>
        /// <param name="panelName"></param>
        /// <param name="type"></param>
        /// <param name="model"></param>
        public void SetPresenter(string panelName, Type type, Model model)
        {
            IPresenter obj = (IPresenter)Activator.CreateInstance(type, UILayer.Inst.Router, panelName, model);
            presenters.Add(panelName, obj);
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

        public void SetGroupPresenter(Type type, UIGroup group, Model model)
        {
            // Debug.Log("Type = "+ type.ToString());
            IGroupPresenter groupPresenter = (IGroupPresenter)Activator.CreateInstance(type, UILayer.Inst.Router, group.name, model);
            foreach ( var panelName in group.panelNames)
            {
                if ( !presenters.ContainsKey(panelName) )
                {
                    Debug.LogError($"[{panelName}Presenter] Not found in prsenters");
                    return;
                }
                groupPresenter.AddPresenter(presenters[panelName]);
            }
            groupPresenters[group.name] = groupPresenter;
        }

        /// <summary>
        /// 全てのEventを受け取って、処理対象のPresenterに処理を渡す
        /// </summary>
        /// <param name="cmd"></param>
        public void Routing(CommandLink cmd)
        {
            Debug.Log(cmd.Log());

            if ( groupPresenters.ContainsKey(cmd.PanelName))
            {
                // GroupPresenterに処理を渡す
                groupPresenters[cmd.PanelName].OnEvent(cmd);
            }
            else
            {
                // Presenterに処理を渡す
                presenters[cmd.PanelName].OnEvent(cmd);
            }
        }

        public IScene CurrentScene;

        public void RouteToScene(CommandLink cmd)
        {
            Debug.Log(cmd.Log(true));

            CurrentScene?.OnEvent(cmd);
        }


        public void Log()
        {
            foreach ( var a in groupPresenters)
            {
                Debug.Log($"groupPresenterName = {a.Key} : count = {a.Value.presenters.Count}");
            }

        }
    }
}