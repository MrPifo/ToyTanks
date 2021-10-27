using UnityEditor;
using UnityEngine;

public class ForceShield : MonoBehaviour {

	public float impactDuration = 1;

	[SerializeField, Range(0, 1)]
	float _DissolveValue;

	const int MAX_HITS_COUNT = 10;

	Renderer _renderer;
	MaterialPropertyBlock _mpb;

	int _hitsCount;
	Vector4[] _hitsObjectPosition = new Vector4[MAX_HITS_COUNT];
	float[] _hitsDuration = new float[MAX_HITS_COUNT];
	float[] _hitsTimer = new float[MAX_HITS_COUNT];
	float[] _hitRadius = new float[MAX_HITS_COUNT];

	void Awake() {
		_renderer = GetComponent<Renderer>();
		_mpb = new MaterialPropertyBlock();
	}

	void LateUpdate() {
		if(_mpb != null && _renderer != null) {
			UpdateHitsLifeTime();
			SendHitsToRenderer();
		}
	}

	public void AddHit(Vector3 worldPosition) {
		int id = GetFreeHitId();
		_hitsObjectPosition[id] = transform.InverseTransformPoint(worldPosition);
		_hitsDuration[id] = impactDuration;
		_hitRadius[id] = 1f;

		_hitsTimer[id] = 0;
	}
	int GetFreeHitId() {
		if(_hitsCount < MAX_HITS_COUNT) {
			_hitsCount++;
			return _hitsCount - 1;
		} else {
			float minDuration = float.MaxValue;
			int minId = 0;
			for(int i = 0; i < MAX_HITS_COUNT; i++) {
				if(_hitsDuration[i] < minDuration) {
					minDuration = _hitsDuration[i];
					minId = i;
				}
			}
			return minId;
		}
	}

	//1(max)..0(end of life time)
	float[] _hitsIntensity = new float[MAX_HITS_COUNT];

	public void ClearAllHits() {
		_hitsCount = 0;
		SendHitsToRenderer();
	}

	void UpdateHitsLifeTime() {
		for(int i = 0; i < _hitsCount;) {
			_hitsTimer[i] += Time.deltaTime * 2;
			if(_hitsTimer[i] > _hitsDuration[i]) {
				SwapWithLast(i);
			} else {
				i++;
			}
		}
	}

	void SwapWithLast(int id) {
		int idLast = _hitsCount - 1;
		if(id != idLast) {
			_hitsObjectPosition[id] = _hitsObjectPosition[idLast];
			_hitsDuration[id] = _hitsDuration[idLast];
			_hitsTimer[id] = _hitsTimer[idLast];
			_hitRadius[id] = _hitRadius[idLast];
		}
		_hitsCount--;
	}

	void SendHitsToRenderer() {
		_renderer.GetPropertyBlock(_mpb);

		_mpb.SetFloat("_DissolveValue", _DissolveValue);
		_mpb.SetFloat("_HitsCount", _hitsCount);
		_mpb.SetFloatArray("_HitsRadius", _hitRadius);

		for(int i = 0; i < _hitsCount; i++) {
			if(_hitsDuration[i] > 0f) {
				_hitsIntensity[i] = 1 - Mathf.Clamp01(_hitsTimer[i] / _hitsDuration[i]);
			}
		}

		_mpb.SetVectorArray("_HitsObjectPosition", _hitsObjectPosition);
		_mpb.SetFloatArray("_HitsIntensity", _hitsIntensity);
		_renderer.SetPropertyBlock(_mpb);
	}

	public void SimulateImpact() {
		Vector3 dir = new Vector3(Random.Range(-1f, 1f), 1, Random.Range(-1f, 1f));
		AddHit(transform.position + dir * 3.5f / 2f);
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(ForceShield))]
public class ForceShieldEditor : Editor {
	public override void OnInspectorGUI() {
		DrawDefaultInspector();
		var builder = (ForceShield)target;
		if(GUILayout.Button("Simulate Impact")) {
			builder.SimulateImpact();
		}
	}
}
#endif