using System.Numerics;
using UnityEngine;

namespace Forge.Core {
    // File to handle damage calcs and other info

    public readonly struct DamageInfo {
        public readonly int Amount;
        public readonly UnityEngine.Vector2 SourcePosition;
        public readonly GameObject Source;
        public readonly bool Parryable;

        public DamageInfo(int amount, UnityEngine.Vector2 sourcePosition, GameObject source, bool parryable = false)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            Source = source;
            Parryable = parryable;
        }
    }

    // Anything that can be hit, just returns true if the hit does damage
    public interface IDamageable {
        bool TakeDamage(DamageInfo info);
    }

    // Anything whose attacks can be parried
    public interface IParryable {
        void OnParried();
    }
}