using System.Collections.Generic;
using Aarthificial.Reanimation;
using UnityEngine;

namespace Jeff
{
    public class JeffRenderer : MonoBehaviour
    {
        [SerializeField] private List<AudioClip> stepSounds = new List<AudioClip>();
        [SerializeField] private List<AudioClip> hitSounds = new List<AudioClip>();

        private const string StateDriver = "state";
        private const string MovementDriver = "movement";
        private const string AttackDriver = "attack";
        private const string IsGroundedDriver = "isGrounded";
        private const string JumpDirectionDriver = "jumpDirection";
        private const string HitDirectionDriver = "hitDirection";
        private const string OneOffDriver = "_oneOff";
        private const string JumpNumberDriver = "jumpNumber";
        private const string FlipDriver = "flip";
        private const string HatDriver = "hasHat";
        private const string StepEventDriver = "stepEvent";

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

            _reanimator.AddOneOffDriver(OneOffDriver);
        }

        private void OnEnable()
        {
            _reanimator.AddListener(StepEventDriver, HandleStep);
            _reanimator.Ticked += UpdateColor;
            _controller.Hat += HandleHat;
            _controller.Hit += HandleHit;
        }

        private void OnDisable()
        {
            _reanimator.RemoveListener(StepEventDriver, HandleStep);
            _reanimator.Ticked -= UpdateColor;
            _controller.Hat -= HandleHat;
            _controller.Hit -= HandleHit;
        }

        private void Update()
        {
            var velocity = _controller.Velocity;

            _reanimator.Set(IsGroundedDriver, _controller.IsGrounded);
            _reanimator.Set(HatDriver, _hasHat);

            if (Mathf.Abs(_controller.DesiredMovementDirection.x) < 0.01f || Mathf.Abs(velocity.x) < 0.1f)
                _reanimator.Set(MovementDriver, false);
            else
                _reanimator.Set(MovementDriver, true);

            _reanimator.Set(JumpDirectionDriver, velocity.y > 0);

            _reanimator.Flip = _controller.FacingDirection < 0;
            _reanimator.Set(StateDriver, (int) _controller.State);
            _reanimator.Set(AttackDriver, _controller.AttackCompletion);

            _reanimator.Set(JumpNumberDriver, _controller.IsJumping && !_controller.IsFirstJump);
            _reanimator.Set(FlipDriver, _controller.JumpCompletion);

            var velocityDirection = velocity.normalized;
            if (velocityDirection.y > 0.65f)
                _reanimator.Set(HitDirectionDriver, 1);
            else if (velocityDirection.y < -0.65f)
                _reanimator.Set(HitDirectionDriver, 2);
            else
                _reanimator.Set(HitDirectionDriver, 0);

            if (_reanimator.WillChange(IsGroundedDriver, 1))
                HandleStep();

            if (_reanimator.WillChange(IsGroundedDriver, 1)
                || _reanimator.WillChange(StateDriver, (int) JeffState.Attack))
            {
                _reanimator.ForceRerender();
            }
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

        private void HandleHat()
        {
            _hasHat = !_hasHat;
        }

        private void HandleHit()
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