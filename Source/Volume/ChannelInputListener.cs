using System;
using Monocle;

namespace Celeste.Mod.AudioSplitter.Volume
{
    public class ChannelInputListener
    {
        public ButtonBinding DecreaseBinding;
        public ButtonBinding IncreaseBinding;

        public event Action OnDecrease;
        public event Action OnIncrease;

        private static readonly float holdingThreshold = 0.6f;
        private static readonly float repeatSpeed = 0.1f;

        bool isHolding = false;
        ButtonBinding currentlyHolding = null;

        float holdingTimer = 0f;
        float repeatTimer = 0f;

        public ChannelInputListener(ButtonBinding decrease, ButtonBinding increase)
        {
            DecreaseBinding = decrease;
            IncreaseBinding = increase;
        }

        public void Update()
        {
            bool? decreasePressed = DecreaseBinding?.Button.Pressed;
            bool? increasePressed = IncreaseBinding?.Button.Pressed;

            if (decreasePressed == true)
            {
                SetCurrentlyHoldingBinding(DecreaseBinding);
                OnDecrease();
            }
            if (increasePressed == true)
            {
                SetCurrentlyHoldingBinding(IncreaseBinding);
                OnIncrease();
            }

            isHolding = (currentlyHolding?.Button.Check == true);

            if (isHolding)
            {
                holdingTimer += Engine.DeltaTime;

                if (holdingTimer >= holdingThreshold)
                {
                    // shouldn't happen but better safe than sorry
                    if (currentlyHolding == null)
                        return;

                    repeatTimer += Engine.DeltaTime;
                    Action actionToRepeat = currentlyHolding switch
                    {
                        var value when value == DecreaseBinding => OnDecrease,
                        var value when value == IncreaseBinding => OnIncrease,
                        _ => null
                    };

                    if (actionToRepeat != null && repeatTimer >= repeatSpeed)
                    {
                        actionToRepeat();
                        repeatTimer %= repeatSpeed;
                    }
                }
            }
            else
            {
                SetCurrentlyHoldingBinding(null);
            }
        }

        private void ResetTimers()
        {
            holdingTimer = 0f;
            repeatTimer = 0f;
        }

        private void SetCurrentlyHoldingBinding(ButtonBinding binding)
        {
            currentlyHolding = binding;
            ResetTimers();
        }
    }
}
