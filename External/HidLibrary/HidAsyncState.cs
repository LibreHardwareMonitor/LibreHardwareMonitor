namespace HidLibrary
{
    public class HidAsyncState
    {
        private readonly object _callerDelegate;
        private readonly object _callbackDelegate;

        public HidAsyncState(object callerDelegate, object callbackDelegate)
        {
            _callerDelegate = callerDelegate;
            _callbackDelegate = callbackDelegate;
        }

        public object CallerDelegate { get { return _callerDelegate; } }
        public object CallbackDelegate { get { return _callbackDelegate; } }
    }
}
