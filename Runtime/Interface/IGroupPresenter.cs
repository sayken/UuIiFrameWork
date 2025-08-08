using System.Collections.Generic;

namespace UuIiView
{
    public interface IGroupPresenter
    {
        List<IPresenter> presenters { get; set; }

        void AddPresenter(IPresenter presenter);

        void OnEvent(CommandLink commandLink);
    }
}