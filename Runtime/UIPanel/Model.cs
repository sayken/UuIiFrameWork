using System.Collections.Generic;
using System.Linq;

namespace UuIiView
{
    public class Model
    {
        List<IModel> models = new ();

        public void Add(IModel model)
        {
            var type = model.GetType();
            if ( models.Any(_=>_.GetType()==type) == false )
            {
                models.Add(model);
            }
        }

        public T Get<T>() where T : IModel
        {
            var type = typeof(T);
            var ret = models.FirstOrDefault(_=>_.GetType()==type);
            if ( ret != null )
            {
                return (T)ret;
            }
            return default(T);
        }
    }
}