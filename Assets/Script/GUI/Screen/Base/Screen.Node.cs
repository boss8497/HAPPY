namespace Script.GUI.Screen {
    public abstract partial class Screen {
        private IScreen _previous;
        private IScreen _next;

        public IScreen Previous {
            get => _previous;
            set => _previous = value;
        }

        public IScreen Next {
            get => _next;
            set => _next = value;
        }
    }
}