using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;
using System.Reflection;
using System.Text;

namespace UuIiView
{
    public class UIPanelDataSettings : EditorWindow
    {
        [MenuItem("Tools/UuIiView/UIPanelData Settings")]
        static void ShowEditor()
        {
            var window = EditorWindow.GetWindow<UIPanelDataSettings>(typeof(SceneView));
            Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/Packages/UuIiView/Editor/Icons/icon.png");
            window.titleContent = new GUIContent(" UIPanelData Settings", icon);
        }

        string[] layerType;
        bool[] supportSafeArea;
        List<PanelInfo> uiPanelData;
        UIPanelData savedUIData;

        GUIStyle normalStyle;
        GUIStyle normalBoxStyle;
        void DefaultStyle()
        {
            normalStyle = new GUIStyle(GUI.skin.label);
            normalStyle.alignment = TextAnchor.MiddleCenter;
            normalBoxStyle = new GUIStyle(GUI.skin.box);
            normalBoxStyle.alignment = TextAnchor.MiddleCenter;
            normalBoxStyle.normal.textColor = Color.white;
            normalBoxStyle.active.textColor = Color.white;
        }
        GUIStyle warningStyle;
        void WarningStyle()
        {
            warningStyle = new GUIStyle(GUI.skin.button);
            warningStyle.alignment = TextAnchor.MiddleLeft;
            warningStyle.fontStyle = FontStyle.Bold;
            warningStyle.normal.textColor = Color.yellow;
        }
        GUIStyle warningTextStyle;
        void WarningTextStyle()
        {
            warningTextStyle = new GUIStyle(GUI.skin.label);
            warningTextStyle.normal.textColor = Color.yellow;
        }
        GUIStyle buttonStyle;
        void ButtonStyle()
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleLeft;
        }

        List<string> duplicatedFiles = new List<string>();

        private Vector2 _scrollPosition = Vector2.zero;

