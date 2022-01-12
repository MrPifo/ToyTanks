using DataStructures.RandomSelector;
using System;
using System.Collections.Generic;

namespace Sperlich.FSM {

    [Serializable]
    public class FSM<T> : IEquatable<FSM<T>> where T : struct, Enum {

        private T state;
        public T State => state;
        public string Text => state.ToString();
		private DynamicRandomSelector<T> WeightedStates;
		private Dictionary<T, float> CurrentWeights;

		// Functionality
		public void Push(T state) {
            this.state = state;
		}
		public void PushRandomWeighted() => Push(GetWeightedRandom());
        public bool IsState(T state) => State.ToString() == state.ToString();
		public void FillWeightedStates(List<(T state, float weight)> states) {
			WeightedStates = new DynamicRandomSelector<T>();
			CurrentWeights = new Dictionary<T, float>();
			foreach(var s in states) {
				WeightedStates.Add(s.state, s.weight);
				CurrentWeights.Add(s.state, s.weight);
			}
			WeightedStates.Build();
		}
		/// <summary>
		/// Changes the weight from a state. Range(0% - 100%)
		/// </summary>
		/// <param name="state"></param>
		/// <param name="newWeight"></param>
		public void ChangeWeight(T state, float newWeight) {
			newWeight = newWeight > 100 ? 100 : newWeight;
			newWeight = newWeight < 0 ? 0 : newWeight;
			WeightedStates.Remove(state);
			WeightedStates.Add(state, newWeight);
			WeightedStates.Build();
			CurrentWeights[state] = newWeight;
		}
		/// <summary>
		/// Adds weight to a state up to a 100%.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="weight"></param>
		public void AddWeight(T state, float weight) {
			ChangeWeight(state, CurrentWeights[state] + weight);
		}
		/// <summary>
		/// Takes weight from a state down to a 0%.
		/// </summary>
		/// <param name="state"></param>
		/// <param name="weight"></param>
		public void SubtractWeight(T state, float weight) {
			ChangeWeight(state, CurrentWeights[state] - weight);
		}

		// Random Values
		public T GetRandom() {
			var v = Enum.GetValues(typeof(T));
			return (T)v.GetValue(UnityEngine.Random.Range(0, v.Length));
		}
		public T GetWeightedRandom() {
			return WeightedStates.SelectRandomItem();
		}


		// Comparison
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
