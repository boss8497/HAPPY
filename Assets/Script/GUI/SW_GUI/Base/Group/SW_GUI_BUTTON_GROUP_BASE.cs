using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SW.GUI.Base {
    public enum SelectType {
        Single,
        Multiple,
    }

    public abstract class SW_GUI_BUTTON_GROUP_BASE : SW_GUI_BASE {
        [SerializeField]
        private SelectType selectType = SelectType.Single;

        [SerializeField]
        protected List<SW_GUI_BUTTON_GROUP_ELEMENT_BASE> _elementsList = new();
        public List<SW_GUI_BUTTON_GROUP_ELEMENT_BASE> ElementsList => _elementsList;

        protected Dictionary<int, SW_GUI_BUTTON_GROUP_ELEMENT_BASE> _elementsDictionary = new();


        [SerializeField]
        protected Transform content;


        private void Awake() {
            Initialize();
            InitializeAwake();
        }

        protected virtual void InitializeAwake() { }

        /// <summary>
        /// 한번만 초기화 할것 여러번 호출 금지
        /// 처음 인스펙터에서 미리 등록한 element 추가
        /// </summary>
        public override void Initialize() {
            foreach (var element in _elementsList) {
                element.Group = this;
                if (Exists(element)) continue;
                if (element.Key == -1) {
                    element.Key = CreateRandomKey();
                }

                _elementsDictionary[element.Key] = element;
            }
        }

        public void Register(SW_GUI_BUTTON_GROUP_ELEMENT_BASE element) {
            if (Exists(element)) return;

            element.Key                      = CreateRandomKey();
            element.Group                    = this;
            _elementsDictionary[element.Key] = element;
            _elementsList.Add(element);
        }

        public void Unregister(SW_GUI_BUTTON_GROUP_ELEMENT_BASE element) {
            if (!Exists(element)) return;

            _elementsList.Remove(element);
            _elementsDictionary.Remove(element.Key);
            element.Key   = -1;
            element.Group = null;
        }

        private int CreateRandomKey() {
            var key = Random.Range(0, int.MaxValue);
            while (_elementsDictionary.ContainsKey(key)) {
                key = Random.Range(0, int.MaxValue);
            }

            return key;
        }

        private bool Exists(SW_GUI_BUTTON_GROUP_ELEMENT_BASE element) {
            return _elementsDictionary.ContainsKey(element.Key);
        }

        private SW_GUI_BUTTON_GROUP_ELEMENT_BASE GetElement(int key) {
            return _elementsDictionary.GetValueOrDefault(key);
        }

        public void Select(SW_GUI_BUTTON_GROUP_ELEMENT_BASE element) {
            if (!Exists(element)) return;

            switch (selectType) {
                case SelectType.Single: {
                    if (element.Selected) return;

                    foreach (var selectedElement in _elementsDictionary.Values) {
                        if (selectedElement.Selected == false) continue;
                        selectedElement.Selected = false;
                    }

                    switch (element.option) {
                        case ElementOption.None:
                            if (element.Selected == false) {
                                element.Selected = true;
                            }

                            break;

                        case ElementOption.SelectToDeSelect:
                            element.Selected = !element.Selected;
                            break;
                    }
                }
                    break;

                case SelectType.Multiple: {
                    switch (element.option) {
                        case ElementOption.None:
                            if (element.Selected == false) {
                                element.Selected = true;
                            }

                            break;

                        case ElementOption.SelectToDeSelect:
                            element.Selected = !element.Selected;
                            break;
                    }
                }
                    break;
            }
        }

        public void DeSelect(SW_GUI_BUTTON_GROUP_ELEMENT_BASE element, bool force = false) {
            if ((!Exists(element) || element.Selected == false) && force == false) return;
            element.Selected = false;
        }

        public void AllDeselect() {
            foreach (var element in _elementsDictionary.Values) {
                if (element.Selected == false) continue;
                element.Selected = false;
            }
        }

        public void ReleaseSelector() {
            AllDeselect();
            foreach (var element in _elementsDictionary.Values) {
                element.Group = null;
            }

            _elementsDictionary.Clear();
            _elementsList.Clear();
        }
    }
}