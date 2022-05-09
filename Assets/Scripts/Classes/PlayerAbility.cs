using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "CombatAbility", menuName = "Abilities/Ability", order = 1)]
public class PlayerAbility : ScriptableObject {

    public CombatAbility ability;
    public new string name;
    public Sprite icon;
    [TextArea]
    public string descShort;
    [TextArea]
    public string descLong;
    public float useDuration;
    public bool singleUse;

}
