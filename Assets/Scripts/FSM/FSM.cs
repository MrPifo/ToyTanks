using SimpleMan.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.FSM {

    [ExecuteAlways]
    [System.Serializable]
    public class FSM<T> where T : struct, System.Enum {

        static FSM_Helper instance;
        public static FSM_Helper Instance {
            get {
                var inst = Object.FindObjectOfType<FSM_Helper>();
                if(inst == null) {
                    instance = new GameObject().AddComponent<FSM_Helper>();
				}
                return instance;
			}
		}
        T state;
        public T State => state;
        public string Text => state.ToString();
        HashSet<T> Queue { get; set; }
        HashSet<Coroutine> Coroutines { get; set; }

        public void Push(T state, bool interrupt = false) {
            this.state = state;

            // interrupt discards all delayed states
            if(interrupt) {
                foreach(Coroutine c in Coroutines) {
                    instance.StopCoroutine(c);
				}
			}
		}
        public void Push(T state, float delay) {
            if(Queue.Contains(state) == false) {
                Queue.Add(state);
                Coroutines.Add(instance.Delay(delay, () => {
                    if(Queue.Contains(state)) {
                        this.state = state; 
                        Queue.Remove(state);
                    }
                }));
            }
		}
    }
}
