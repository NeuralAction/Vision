using System;
using static System.Diagnostics.Debug;

// Convenience typedefs
using uint64 = System.UInt64;
using uint32 = System.UInt32;
using SeedBuffer = Vision.RNG_XORShift.Xorshift128PlusKey;

namespace Vision
{
    public class Random
    {
        private SeedBuffer _seedBuffer;

        public Random()
        {
            _seedBuffer = new SeedBuffer
            {
                s1 = RNG_Seeder.GenTime32(),
                s2 = RNG_Seeder.GenTime32()
            };
        }

        public uint64 NextUInt64()
        {
            return RNG_XORShift.Xorshift128Plus(ref _seedBuffer);
        }

        public uint32 NextUInt32(uint32 range)
        {
            // [ 0; range [
            return (uint32)(RNG_XORShift.Xorshift128Plus(ref _seedBuffer) % (range));
        }

        public int NextInt(int lower, int upper)
        {
            // [ lower; upper [
            Assert(lower < upper, "lower range cannot be bigger then upper range!");

            return (int)((uint32)(RNG_XORShift.Xorshift128Plus(ref _seedBuffer))
                % (uint32)(Math.Abs(upper - lower))
                + lower);
        }

        public double NextDouble(double lower, double upper)
        {
            return (double)NextInt((int)(lower * 1500), (int)(upper * 1500)) / 1500;
        }

        public void ReSeed()
        {
            RNG_XORShift.XorshiftPlusJump(ref _seedBuffer);
        }

        // Maintaining API compatibility
        public static Random R { get; private set; } = new Random();
    }

    // ---------------------------------------------------------------------------------------------------


    internal static class RNG_Seeder
    {
        public static uint64 GenTime64() { return (uint64)System.DateTime.UtcNow.Ticks & 0xFFFFFFFF; }
        public static uint32 GenTime32() { return (uint32)System.DateTime.UtcNow.Ticks & 0xFFFFFFFF; }
    }

    // ---------------------------------------------------------------------------------------------------

    // Implementation of xorshiftplus RNG. See https://en.wikipedia.org/wiki/Xorshift.
    // Based on spiky::random::xorshift128plus's implementation :P https://pastebin.com/xu3KA3jS
    internal static class RNG_XORShift
    {
        // Keys for scalar xorshift128. Must be non-zero.
        // These are modified by xorshift128plus.
        public struct Xorshift128PlusKey
        {
            public uint64 s1;
            public uint64 s2;
        };

        // Returns a new 64-bit random number.
        public static uint64 Xorshift128Plus(ref Xorshift128PlusKey nextKey)
        {
            uint64 s1 = nextKey.s1;
            uint64 s0 = nextKey.s2;
            nextKey.s1 = s0;
            s1 ^= s1 << 23; // a
            nextKey.s2 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5); // b, c
            return nextKey.s2 + s0;
        }

        static readonly uint64[] JumpTable = new uint64[] { 0x8a5cd789635d2dff, 0x121fd2155c472f96 };

        // Equivalent to skipping 2^64 xorshift128plus() calls
        // useful to generate a new key from an existing one (multi-threaded context).
        public static void XorshiftPlusJump(ref Xorshift128PlusKey key)
        {
            uint64 s0 = 0;
            uint64 s1 = 0;
            for (uint32 i = 0; i < JumpTable.Length; i++)
            {
                for (int b = 0; b < 64; b++)
                {
                    if ((JumpTable[i] & 1UL) << b != 0)
                    {
                        s0 ^= key.s1;
                        s1 ^= key.s2;
                    }

                    Xorshift128Plus(ref key);
                }
            }

            key.s1 = s0;
            key.s2 = s1;
        }

        private static void Xorshift128Plus_bounded_two_by_two(
            ref Xorshift128PlusKey key,
            uint32 bound1, uint32 bound2,
            ref uint32 bounded1, ref uint32 bounded2
        )
        {
            uint64 rand = Xorshift128Plus(ref key);
            bounded1 = (uint32)((((rand & 0xFFFFFFFF) * bound1)) >> 32);
            bounded2 = (uint32)(((rand >> 32) * bound2) >> 32);
        }

        static uint32 Xorshift128Plus_bounded(ref Xorshift128PlusKey key, uint32 bound)
        {
            uint64 rand = Xorshift128Plus(ref key);
            return (uint32)(((rand & (0xFFFFFFFF)) * bound) >> 32);
        }

        // Fisher-Yates shuffle, shuffling an array of 32-bit values, use the provided key.
        public static void Xorshift128plusShuffle32(ref Xorshift128PlusKey key, uint32[] storage)
        {
            uint32 i = 0;
            uint32 nextpos1 = 0, nextpos2 = 0;

            for (i = (uint32)storage.Length; i > 2; i -= 2)
            {
                Xorshift128Plus_bounded_two_by_two(ref key, i, i - 1, ref nextpos1, ref nextpos2);

                uint32 tmp1 = storage[i - 1];       // Probably in cache
                uint32 val1 = storage[nextpos1];
                storage[i - 1] = val1;
                storage[nextpos1] = tmp1;
                uint32 tmp2 = storage[i - 2];       // Probably in cache
                uint32 val2 = storage[nextpos2];
                storage[i - 2] = val2;
                storage[nextpos2] = tmp2;
            }
            if (i > 1)
            {
                uint32 nextpos = Xorshift128Plus_bounded(ref key, i);
                uint32 tmp = storage[i - 1];        // Probably in cache
                uint32 val = storage[nextpos];
                storage[i - 1] = val;
                storage[nextpos] = tmp;
            }
        }
    }
}
