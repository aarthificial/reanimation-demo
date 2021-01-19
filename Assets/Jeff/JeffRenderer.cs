using System.Collections.Generic;
using Aarthificial.Reanimation;
using UnityEngine;

namespace Jeff
{
    public class JeffRenderer : MonoBehaviour
    {
        private static class Drivers
        {
            public const string AttackCompletion = "attackCompletion";
            public const string FlipCompletion = "flipCompletion";
            public const string HasHat = "hasHat";
            public const string HitDirection = "hitDirection";
            public const string IsGrounded = "isGrounded";
            public const string IsMoving = "isMoving";
            public const string JumpDirection = "jumpDirection";
            public const string ShouldFlip = "shouldFlip";
            public const string State = "state";
            public const string StepEvent = "stepEvent";
        }

        [SerializeField] private List<AudioClip> stepSounds = new List<AudioClip>();
        [SerializeField] private List<AudioClip> hitSounds = new List<AudioClip>();

        private Reanimator _reanimator;
        private AudioSource _audioSource;
        private JeffController _controller;

        private bool _isRed;
        private bool _hasHat;

        private void Awake()
        {
            _reanimator = GetComponent<Reanimator>();
            _controller = GetComponent<JeffController>();
            _audioSource = GetComponent<AudioSource>();
        }

        private void OnEnable()
        {
            _controller.HitStateEntered += PlayHitSound;
            _controller.HatTriggered += ToggleHat;
            _reanimator.Ticked += UpdateColor;
            _reanimator.AddListener(Drivers.StepEvent, PlayStepSound);
        }

        private void OnDisable()
        {
            _controller.HitStateEntered -= PlayHitSound;
            _controller.HatTriggered -= ToggleHat;
            _reanimator.Ticked -= UpdateColor;
            _reanimator.RemoveListener(Drivers.StepEvent, PlayStepSound);
        }

        private void Update()
        {
            var velocity = _controller.Velocity;
            bool isMoving = Mathf.Abs(_controller.DesiredDirection.x) > 0 && Mathf.Abs(velocity.x) > 0.01f;
            
            int hitDirection;
            float speed = velocity.magnitude;
            var velocityDirection = velocity / speed;
            if (speed < 0.1f || velocityDirection.y < -0.65f)
                hitDirection = 2;
            else if (velocityDirection.y > 0.65f)
                hitDirection = 1;
            else
                hitDirection = 0;
            
            _reanimator.Flip = _controller.FacingDirection < 0;
            _reanimator.Set(Drivers.State, (int) _controller.State);
            _reanimator.Set(Drivers.IsGrounded, _controller.IsGrounded);
            _reanimator.Set(Drivers.IsMoving, isMoving);
            _reanimator.Set(Drivers.JumpDirection, velocity.y > 0);
            _reanimator.Set(Drivers.ShouldFlip, _controller.IsJumping && !_controller.IsFirstJump);
            _reanimator.Set(Drivers.FlipCompletion, _controller.JumpCompletion);
            _reanimator.Set(Drivers.AttackCompletion, _controller.AttackCompletion);
            _reanimator.Set(Drivers.HitDirection, hitDirection);

            bool didLandInThisFrame = _reanimator.WillChange(Drivers.IsGrounded, true);
            bool didDashInThisFrame = _reanimator.WillChange(Drivers.State, (int) JeffState.Attack);

            if (didLandInThisFrame)
                PlayStepSound();

            if (didLandInThisFrame || didDashInThisFrame)
                _reanimator.ForceRerender();
        }

        private void UpdateColor()
        {
            if (_controller.State == JeffState.Hit)
            {
                _reanimator.Renderer.color = _isRed ? Color.red : Color.white;
                _isRed = !_isRed;
            }
            else
            {
                _reanimator.Renderer.color = Color.white;
                _isRed = true;
            }
        }

        private void ToggleHat()
        {
            _hasHat = !_hasHat;
            _reanimator.Set(Drivers.HasHat, _hasHat);
        }

        private void PlayHitSound()
        {
            if (hitSounds.Count > 0)
                _audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Count)]);
        }

        private void PlayStepSound()
        {
            if (stepSounds.Count > 0)
                _audioSource.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Count)]);
        }
    }
}