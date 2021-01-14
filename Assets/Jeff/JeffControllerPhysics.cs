using System;
using UnityEngine;

namespace Jeff
{
    [RequireComponent(typeof(Rigidbody2D))]
    public partial class JeffController
    {
        [Header("Walking")] [SerializeField] private float maxWalkCos = 0.5f;
        [SerializeField] private float walkSpeed = 7;

        [Header("Jumping")] [SerializeField] private float jumpTime = 0.4f;
        [SerializeField] private float jumpSpeed = 8;
        [SerializeField] private int jumpCount = 2;
        [SerializeField] private float fallSpeed = 12;
        [SerializeField] private AnimationCurve jumpFallOff = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("Getting a whooping")] [SerializeField]
        private Vector2 bounceBackStrength = new Vector2(8, 12);

        [SerializeField] private float unconsciousDuration = 0.2f;
        [SerializeField] private float hitCooldown = 0.2f;

        [Header("Giving a whooping")] [SerializeField]
        private float attackDuration = 0.2f;

        [SerializeField] private float attachCooldown = 0.2f;
        [SerializeField] private float attackSpeed = 12;

        public bool IsGrounded => _groundContact.HasValue;
        public Vector2 Velocity => _rigidbody2D.velocity;
        public float AttackCompletion => Mathf.Clamp01(_lastAttackTime / attackDuration);
        public float JumpCompletion => Mathf.Clamp01((Time.fixedTime - _timeOfLastJump) / jumpTime);
        public bool IsJumping => Time.fixedTime - _timeOfLastJump < jumpTime;
        public bool IsFirstJump => _jumpsLeft == jumpCount - 1;
        public int FacingDirection { get; private set; } = 1;

        private Rigidbody2D _rigidbody2D;
        private ContactFilter2D _contactFilter;
        private ContactPoint2D? _groundContact;
        private ContactPoint2D? _wallContact;
        private readonly ContactPoint2D[] _contacts = new ContactPoint2D[16];

        private float _timeOfLastJump;

        private float _lastHitTime;
        private float _lastAttackTime;
        private int _jumpsLeft;
        private bool _canAttack;
        private int _enemyLayer;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _enemyLayer = LayerMask.NameToLayer("Enemy");
            _contactFilter = new ContactFilter2D();
            _contactFilter.SetLayerMask(LayerMask.GetMask("Ground", "Enemy"));
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.layer != _enemyLayer) return;

            var relativePosition = (Vector2) transform.InverseTransformPoint(other.transform.position);
            var direction = (_rigidbody2D.centerOfMass - relativePosition).normalized;
            EnterHitState(direction);
        }

        private void Recalculate()
        {
            if (_rigidbody2D.velocity.x > 0.1f)
                FacingDirection = 1;
            else if (_rigidbody2D.velocity.x < -0.1f)
                FacingDirection = -1;

            _groundContact = null;
            _wallContact = null;

            float groundProjection = maxWalkCos;
            float wallProjection = maxWalkCos;

            int numberOfContacts = _rigidbody2D.GetContacts(_contactFilter, _contacts);
            for (var i = 0; i < numberOfContacts; i++)
            {
                var contact = _contacts[i];
                float projection = Vector2.Dot(Vector2.up, contact.normal);

                if (projection > groundProjection)
                {
                    _groundContact = contact;
                    groundProjection = projection;
                }
                else if (projection <= wallProjection && projection >= 0)
                {
                    _wallContact = contact;
                    wallProjection = projection;
                }
            }
        }

        private void FixedUpdate()
        {
            Recalculate();

            switch (State)
            {
                case JeffState.Default:
                    UpdateDefault();
                    break;
                case JeffState.Attack:
                    UpdateAttackState();
                    break;
                case JeffState.Hit:
                    UpdateHitState();
                    break;
            }
        }

        private void EnterHitState(Vector2 direction)
        {
            if (State != JeffState.Hit && Time.fixedTime - _lastHitTime < hitCooldown) return;
            State = JeffState.Hit;
            
            _lastHitTime = Time.fixedTime;
            _rigidbody2D.AddForce(
                direction * bounceBackStrength - _rigidbody2D.velocity,
                ForceMode2D.Impulse
            );
        }

        private void UpdateHitState()
        {
            _rigidbody2D.AddForce(Physics2D.gravity * 4);
            if (Time.fixedTime - _lastHitTime > unconsciousDuration
                && (_groundContact.HasValue || _wallContact.HasValue))
            {
                _lastHitTime = Time.fixedTime;
                EnterDefaultState();
            }
        }

        private void EnterAttackState()
        {
            if (State != JeffState.Default || Time.fixedTime - _lastAttackTime < attachCooldown || !_canAttack) return;
            State = JeffState.Attack;
            
            _canAttack = false;
            _lastAttackTime = Time.fixedTime;
        }

        private void UpdateAttackState()
        {
            _rigidbody2D.AddForce(
                new Vector2(FacingDirection * attackSpeed, 0) - _rigidbody2D.velocity,
                ForceMode2D.Impulse
            );
            if (Time.fixedTime - _lastAttackTime > attackDuration || _wallContact.HasValue)
            {
                _lastAttackTime = Time.fixedTime;
                EnterDefaultState();
            }
        }

        private void EnterDefaultState()
        {
            State = JeffState.Default;
        }

        private void UpdateDefault()
        {
            var previousVelocity = _rigidbody2D.velocity;
            var velocityChange = Vector2.zero;

            if (WantsToJump && IsJumping)
            {
                float currentJumpSpeed = jumpSpeed;
                if (!IsFirstJump)
                    currentJumpSpeed /= 2;
                currentJumpSpeed *= jumpFallOff.Evaluate(JumpCompletion);

                velocityChange.y = currentJumpSpeed - previousVelocity.y;
            }
            else if (_groundContact.HasValue)
            {
                _jumpsLeft = jumpCount;
                _canAttack = true;
            }
            else
            {
                velocityChange.y = (-fallSpeed - previousVelocity.y) / 8;
            }

            velocityChange.x = (DesiredMovementDirection.x * walkSpeed - previousVelocity.x) / 4;

            if (_wallContact.HasValue)
            {
                var wallDirection = (int) Mathf.Sign(_wallContact.Value.point.x - transform.position.x);
                var walkDirection = (int) Mathf.Sign(DesiredMovementDirection.x);

                if (walkDirection == wallDirection)
                    velocityChange.x = 0;
            }

            _rigidbody2D.AddForce(velocityChange, ForceMode2D.Impulse);
        }
    }
}