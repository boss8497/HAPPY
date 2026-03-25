using Script.GUI.Interface;
using UnityEngine;
using VContainer;

public class TestAction : MonoBehaviour {
    
    private IScreenManager _screenManager;

    [Inject]
    public void Constructor(
        IScreenManager screenManager
    ) {
        _screenManager = screenManager;
    }


    public void OpenScreenTest() {
        _screenManager.OpenScreen("Test");
    }
}
