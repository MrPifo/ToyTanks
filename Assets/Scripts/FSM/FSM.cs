using SimpleMan.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.FSM {

    public class FSM<T> : IEquatable<FSM<T>> where T : struct, System.Enum {

        static FSM_Helper instance;
        public static FSM_Helper Instance {
            get {
                var inst = UnityEngine.Object.FindObjectOfType<FSM_Helper>();
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
        public bool IsState(T state) => State.ToString() == state.ToString();
        public T GetRandom() => RandomEnumValue();
        public static T RandomEnumValue() {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(UnityEngine.Random.Range(0, v.Length));
        }
        public static implicit operator T(FSM<T> value) => value.State;
        public override string ToString() => state.ToString();
        public static bool operator ==(FSM<T> left, FSM<T> right) {
			return EqualityComparer<FSM<T>>.Default.Equals(left, right);
		}
		public static bool operator !=(FSM<T> left, FSM<T> right) {
			return !(left == right);
		}
		public override bool Equals(object obj) {
			return Equals(obj as FSM<T>);
		}
		public bool Equals(FSM<T> other) {
			return other != null && EqualityComparer<T>.Default.Equals(state, other.state);
		}
		public override int GetHashCode() {
			return 259708774 + state.GetHashCode();
		}
	}
}
