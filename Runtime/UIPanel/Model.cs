using System;
using System.Collections.Generic;
using UnityEngine;

namespace UuIiView
{
    /// <summary>
    /// IModelインスタンスを管理するコンテナクラス
    /// 型ベースでモデルを登録・取得する機能を提供する
    /// </summary>
    public class Model
    {
        private readonly List<IModel> _models = new();
        private readonly Dictionary<Type, IModel> _modelCache = new();

        /// <summary>
        /// モデルを登録する
        /// 同一型のモデルが既に登録されている場合は無視される
        /// </summary>
        /// <param name="model">登録するモデルインスタンス</param>
        public void Add(IModel model)
        {
            if (model == null)
            {
                Debug.LogError("[Model] Cannot add null model");
                return;
            }

            var type = model.GetType();
            if (!_modelCache.ContainsKey(type))
            {
                _modelCache[type] = model;
                _models.Add(model);
            }
        }

        /// <summary>
        /// 指定した型のモデルを取得する
        /// </summary>
        /// <typeparam name="T">取得するモデルの型</typeparam>
        /// <returns>登録されているモデル、存在しない場合はdefault</returns>
        public T Get<T>() where T : IModel
        {
            var type = typeof(T);
            if (_modelCache.TryGetValue(type, out var model))
            {
                return (T)model;
            }
            return default;
        }

        /// <summary>
        /// 登録されている全モデルを取得する
        /// </summary>
        /// <returns>登録済みモデルの読み取り専用リスト</returns>
        public IReadOnlyList<IModel> GetAll() => _models.AsReadOnly();
    }
}