        private void OnGUI()
        {
            float wm = position.width * 0.2f;
            float wl = position.width * 0.3f;
            float ws = position.width * 0.12f;
            float wt = position.width * 0.05f;

            if (normalStyle == null) DefaultStyle();
            if (warningStyle == null) WarningStyle();
            if (warningTextStyle == null) WarningTextStyle();
            if (buttonStyle == null) ButtonStyle();


            savedUIData = (UIPanelData)EditorGUILayout.ObjectField("UIPanelData", savedUIData, typeof(UIPanelData), true);

            if (savedUIData == null)
            {
                GUILayout.Space(10f);
                GUILayout.Label("UIPanelData(ScriptableObject) を指定してください", warningStyle);
                return;
            }

            EditorGUI.BeginChangeCheck();

            savedUIData.canvasRoot = (GameObject)EditorGUILayout.ObjectField("CanvasRoot", savedUIData.canvasRoot, typeof(GameObject), true );

            if (savedUIData.canvasRoot == null)
            {
                GUILayout.Space(10f);
                GUILayout.Label("UIPanelData(ScriptableObject) に CanvasRoot(prefab) を指定してください", warningStyle);
                return;
            }

            GUILayout.Space(10f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField("UI Prefabs Path", savedUIData.uiPanelPath);

            if (GUILayout.Button("Folder", GUILayout.Width(wm)) )
            {
                string path = (string.IsNullOrEmpty(savedUIData.uiPanelPath)) ? Application.dataPath : savedUIData.uiPanelPath;
                path = EditorUtility.OpenFolderPanel("UIPanel Path", path, "");

                if ( !string.IsNullOrEmpty(path) )
                {
                    savedUIData.uiPanelPath = path.Replace(Application.dataPath, "Assets");
                    Reset();
                    EditorGUILayout.EndHorizontal();
                    return;
                }
            }
            EditorGUILayout.EndHorizontal();

            if (string.IsNullOrEmpty(savedUIData.uiPanelPath))
            {
                GUILayout.Space(10f);
                GUILayout.Label("UIPanelを保存するPathを指定してください", warningStyle);
                return;
            }

            GUILayout.Space(10f);

            // ===== canvasRootの情報 =======================================================================
            if ( layerType == null || layerType.Length==0 || supportSafeArea == null || supportSafeArea.Length==0 )
            {
                var layerRoots = savedUIData.canvasRoot.GetComponentsInChildren<Canvas>();
                Dictionary<string, bool> dict = new Dictionary<string, bool>();
                foreach ( var r in layerRoots )
                {
                    var sup = r.GetComponent<SafeAreaSupport>();
                    dict[r.name] = (sup != null);
                }

                layerType = dict.Keys.ToArray();
                supportSafeArea = dict.Values.ToArray();
            }

            // ===== Layerの情報 =======================================================================
            EditorGUILayout.BeginHorizontal();
            GUILayout.Box("Layer Name", normalBoxStyle, GUILayout.Width(wm));
            GUILayout.Box("SafeArea", normalBoxStyle, GUILayout.Width(wm));
            EditorGUILayout.EndHorizontal();

            if (layerType == null || layerType.Length == 0 || supportSafeArea == null || supportSafeArea.Length == 0)
            {
                EditorGUILayout.LabelField("UIPanelData に CanvasRoot がセットされているか確認してください。", GUILayout.Width(position.width));
            }
            else
            {
                for (int i = 0; i < layerType.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(layerType[i], GUILayout.Width(wm));
                    EditorGUILayout.LabelField(supportSafeArea[i] ? "o" : "-", normalStyle, GUILayout.Width(wm));
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUILayout.Space(10f);


            try
            {
                // ===== UIPanelDataの生成 =======================================================================
                if (uiPanelData == null)
                {
                    SetClassTypeBuf();

                    uiPanelData = new List<PanelInfo>();

                    string[] prefabs = Directory.GetFiles(savedUIData.uiPanelPath, "*.prefab", SearchOption.AllDirectories);

                    var files = new List<string>();
                    duplicatedFiles = new List<string>();
                    foreach (var prefab in prefabs)
                    {
                        string panelName = Path.GetFileNameWithoutExtension(prefab);

                        var panelInfo = new PanelInfo();
                        var panel = savedUIData.panels.FirstOrDefault(_ => _.name == panelName);
                        panelInfo.layerTypeIdx = panel == null ? 0 : panel.layerTypeIdx;
                        panelInfo.blindType = panel == null ? BlindType.None : panel.blindType;
                        panelInfo.cache = panel == null ? false : panel.cache;
                        panelInfo.prefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefab, typeof(GameObject));
                        panelInfo.name = panelName;

                        if (files.Any(_ => _ == panelName))
                        {
                            duplicatedFiles.Add(panelName);
                        }

                        uiPanelData.Add(panelInfo);
                        files.Add(panelName);
                    }

                    savedUIData.panels = uiPanelData;
                }

                // ===== UIPanelの情報 =======================================================================
                float stretchWidth = position.width - wm - ws - ws - wt - wt - 40;// 40は右側に隙間を開けるmargin

                EditorGUILayout.BeginHorizontal();
                GUILayout.Box("UI Panel Name", normalBoxStyle, GUILayout.Width(wm));
                GUILayout.Box("Path", normalBoxStyle, GUILayout.Width(stretchWidth));
                GUILayout.Box("Layer", normalBoxStyle, GUILayout.Width(ws));
                GUILayout.Box("Blind", normalBoxStyle, GUILayout.Width(ws));
                GUILayout.Box("Cache", normalBoxStyle, GUILayout.Width(wt));
                GUILayout.Box("Preview", normalBoxStyle, GUILayout.Width(wt));
                EditorGUILayout.EndHorizontal();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

                // 変更チェック開始
                //EditorGUI.BeginChangeCheck();

                List<string> fileList = new List<string>();

                foreach (var panel in savedUIData.panels)
                {
                    EditorGUILayout.BeginHorizontal();
                    bool dup = duplicatedFiles.Any(_ => _ == panel.name);
                    if (dup)
                    {
                        GUILayout.Label(panel.name, warningTextStyle, GUILayout.Width(wm));
                    }
                    else
                    {
                        GUILayout.Label(panel.name, GUILayout.Width(wm));
                    }

                    string prefabPath = AssetDatabase.GetAssetPath(panel.prefab).Substring(savedUIData.uiPanelPath.Length);


                    if (dup)
                    {
                        if (GUILayout.Button(prefabPath + " (duplicated filename)", warningStyle, GUILayout.Width(stretchWidth)))
                        {
                            OpenPrefab(panel.prefab);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(prefabPath, buttonStyle, GUILayout.Width(stretchWidth)))
                        {
                            OpenPrefab(panel.prefab);
                        }
                    }
                    panel.layerTypeIdx = EditorGUILayout.Popup(panel.layerTypeIdx, layerType, GUILayout.Width(ws));
                    panel.blindType = (BlindType)EditorGUILayout.EnumPopup(panel.blindType, GUILayout.Width(ws));
                    panel.cache = EditorGUILayout.Toggle(panel.cache, GUILayout.Width(wt));

                    if (Application.isPlaying)
                    {
                        if (GUILayout.Button("Preview"))
                        {
                            var canvasRoot = GameObject.Find("CanvasRoot");
                            if (canvasRoot != null)
                            {
                                var ts = canvasRoot.transform.GetComponentsInChildren<Transform>().FirstOrDefault(_ => _.name == panel.name);
                                if (ts == null)
                                {
                                    UILayer.Inst.AddPanel(panel.name).Open((url)=>Debug.Log(url));
                                }
                                else
                                {
                                    ts.GetComponent<UIPanel>().Close(true);
                                }
                            }
                        }

                    }
                    else
                    {
                        GUILayout.Label("");
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();

                GUILayout.Space(20f);

                // 変更チェック終了（変更があった場合は保存）
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(savedUIData);
                    AssetDatabase.SaveAssets();
                }
            }
            catch ( Exception e)
            {
                Debug.LogError(e);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(wm)))
            {
                Reset();
            }
            if ( GUILayout.Button("Make Enum", GUILayout.Width(wm)))
            {
                MakeEnum();
            }
            if (Event.current.type == UnityEngine.EventType.Repaint) buttonRect = GUILayoutUtility.GetLastRect();
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(20f);
        }

        Rect buttonRect;

        void OpenPrefab(GameObject prefab)
        {
            AssetDatabase.OpenAsset(prefab);
        }

        List<string> types;
        public void SetClassTypeBuf()
        {
            types = new List<string>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    types.Add(type.Name);
                }
            }
        }

