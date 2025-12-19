using System;
using System.Collections.Generic;
using UnityEngine;

namespace UuIiView
{
    public class Model
    {
        private readonly List<IModel> _models = new();
        private readonly Dictionary<Type, IModel> _modelCache = new();

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

        public T Get<T>() where T : IModel
        {
            var type = typeof(T);
            if (_modelCache.TryGetValue(type, out var model))
            {
                return (T)model;
            }
            return default;
        }

        public IReadOnlyList<IModel> GetAll() => _models.AsReadOnly();
    }
}