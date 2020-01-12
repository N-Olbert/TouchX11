using JetBrains.Annotations;
using TX11Shared.Keyboard;

namespace TX11Business.BusinessObjects.Keyboard
{
    internal class ModifierMapping
    {
        [NotNull]
        private readonly byte[] mapping =
        {
            0, 0, //Shift
            0, 0, //Lock
            0, 0, //Control
            0, 0, //Mod1
            0, 0, //Mod2
            0, 0, //Mod3
            0, 0, //Mod4
            0, 0, //Mod5
        };

        internal byte this[int i] => this.mapping[i];

        internal void SetShiftMapping(byte key1, byte key2)
        {
            this.mapping[0] = key1;
            this.mapping[1] = key2;
        }

        internal void SetControlMapping(byte key1, byte key2)
        {
            this.mapping[4] = key1;
            this.mapping[5] = key2;
        }

        internal void SetMod1Mapping(byte key1, byte key2)
        {
            this.mapping[6] = key1;
            this.mapping[7] = key2;
        }

        internal int GetModifierMask(XKeyEvent e, bool pressed)
        {
            var mask = 0;
            var modifierKeys = this.mapping;
            var modifierPressed = pressed || (e.KeyCode != modifierKeys[0] && e.KeyCode != modifierKeys[1]);
            if (e.IsShiftPressed && modifierPressed)
            {
                mask |= 1; // Shift.
            }

            modifierPressed = pressed || (e.KeyCode != modifierKeys[4] && e.KeyCode != modifierKeys[5]);
            if (e.IsControlPressed && modifierPressed)
            {
                mask |= 4; //Ctrl
            }

            modifierPressed = pressed || (e.KeyCode != modifierKeys[6] && e.KeyCode != modifierKeys[7]);
            if (e.IsAltPressed && modifierPressed)
            {
                mask |= 8; // Mod1.
            }

            return mask;
        }
    }
}