        public void Reset()
        {
            uiPanelData = null;
            layerType = null;
            supportSafeArea = null;
            types = null;
            EditorUtility.SetDirty(savedUIData);
        }

        public void MakeEnum()
        {
            StringBuilder sb = new StringBuilder();

            // PanelNameのEnum作成
            sb.AppendLine("public enum ePanelName\n{");
            foreach ( string name in savedUIData.panels.Select(_ => _.name) )
            {
                sb.Append("\t").Append(name).AppendLine(",");
            }
            sb.AppendLine("}");

            // EventButtonのEnum作成
            List<string> buttonList = new List<string>();
            sb.AppendLine();
            sb.AppendLine("public enum eEventName\n{");
            foreach (GameObject prefab in savedUIData.panels.Select(_ => _.prefab))
            {
                var buttons = prefab.GetComponentsInChildren<UIEvent>(true);
                foreach ( var button in buttons )
                {
                    buttonList.Add(button.name);
                }
            }
            foreach ( var name in buttonList.Distinct() )
            {
                sb.Append("\t").Append(name).AppendLine(",");
            }
            sb.AppendLine("}");

            var saveKey = GetSaveEnumPathKey();
            var savePath = EditorPrefs.GetString(saveKey, "Assets");

            // ファイル書き出し
            var path = EditorUtility.SaveFilePanel("Save Enum", savePath, "UIEnum", "cs");

            if (!string.IsNullOrEmpty(path))
            {
                string enumCs = sb.ToString();
                StreamWriter sw = new StreamWriter(path, false);
                sw.WriteLine(enumCs);
                sw.Flush();
                sw.Close();

                // 次回同じパスに保存できるように、セーブしたパスを保存
                savePath = Path.GetDirectoryName(path);
                EditorPrefs.SetString(saveKey, savePath);
            }

        }

        string GetSaveEnumPathKey()
        {
            string projectPath = Application.dataPath;
            string projectName = new DirectoryInfo(projectPath).Parent.Name;

            return projectName +"_AutoGeneratedEnumPath";
        }

    }
}