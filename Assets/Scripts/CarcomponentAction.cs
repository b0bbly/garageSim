using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class CarComponentAction
{
    public string actionName;
    public KeyCode hotkey = KeyCode.None;
    public bool requiresSeated = true;
    public UnityEvent onActivate;
    public UnityEvent onDeactivate;
}