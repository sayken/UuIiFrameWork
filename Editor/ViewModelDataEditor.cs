using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using System.IO;

namespace UuIiView
{
    public class ViewModelDataEditor : EditorWindow
    {
        [MenuItem("Tools/UuIiView/ViewModelData")]
        static void ShowEditor()
        {
            var window = EditorWindow.GetWindow<ViewModelDataEditor>(typeof(SceneView));
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Packages/UuIiView/Editor/Icons/icon.png");
            window.titleContent = new GUIContent(" ViewModelData", icon);
        }

        GameObject canvasRoot;
        Router router;
        string[] uiPanelNames;
        int selectedIndex = 0;

        bool prepared = false;
        string log = string.Empty;
        private Vector2 scrollPosition = Vector2.zero;

        private void OnGUI()
        {
            // 再生チェック
            if ( Application.isPlaying == false )
            {
                EditorGUILayout.LabelField("再生中でのみ機能します");
                prepared = false;
                return;
            }

            // 必要なものが揃ってるかチェック
            if ( prepared == false )
            {
                return;
            }

            // 実処理
            EditorGUILayout.BeginHorizontal();
            selectedIndex = EditorGUILayout.Popup(new UnityEngine.GUIContent("UIPanelName"), selectedIndex, uiPanelNames);

            if ( GUILayout.Button("Show Repository") )
            {
                var presenter = (ReactivePresenter)router.GetPresenter(uiPanelNames[selectedIndex]);
                log = presenter.ViewModel.Log();

                // string cmd = uiPanelNames[selectedIndex] +"/Log/None/ShowLog/ParentName/Id";
                // router.Dispatch(new CommandLink(cmd));
            }
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label(log);
            EditorGUILayout.EndScrollView();
        }


        void OnFocus()
        {
            if ( Application.isPlaying == false ) return;

            prepared = Prepare();
        }

        bool Prepare()
        {
            if ( canvasRoot == null ) canvasRoot = GameObject.Find("CanvasRoot");
            if ( router == null ) router = GameObject.Find("LayerController").GetComponent<Router>();

            if ( canvasRoot == null )
            {
                Debug.LogError("CanvasRootが見つかりません");
                return false;
            }
            if ( router == null )
            {
                Debug.LogError("Routerが見つかりません");
                return false;
            }

            var uiPanels = canvasRoot.GetComponentsInChildren<UIPanel>(true);
            if ( uiPanels != null && uiPanels.Length > 0 )
            {
                uiPanelNames = uiPanels.Select(_=>_.name).ToArray();
            }
            if ( uiPanelNames.Length < selectedIndex )
            {
                selectedIndex = uiPanelNames.Length - 1;
            }

            return true;
        }
    }
}
