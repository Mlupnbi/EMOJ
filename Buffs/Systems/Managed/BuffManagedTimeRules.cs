using System;
using Terraria;
using Terraria.ID;

namespace EvenMoreOverpoweredJourney.Buffs.Systems.Managed
{
    /// <summary>
    /// ���� BuffsPlus����ģ���йܵ��������� <see cref="BuffID.Sets.TimeLeftDoesNotDecrease"/>��������λʱ��˥����
    /// </summary>
    public static class BuffManagedTimeRules
    {
        private static bool[] _originalTimeLeftDoesNotDecrease;
        private static bool[] _timeLeftOverridden;
        private static bool[] _originalBuffNoTimeDisplay;
        private static bool[] _buffNoTimeDisplayOverridden;

        public static void RestoreAll()
        {
            if (_timeLeftOverridden != null && _originalTimeLeftDoesNotDecrease != null)
            {
                int limit = Math.Min(
                    Math.Min(_timeLeftOverridden.Length, _originalTimeLeftDoesNotDecrease.Length),
                    BuffID.Sets.TimeLeftDoesNotDecrease.Length);
                for (int buffType = 1; buffType < limit; buffType++)
                {
                    if (_timeLeftOverridden[buffType])
                        BuffID.Sets.TimeLeftDoesNotDecrease[buffType] = _originalTimeLeftDoesNotDecrease[buffType];
                }
            }

            if (_buffNoTimeDisplayOverridden != null && _originalBuffNoTimeDisplay != null && Main.buffNoTimeDisplay != null)
            {
                int limit = Math.Min(
                    Math.Min(_buffNoTimeDisplayOverridden.Length, _originalBuffNoTimeDisplay.Length),
                    Main.buffNoTimeDisplay.Length);
                for (int buffType = 1; buffType < limit; buffType++)
                {
                    if (_buffNoTimeDisplayOverridden[buffType])
                        Main.buffNoTimeDisplay[buffType] = _originalBuffNoTimeDisplay[buffType];
                }
            }

            _originalTimeLeftDoesNotDecrease = null;
            _timeLeftOverridden = null;
            _originalBuffNoTimeDisplay = null;
            _buffNoTimeDisplayOverridden = null;
        }

        public static void SetEnabled(int buffType, bool enabled)
        {
            if (buffType <= 0 ||
                buffType >= BuffID.Sets.TimeLeftDoesNotDecrease.Length ||
                buffType >= Main.buffNoTimeDisplay.Length)
                return;

            EnsureArrays();

            if (enabled)
            {
                if (!_timeLeftOverridden[buffType])
                {
                    _originalTimeLeftDoesNotDecrease[buffType] = BuffID.Sets.TimeLeftDoesNotDecrease[buffType];
                    _timeLeftOverridden[buffType] = true;
                }

                BuffID.Sets.TimeLeftDoesNotDecrease[buffType] = true;

                if (!_buffNoTimeDisplayOverridden[buffType])
                {
                    _originalBuffNoTimeDisplay[buffType] = Main.buffNoTimeDisplay[buffType];
                    _buffNoTimeDisplayOverridden[buffType] = true;
                }

                Main.buffNoTimeDisplay[buffType] = true;
                return;
            }

            if (_timeLeftOverridden[buffType])
            {
                BuffID.Sets.TimeLeftDoesNotDecrease[buffType] = _originalTimeLeftDoesNotDecrease[buffType];
                _timeLeftOverridden[buffType] = false;
                _originalTimeLeftDoesNotDecrease[buffType] = false;
            }

            if (_buffNoTimeDisplayOverridden[buffType])
            {
                Main.buffNoTimeDisplay[buffType] = _originalBuffNoTimeDisplay[buffType];
                _buffNoTimeDisplayOverridden[buffType] = false;
                _originalBuffNoTimeDisplay[buffType] = false;
            }
        }

        private static void EnsureArrays()
        {
            int length = Math.Min(BuffID.Sets.TimeLeftDoesNotDecrease.Length, Main.buffNoTimeDisplay.Length);
            if (_timeLeftOverridden == null || _timeLeftOverridden.Length != length)
                Array.Resize(ref _timeLeftOverridden, length);
            if (_originalTimeLeftDoesNotDecrease == null || _originalTimeLeftDoesNotDecrease.Length != length)
                Array.Resize(ref _originalTimeLeftDoesNotDecrease, length);
            if (_buffNoTimeDisplayOverridden == null || _buffNoTimeDisplayOverridden.Length != length)
                Array.Resize(ref _buffNoTimeDisplayOverridden, length);
            if (_originalBuffNoTimeDisplay == null || _originalBuffNoTimeDisplay.Length != length)
                Array.Resize(ref _originalBuffNoTimeDisplay, length);
        }
    }
}
