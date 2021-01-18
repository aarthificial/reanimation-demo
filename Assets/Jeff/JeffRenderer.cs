using System.Collections.Generic;
using Aarthificial.Reanimation;
using UnityEngine;

namespace Jeff
{
    public class JeffRenderer : MonoBehaviour
    {
        private static class Drivers
        {
            public const string Attack = "attack";
            public const string FlipCompletion = "flipCompletion";
            public const string HasHat = "hasHat";
            public const string HitDirection = "hitDirection";
            public const string IdleTransition = "idleTransition";
            public const string IsGrounded = "isGrounded";
            public const string JumpDirection = "jumpDirection";
            public const string Movement = "movement";
            public const string ShouldFlip = "shouldFlip";
            public const string State = "state";
            public const string StepEvent = "stepEvent";
            public const string Temporary = "temporary";
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
            _controller.HitStateEntered += HandleHitStateEntered;
            _controller.HatTriggered += HandleHatTriggered;
            _reanimator.Ticked += UpdateColor;
            _reanimator.AddListener(Drivers.StepEvent, HandleStep);
            _reanimator.AddTemporaryDriver(Drivers.Temporary, Drivers.IdleTransition);
        }

        private void OnDisable()
        {
            _controller.HitStateEntered -= HandleHitStateEntered;
            _controller.HatTriggered -= HandleHatTriggered;
            _reanimator.Ticked -= UpdateColor;
            _reanimator.RemoveListener(Drivers.StepEvent, HandleStep);
            _reanimator.RemoveTemporaryDriver(Drivers.Temporary, Drivers.IdleTransition);
        }

        private void Update()
        {
            var velocity = _controller.Velocity;

            _reanimator.Flip = _controller.FacingDirection < 0;

            _reanimator.Set(Drivers.State, (int) _controller.State);
            _reanimator.Set(Drivers.IsGrounded, _controller.IsGrounded);
            _reanimator.Set(Drivers.Movement, _controller.IsMoving);
            _reanimator.Set(Drivers.JumpDirection, velocity.y > 0);
            _reanimator.Set(Drivers.ShouldFlip, _controller.IsJumping && !_controller.IsFirstJump);
            _reanimator.Set(Drivers.FlipCompletion, _controller.JumpCompletion);
            _reanimator.Set(Drivers.Attack, _controller.AttackCompletion);

            var velocityDirection = velocity.normalized;
            if (velocityDirection.y > 0.65f)
                _reanimator.Set(Drivers.HitDirection, 1);
            else if (velocityDirection.y < -0.65f)
                _reanimator.Set(Drivers.HitDirection, 2);
            else
                _reanimator.Set(Drivers.HitDirection, 0);

            bool didLandInThisFrame = _reanimator.WillChange(Drivers.IsGrounded, true);
            bool didDashInThisFrame = _reanimator.WillChange(Drivers.State, (int) JeffState.Attack);

            if (didLandInThisFrame)
                HandleStep();

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

        private void HandleHatTriggered()
        {
            _hasHat = !_hasHat;
            _reanimator.Set(Drivers.HasHat, _hasHat);
        }

        private void HandleHitStateEntered()
        {
            if (hitSounds.Count > 0)
                _audioSource.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Count)]);
        }

        private void HandleStep()
        {
            if (stepSounds.Count > 0)
                _audioSource.PlayOneShot(stepSounds[Random.Range(0, stepSounds.Count)]);
        }
    }
}