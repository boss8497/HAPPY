using System.Collections.Generic;
using UnityEngine.Pool;

namespace Script.GUI {
    public enum SelectorType {
        Single,
        Multiple,
    }

    public abstract class Selector : Screen.Screen {
        public SelectorType type;

        private Dictionary<int, List<SelectElement>> _elements = new();


        public void Register(SelectElement element) {
            if (Exists(element)) return;
            _elements[element.Key].Add(element);
        }

        public void Unregister(SelectElement element) {
            if (!Exists(element, false)) return;
            var list = _elements[element.Key];
            list.Remove(element);

            // Key에 해당하는 Element가 하나도 없으면 Key 제거
            if (list.Count <= 0) {
                _elements.Remove(element.Key);
                ListPool<SelectElement>.Release(list);
            }
        }

        private bool Exists(SelectElement element, bool createKey = true) {
            if (createKey && _elements.ContainsKey(element.Key) == false) {
                var pool = ListPool<SelectElement>.Get();
                _elements.Add(element.Key, pool);
                return false;
            }

            if (_elements.TryGetValue(element.Key, out var elements)) {
                return elements.Contains(element);
            }

            return false;
        }

        private List<SelectElement> GetElements(int key) {
            return _elements.GetValueOrDefault(key);
        }

        public void Select(SelectElement element) {
            // 혹시라도 등록 안되어 있으면 등록
            Register(element);

            switch (type) {
                case SelectorType.Single: {
                    foreach (var selectedElement in GetElements(element.Key)) {
                        selectedElement.Selected = false;
                    }
                }
                    break;
            }

            element.Selected = true;
        }

        public void DeSelect(SelectElement element) {
            // 혹시라도 등록 안되어 있으면 등록
            Register(element);

            element.Selected = false;
        }

        public void AllDeselect() {
            foreach (var elements in _elements) {
                foreach (var element in elements.Value) {
                    element.Selected = false;
                }
            }
        }

        public void AllDeselect(int key) {
            if (_elements.TryGetValue(key, out var elements)) {
                foreach (var element in elements) {
                    element.Selected = false;
                }
            }
        }

        public void ReleaseSelector() {
            foreach (var elements in _elements.Values) {
                foreach (var element in elements) {
                    element.Selected = false;
                }
                elements.Clear();
                ListPool<SelectElement>.Release(elements);
            }
            _elements.Clear();
        }
        
        
        #region Unity Event

        private void OnDestroy() {
            ReleaseSelector();

            OnDestroyInternal();
        }

        public virtual void OnDestroyInternal() {
        }

        #endregion
    }
}