using UnityEngine;
using UnityEngine.InputSystem;

public class TestInput : MonoBehaviour
{
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.zKey.wasPressedThisFrame) Debug.Log("Z funciona!");
        if (keyboard.xKey.wasPressedThisFrame) Debug.Log("X funciona!");
        if (keyboard.cKey.wasPressedThisFrame) Debug.Log("C funciona!");
    }
}