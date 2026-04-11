namespace Script.GUI.Screen {
    public abstract partial class Screen {
        private Screen _previous;
        private Screen _next;

        public Screen Previous {
            get => _previous;
            set => _previous = value;
        }

        public Screen  Next {
            get => _next;
            set => _next = value;
        }
    }
}