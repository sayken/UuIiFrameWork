using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using System.IO;

namespace UuIiView
{
    public class UIViewTestEditor : EditorWindow
    {
        public const string devPanelSceneName = "PanelEditor (Dev)";


        [MenuItem("Tools/UuIiView/UIViewTestEditor")]
        static void ShowEditor()
        {
            var window = EditorWindow.GetWindow<UIViewTestEditor>(typeof(SceneView));
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Packages/UuIiView/Editor/Icons/icon.png");
            window.titleContent = new GUIContent(" UIViewTestEditor", icon);
        }

        ViewTest savedViewTestList;
        UIPanelData uiPanelData;
        GameObject canvasRoot;

        private void OnGUI()
        {
            savedViewTestList = (ViewTest)EditorGUILayout.ObjectField("ViewTestList", savedViewTestList, typeof(ViewTest), true);
            if (savedViewTestList == null)
                return;

            uiPanelData = (UIPanelData)EditorGUILayout.ObjectField("UIPanelData", uiPanelData, typeof(UIPanelData), true);
            if (uiPanelData == null)
                return;

            // シーン内のCanvasRoot取得
            if (canvasRoot == null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("シーンにCanvasRootが見つかりません");
                if ( GUILayout.Button("更新", GUILayout.Width(100)) )
                {
                    canvasRoot = GameObject.Find("CanvasRoot");
                }
                return;
            }

            EditorGUI.BeginChangeCheck();

            if ( savedViewTestList.testDatas == null )
            {
                savedViewTestList.testDatas = new List<ViewTestData>();
            }

            // シーン内のUIPanel取得
            var uiPanels = canvasRoot.GetComponentsInChildren<UIPanel>();
            foreach (var panel in uiPanels)
            {
                var viewRoot = panel.GetComponent<UIViewRoot>();
                var testJsons = savedViewTestList.testDatas.FirstOrDefault(_ => _.uiPanel!=null && _.uiPanel.name == viewRoot.name);
                if (testJsons == null)
                {
                    var panelInfo = uiPanelData.panels.FirstOrDefault(_ => _.prefab.name == viewRoot.name);
                    if (panelInfo != null)
                    {
                        testJsons = new ViewTestData() { uiPanel = panelInfo.prefab, uiViewTestJsons = new List<TextAsset>() };
                        savedViewTestList.testDatas.Add(testJsons);
                    }
                }
                ViewTestList(viewRoot, testJsons.uiViewTestJsons);
            }

            // 変更チェック終了（変更があった場合は保存）
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(savedViewTestList);
                AssetDatabase.SaveAssets();
            }
        }

        void ViewTestList(UIViewRoot viewRoot, List<TextAsset> testJsons)
        {
            EditorGUILayout.Space(10);

            // Panel名
            EditorGUILayout.LabelField("[ "+ viewRoot.name +" ]");

            // PanelのTestJson

            EditorGUI.indentLevel++;
            for ( int i=0 ; i<testJsons.Count ; i++)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    testJsons.RemoveAt(i);
                    EditorGUILayout.EndHorizontal();
                    break;
                }

                testJsons[i] = (TextAsset)EditorGUILayout.ObjectField("", testJsons[i], typeof(TextAsset), true);

                if (EditorApplication.isPlaying && GUILayout.Button("Test") )
                {
                    viewRoot.SetData(testJsons[i].text);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add", GUILayout.Width(100)))
            {
                testJsons.Add(null);
            }
            if (GUILayout.Button("Create", GUILayout.Width(100)))
            {
                var path = EditorUtility.SaveFilePanel("Save Json", "Assets", "viewtest", "json");

                if (!string.IsNullOrEmpty(path))
                {
                    string json = CreateJson(viewRoot.gameObject);
                    StreamWriter sw = new StreamWriter(path, false);
                    sw.WriteLine(json);
                    sw.Flush();
                    sw.Close();
                    Debug.Log(json);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // =========================================================================
        // Json作成
        // =========================================================================

        Dictionary<UIType, string> keyTypeTable = new Dictionary<UIType, string>()
        {
            {UIType.Text,"\"string\""},
            {UIType.Image,"\"string : path to sprite(Resources/xxx)\""},
            {UIType.RawImage,"\"string : image url(https://xxxx/)\""},
            {UIType.GameObject,"bool"},
            {UIType.CustomButton,"bool"},
            {UIType.CustomToggle,"bool"},
            {UIType.Button,"bool"},
            {UIType.Toggle,"bool"},
            {UIType.Slider,"float : (0f〜1f)"},
            {UIType.List,"[]"}

        };

        string indent = "";

        public string CreateJson(GameObject go)
        {
            indent = "";
            StringBuilder sb = new StringBuilder();
            sb.Append(indent).AppendLine("{");
            AddIndent();

            if (go != null)
            {
                sb.Append(indent).AppendLine("\"Id\" : \"Some unique Id\",");
            }
            List<string> elements = new List<string>();
            var uiSetters = go.GetComponentsInChildren<UISetter>(true);
            foreach (var uiSetter in uiSetters)
            {
                var str = ToJson(uiSetter);
                if (!string.IsNullOrEmpty(str)) elements.Add(str);
            };
            sb.AppendLine(string.Join(",\n", elements));

            AddIndent(false);
            sb.Append(indent).Append("}");
            return sb.ToString();
        }

        string ToJson(UISetter uiSetter)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(indent).Append("\"").Append(uiSetter.name).Append("\" : ");
            if (uiSetter.GetType() == typeof(UISetterSimple))
            {
                var type = ((UISetterSimple)uiSetter).uiType;
                if (keyTypeTable.ContainsKey(type))
                {
                    sb.Append(keyTypeTable[type]);
                }
                else
                {
                    sb.Append("\"\"");
                }
            }
            else
            {
                sb.Append("\"\"");
            }
            return sb.ToString();
        }

        void AddIndent(bool isAdd = true)
        {
            if (isAdd)
            {
                indent += "\t";
            }
            else if (indent.Length > 0)
            {
                indent = indent.Substring(1);
            }
        }
    }
}
