namespace UuIiView
{
    public interface ITransition
    {
        void TransitionIn(System.Action onComplete);
        void TransitionOut(System.Action onComplete);
    }
}