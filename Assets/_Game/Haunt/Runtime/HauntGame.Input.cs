using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Ecos.Haunt
{
    public sealed partial class HauntGame
    {
        // Gamepad includes DualShock devices recognized by the Input System. No
        // hardware-specific polling, native plugin or virtual mouse is needed.
        bool controllerActive, controllerHasAim, confirmNeedsRelease;
        Vector2 controllerAim = Vector2.down;
        int controlFrame = -1, consumedControlFrame = -1, controlContext = -1;
        int pauseChoice, deathChoice, shopChoice;
        float nextMenuStep;
        bool UsingController => controllerActive && Gamepad.current != null;

        void PollControlInput()
        {
            if (controlFrame == Time.frameCount) return;
            controlFrame = Time.frameCount;
            var pad = Gamepad.current;
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            bool padInput = pad != null && (pad.leftStick.ReadValue().sqrMagnitude > .06f
                || pad.rightStick.ReadValue().sqrMagnitude > .06f || pad.dpad.ReadValue().sqrMagnitude > .1f
                || pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
                || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame
                || pad.rightTrigger.ReadValue() > .2f || pad.leftTrigger.ReadValue() > .2f);
            bool desktopInput = (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4
                    || mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame));
            if (padInput) controllerActive = true;
            else if (desktopInput || pad == null) controllerActive = false;
            if (pad != null && pad.rightStick.ReadValue().sqrMagnitude > .06f)
            {
                controllerAim = pad.rightStick.ReadValue().normalized;
                controllerHasAim = true;
            }

            if (ConversationCapturesInput && !Paused) return;

            int context = !Playing ? 0 : State.IsDead ? 1 : Paused ? 2 : shopMenuOpen ? 3 : 4;
            if (context != controlContext)
            {
                bool hadContext = controlContext >= 0;
                controlContext = context;
                pauseChoice = deathChoice = shopChoice = 0;
                nextMenuStep = 0;
                // Opening a modal can never spend the same press on a purchase.
                confirmNeedsRelease = hadContext && pad != null && pad.buttonSouth.isPressed;
            }
            if (pad == null || !controllerActive) return;
            if (!pad.buttonSouth.isPressed) confirmNeedsRelease = false;
            bool confirm = pad.buttonSouth.wasPressedThisFrame && !confirmNeedsRelease;
            Vector2 step = MenuStep(pad);
            if (context == 0)
            {
                int choice = (int)selectedCharacter;
                if (step.x != 0) choice = (choice + (step.x > 0 ? 1 : 3)) % 4;
                else if (step.y != 0) choice = (choice + 2) % 4;
                selectedCharacter = (CharacterKind)choice;
                if (confirm || pad.startButton.wasPressedThisFrame)
                {
                    ConsumeControl();
                    StartFromMenu();
                }
            }
            else if (context == 1)
            {
                deathChoice = CycleMenuChoice(deathChoice, step, 3);
                if (confirm)
                {
                    ConsumeControl();
                    if (deathChoice == 0) BeginRun(System.Guid.NewGuid().GetHashCode());
                    else if (deathChoice == 1) BeginRun(State.Seed);
                    else ReturnToMenu();
                }
            }
            else if (context == 2)
            {
                pauseChoice = CycleMenuChoice(pauseChoice, step, 3);
                if (pad.buttonEast.wasPressedThisFrame) { ConsumeControl(); Paused = false; }
                else if (confirm)
                {
                    ConsumeControl();
                    if (pauseChoice == 0) Paused = false;
                    else if (pauseChoice == 1) ReturnToMenu();
                    else SetSoundEnabled(!soundEnabled);
                }
            }
            else if (context == 3)
            {
                if (pad.buttonEast.wasPressedThisFrame) { ConsumeControl(); CloseMerchant(); return; }
                if (pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame)
                {
                    shopWeapons = !shopWeapons;
                    shopWeaponPage = shopChoice = 0;
                }
                ShopItem[] items = ShopItems();
                int pages = Mathf.Max(1, (items.Length + 2) / 3);
                shopWeaponPage = Mathf.Clamp(shopWeaponPage, 0, pages - 1);
                if (pad.buttonNorth.wasPressedThisFrame)
                {
                    shopWeaponPage = (shopWeaponPage + 1) % pages;
                    shopChoice = 0;
                }
                int count = Mathf.Min(3, items.Length - shopWeaponPage * 3);
                shopChoice = CycleMenuChoice(shopChoice, step, count + 1);
                if (confirm)
                {
                    ConsumeControl();
                    if (shopChoice == count) CloseMerchant();
                    else ActivateShopItem(items[shopWeaponPage * 3 + shopChoice]);
                }
            }
        }

        Vector2 MenuStep(Gamepad pad)
        {
            Vector2 value = pad.dpad.ReadValue();
            if (value.sqrMagnitude < .25f) value = pad.leftStick.ReadValue();
            if (value.sqrMagnitude < .35f) { nextMenuStep = 0; return Vector2.zero; }
            if (Time.unscaledTime < nextMenuStep) return Vector2.zero;
            nextMenuStep = Time.unscaledTime + .22f;
            return Mathf.Abs(value.x) > Mathf.Abs(value.y)
                ? new Vector2(Mathf.Sign(value.x), 0) : new Vector2(0, Mathf.Sign(value.y));
        }

        static int CycleMenuChoice(int choice, Vector2 step, int count)
        {
            int direction = step.x > 0 || step.y < 0 ? 1 : step.x < 0 || step.y > 0 ? -1 : 0;
            return (choice + direction + count) % count;
        }

        void ConsumeControl() { consumedControlFrame = Time.frameCount; confirmNeedsRelease = true; }
        bool ControlsConsumed => consumedControlFrame == Time.frameCount;
        bool PausePressed() => !ControlsConsumed && ((Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame));
        bool MapPressed()
        {
            if (ControlsConsumed) return false;
            var pad = Gamepad.current;
            var touchpad = pad == null ? null : pad.TryGetChildControl<ButtonControl>("touchpadButton");
            return (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
                || (pad != null && pad.selectButton.wasPressedThisFrame) || (touchpad != null && touchpad.wasPressedThisFrame);
        }
        bool PowerPressed() => !ControlsConsumed && ((Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame));
        bool InteractPressed() => !ControlsConsumed && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame && !confirmNeedsRelease));
        bool DodgePressed() => !ControlsConsumed && ((Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            || (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || Gamepad.current.leftTrigger.wasPressedThisFrame)));
        bool FireHeld() => !ControlsConsumed && ((Mouse.current != null && Mouse.current.leftButton.isPressed && !controllerActive)
            || (Gamepad.current != null && Gamepad.current.rightTrigger.ReadValue() > .2f));

        Vector2 MovementInput(Keyboard keyboard)
        {
            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > .0324f) input += stick;
            }
            return Vector2.ClampMagnitude(input, 1);
        }

        Vector2 ControllerAimOr(Vector2 fallback) => controllerActive && Gamepad.current != null
            ? controllerHasAim ? controllerAim : lastMove : fallback;
        Vector2 ControlAimPoint() => controllerActive && Gamepad.current != null
            ? playerPosition + Vector2.up * .35f + ControllerAimOr(lastMove) * 4.5f
            : Mouse.current != null ? (Vector2)view.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : playerPosition + lastMove * 4.5f;

        // Index zero represents the current character's starting weapon. The
        // remainder is shared and never allocated during input polling.
        static readonly WeaponKind[] WeaponCycle = { WeaponKind.HunterCarbine, WeaponKind.Stapler, WeaponKind.Banana,
            WeaponKind.SpinningTop, WeaponKind.MirrorDisc, WeaponKind.RayLantern, WeaponKind.BoneFlute,
            WeaponKind.Umbrella, WeaponKind.HiveLauncher, WeaponKind.BoxingGlove,
            WeaponKind.Scattergun, WeaponKind.Repeater, WeaponKind.SoulCannon, WeaponKind.Pistol };

        static int WeaponDigit(Keyboard keyboard)
        {
            if (keyboard == null) return -1;
            if (keyboard.digit1Key.wasPressedThisFrame) return 0;
            if (keyboard.digit2Key.wasPressedThisFrame) return 1;
            if (keyboard.digit3Key.wasPressedThisFrame) return 2;
            if (keyboard.digit4Key.wasPressedThisFrame) return 3;
            if (keyboard.digit5Key.wasPressedThisFrame) return 4;
            if (keyboard.digit6Key.wasPressedThisFrame) return 5;
            if (keyboard.digit7Key.wasPressedThisFrame) return 6;
            if (keyboard.digit8Key.wasPressedThisFrame) return 7;
            if (keyboard.digit9Key.wasPressedThisFrame) return 8;
            if (keyboard.digit0Key.wasPressedThisFrame) return 9;
            return -1;
        }

        WeaponKind WeaponAt(int index) => index == 0 ? State.StartingWeapon : WeaponCycle[index];

        void UpdateWeaponSelection(Keyboard keyboard)
        {
            if (shopMenuOpen || ConversationCapturesInput || ControlsConsumed) return;
            int digit = WeaponDigit(keyboard);
            if (digit >= 0) State.Equip(WeaponAt(digit));
            var pad = Gamepad.current;
            float wheel = Mouse.current == null ? 0 : Mouse.current.scroll.ReadValue().y;
            int direction = pad != null && pad.rightShoulder.wasPressedThisFrame ? 1
                : pad != null && pad.leftShoulder.wasPressedThisFrame ? -1
                : keyboard != null && keyboard.tabKey.wasPressedThisFrame ? 1
                : Mathf.Abs(wheel) > .01f ? (wheel > 0 ? 1 : -1) : 0;
            if (direction == 0) return;
            int current = State.Weapon == State.StartingWeapon ? 0 : System.Array.IndexOf(WeaponCycle, State.Weapon);
            for (int step = 1; step <= WeaponCycle.Length; step++)
            {
                WeaponKind candidate = WeaponAt((current + direction * step + WeaponCycle.Length * 2) % WeaponCycle.Length);
                if (State.Owns(candidate)) { State.Equip(candidate); break; }
            }
        }
    }
}
