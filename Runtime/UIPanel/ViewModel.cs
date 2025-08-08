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
    public class ViewModel
    {
        private Action<Dictionary<string,object>> bind;
        private Dictionary<string,object> data;
        private List<string> updatedKeys = new List<string>();
        private Dictionary<string,List<string>> updatedListKeys = new ();

        public ViewModel(Action<Dictionary<string,object>> bind)
        {
            this.bind = bind;
        }

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
            // Debug.Log($"SyncListItemです : {rootKey}, {Id}, {key}, {value}");
            if ( !data.ContainsKey(rootKey) )
            {
                Debug.LogError("rootKeyがない : "+ rootKey);
                return false;
            }
            if ( data[rootKey].GetType() != typeof(List<IDictionary<string,object>>) )
            {
                Debug.LogError($"data[{rootKey}] がListじゃない : "+ data[rootKey].GetType());
                return false;
            }

            List<IDictionary<string,object>> arr = (List<IDictionary<string,object>>)data[rootKey];

            for ( int i=0 ; i<arr.Count ; i++ )
            {
                if ( arr[i].GetType() != typeof(Dictionary<string,object>) )
                {
                    Debug.LogError("型が違う "+ arr[i].GetType());
                    continue;
                }
                var dic = arr[i];

                if ( dic.ContainsKey("Id") && (string)dic["Id"] == id && dic.ContainsKey(key) )
                {
                    dic[key] = value;
                    return true;
                }
            }
            return false;
        }

        public object Get(string key)
        {
            if ( data.TryGetValue(key, out var value) )
            {
                return value;
            }
            return null;
        }

        public string GetJson()
        {
            return JsonConvert.SerializeObject(data);
        }

        /// <summary>
        /// 値を更新する
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="forceNotify"></param>
        public void UpdateData(string key, object obj, bool forceNotify = false)
        {
            if ( Sync(key, obj) == false ) return;

            if ( !updatedKeys.Contains(key) ) updatedKeys.Add(key);

            if ( forceNotify ) ForceNotify();
        }

        /// <summary>
        /// リストの値を更新する
        /// </summary>
        /// <param name="rootKey"></param>
        /// <param name="id"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
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

        public void Clear()
        {
            data?.Clear();
            updatedKeys.Clear();
            updatedListKeys.Clear();
        }

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