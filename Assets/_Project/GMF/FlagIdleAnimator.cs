using UnityEngine;

namespace GMF
{
    public class FlagIdleAnimator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Flag _flag; // referencia al Flag

        [Header("Float Settings")]
        [SerializeField] private float _floatAmplitude = 0.25f;
        [SerializeField] private float _floatSpeed = 2f;

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 45f;

        private Vector3 _startLocalPos;
        private bool _initialized;

        private string _lastState;

        private void Awake()
        {
            if (_flag == null)
                _flag = GetComponentInParent<Flag>();

            _startLocalPos = transform.localPosition;
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized || _flag == null)
                return;

            if (_flag.State != _lastState)
            {
                OnStateChanged();
                _lastState = _flag.State;
            }

            if (!IsIdleState())
                return;

            ApplyFloat();
            ApplyRotation();
        }

        private void OnStateChanged()
        {
            if (IsIdleState())
            {
                _startLocalPos = transform.localPosition;
                transform.localRotation = Quaternion.identity;
            }
        }

        private bool IsIdleState()
        {
            // 🔥 RECOMENDADO (ENUM)
            if (_flag.State == "Idle" || _flag.State == "Dropped")
                return true;

            // Si ya usas enum:
            // return _flag.State == FlagState.Idle || _flag.State == FlagState.Dropped;

            return false;
        }

        private void ApplyFloat()
        {
            float offsetY = Mathf.Sin(Time.time * _floatSpeed) * _floatAmplitude;

            Vector3 pos = _startLocalPos;
            pos.y += offsetY;

            transform.localPosition = pos;
        }

        private void ApplyRotation()
        {
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.Self);
        }

        private void OnDisable()
        {
            // 🔧 Reset visual para evitar offsets raros
            if (_initialized)
                transform.localPosition = _startLocalPos;
        }
    }
}