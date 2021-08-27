using EasyState.FSM.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace FSM_BrownTank.Action
{

    public class Idle : Action<BrownTank>
    {
        public override void Act(BrownTank data)
        {
            Debug.Log("IDLE");
        }
    }
}