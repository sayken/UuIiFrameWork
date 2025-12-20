using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace UuIiView
{
    /// <summary>
    /// リアクティブなデータバインディングを提供するViewModelクラス
    /// JSON/Dictionaryデータの管理と変更通知を担当する
    /// </summary>
    public class ViewModel
    {
        private Action<Dictionary<string, object>> bind;
        private Dictionary<string, object> data;
        private List<string> updatedKeys = new List<string>();
        private Dictionary<string, List<string>> updatedListKeys = new();

        /// <summary>
        /// ViewModelを初期化する
        /// </summary>
        /// <param name="bind">データ更新時に呼び出されるバインディングコールバック</param>
        public ViewModel(Action<Dictionary<string, object>> bind)
        {
            this.bind = bind;
        }

        /// <summary>
        /// JSON文字列からデータを初期化する
        /// </summary>
        /// <param name="json">初期化用のJSON文字列</param>
        public void Init(string json)
        {
            data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var result = new Dictionary<string,object>();
            foreach ( var kv in data )
            {
                if ( kv.Value == null )
                {
                    Debug.LogError($"{kv.Key} value is null");
                    continue;
                }
                if (kv.Value.GetType() == typeof(JArray) )
                {
                    List<IDictionary<string,object>> objs = new ();
                    foreach ( var jtoken in (JArray)kv.Value )
                    {
                        if ( jtoken is JObject )
                        {
                            var obj = JsonConvert.DeserializeObject<Dictionary<string,object>>(jtoken.ToString());
                            objs.Add(obj);
                        }
                    }
                    result[kv.Key] = objs;
                }
                else if ( kv.Value.GetType() == typeof(JObject) )
                {
                    result[kv.Key] = JsonConvert.DeserializeObject<Dictionary<string,object>>(kv.Value.ToString());
                }
                else
                {
                    result[kv.Key] = kv.Value;
                }
            }
            data = result;

            BaseInit(data);
        }

        /// <summary>
        /// Dictionaryからデータを初期化する
        /// </summary>
        /// <param name="dic">初期化用のDictionary</param>
        public void Init(Dictionary<string, object> dic) => BaseInit(dic);

        void BaseInit(Dictionary<string, object> dic)
        {
            data = new Dictionary<string, object>(dic);
            bind?.Invoke(data);
        }

        /// <summary>
        /// Viewの操作で変更された値をdataに反映（Viewへの更新通知はしない）
        /// </summary>
        /// <param name="key">クラス内の変更する値のキー</param>
        /// <param name="value">変更したい値</param>
        /// <returns></returns>
        public bool Sync(string key, object value)
        {
            if ( data == null )
            {
                Debug.LogError("Need to call InitData first.");
                return false;
            }
            data[key] = value;
            return true;
        }

        /// <summary>
        /// Viewの操作で変更されたリストの値をdataに反映（Viewへの更新通知はしない）
        /// </summary>
        /// <param name="rootKey">リストのキー</param>
        /// <param name="id">リストに含まれているクラスに設定されているID</param>
        /// <param name="key">クラス内の変更する値のキー</param>
        /// <param name="value">変更したい値</param>
        /// <returns>成功 = true, 失敗 = false</returns>
        public bool SyncListItem(string rootKey, string id, string key, object value)
        {
            if (data == null)
            {
                Debug.LogError("[ViewModel] data is null. Call Init first.");
                return false;
            }

            if (!data.TryGetValue(rootKey, out var rootValue))
            {
                Debug.LogError($"[ViewModel] rootKey not found: {rootKey}");
                return false;
            }

            if (rootValue is not List<IDictionary<string, object>> arr)
            {
                Debug.LogError($"[ViewModel] data[{rootKey}] is not List<IDictionary<string,object>>: {rootValue?.GetType()}");
                return false;
            }

            foreach (var item in arr)
            {
                if (item is not Dictionary<string, object> dic)
                {
                    continue;
                }

                // 安全な型チェックとキャスト
                if (dic.TryGetValue("Id", out var idValue) &&
                    idValue?.ToString() == id &&
                    dic.ContainsKey(key))
                {
                    dic[key] = value;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 指定されたキーの値を取得する
        /// </summary>
        /// <param name="key">取得する値のキー</param>
        /// <returns>キーに対応する値、存在しない場合はnull</returns>
        public object Get(string key)
        {
            if ( data.TryGetValue(key, out var value) )
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// 現在のデータをJSON文字列として取得する
        /// </summary>
        /// <returns>JSON形式の文字列</returns>
        public string GetJson()
        {
            return JsonConvert.SerializeObject(data);
        }

        /// <summary>
        /// 値を更新し、オプションでViewに通知する
        /// </summary>
        /// <param name="key">更新する値のキー</param>
        /// <param name="obj">新しい値</param>
        /// <param name="forceNotify">trueの場合、即座にViewに更新を通知する</param>
        public void UpdateData(string key, object obj, bool forceNotify = false)
        {
            if ( Sync(key, obj) == false ) return;

            if ( !updatedKeys.Contains(key) ) updatedKeys.Add(key);

            if ( forceNotify ) ForceNotify();
        }

        /// <summary>
        /// リスト内の特定アイテムの値を更新する
        /// </summary>
        /// <param name="rootKey">リストのキー</param>
        /// <param name="id">更新対象アイテムのID</param>
        /// <param name="key">更新するプロパティのキー</param>
        /// <param name="value">新しい値</param>
        /// <param name="forceNotify">trueの場合、即座にViewに更新を通知する</param>
        public void UpdateListData(string rootKey, string id, string key, string value, bool forceNotify = false)
        {
            // Debug.LogWarning($"[UpdateListData] {rootKey}, {id}, {key}, {value}, {forceNotify}");
            if ( SyncListItem(rootKey, id, key, value) == false ) return;

            if ( !updatedListKeys.ContainsKey(rootKey) )
            {
                updatedListKeys[rootKey] = new();
            }
            if ( !updatedListKeys[rootKey].Contains(id) )
            {
                updatedListKeys[rootKey].Add(id);
            }

            if ( forceNotify ) ForceNotify();
        }

        /// <summary>
        /// 蓄積された更新をViewに強制通知する
        /// UpdateDataやUpdateListDataで蓄積された変更を一括でViewに反映する
        /// </summary>
        public void ForceNotify()
        {
            // 更新された値を抽出
            var dat = data.Where(_=>updatedKeys.Contains(_.Key)).ToDictionary(x=>x.Key, x=>x.Value);

            // 更新されたリストの要素を抽出
            foreach ( var rootKey in updatedListKeys.Keys)
            {
                var dataList = (IList)data[rootKey];
                var list = new List<Dictionary<string, object>>();
                foreach ( Dictionary<string,object> d in dataList )
                {
                    if ( !d.ContainsKey("Id") || !updatedListKeys[rootKey].Contains(d["Id"]) ) continue;
                    list.Add(d);
                }
                dat[rootKey] = list;
            }

            bind?.Invoke(dat);

            updatedKeys.Clear();
            updatedListKeys.Clear();
        }

        /// <summary>
        /// すべてのデータと更新履歴をクリアする
        /// </summary>
        public void Clear()
        {
            data?.Clear();
            updatedKeys.Clear();
            updatedListKeys.Clear();
        }

        /// <summary>
        /// デバッグ用にデータの内容と更新キーを文字列として出力する
        /// </summary>
        /// <returns>整形されたデータ内容の文字列</returns>
        public string Log()
        {
            StringBuilder sb = new ();
            sb.AppendLine("[contents]");
            foreach ( var kv in data)
            {
                if ( kv.Value is IDictionary<string,object> )
                {
                    sb.Append(kv.Key).AppendLine(" : {");
                    foreach ( var v in (IDictionary<string,object>)kv.Value)
                    {
                        sb.Append("    ").Append(v.Key).Append(" : ").AppendLine(v.Value.ToString());
                    }
                    sb.AppendLine("}");
                }
                else if ( kv.Value is List<IDictionary<string,object>> )
                {
                    sb.Append(kv.Key).AppendLine(" : [");
                    foreach ( var l in (List<IDictionary<string,object>>)kv.Value )
                    {
                        sb.AppendLine("    {");
                        foreach ( var v in (IDictionary<string,object>)l)
                        {
                            sb.Append("        ").Append(v.Key).Append(" : ").AppendLine(v.Value.ToString());
                        }
                        sb.AppendLine("    }");
                    }
                    sb.AppendLine("]");
                }
                else
                {
                    sb.Append(kv.Key).Append(" : ").AppendLine(kv.Value.ToString());
                }
            }
            sb.AppendLine("\n[updatedKeys]");
            foreach ( var key in updatedKeys )
            {
                sb.AppendLine(key);
            }

            return sb.ToString();
        }
    }
}