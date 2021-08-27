using EasyState.FSM.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FSM_BrownTank.Action
{

    public class Attack : Action<BrownTank>
    {
        public override void Act(BrownTank data)
        {
            Debug.Log("Attack");
        }
    }
}