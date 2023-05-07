using System;
using System.Security.Cryptography;

namespace AuthorizeNet
{
    /// <summary>
    /// Source Code from MSDN article http://msdn.microsoft.com/en-us/magazine/cc163367.aspx
    /// Updated to use newer random number classes
    /// </summary>
    public class CryptoRandom
    {
        readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
        readonly byte[] _uint32Buffer = new byte[4];

        public CryptoRandom() { }
        public CryptoRandom(int ignoredSeed) { }

        public int Next()
        {
            _rng.GetBytes(_uint32Buffer);
            return BitConverter.ToInt32(_uint32Buffer, 0) & 0x7FFFFFFF;
        }

        public int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            return Next(0, maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));
            if (minValue == maxValue) return minValue;
            long diff = maxValue - minValue;
            while (true)
            {
                _rng.GetBytes(_uint32Buffer);
                uint rand = BitConverter.ToUInt32(_uint32Buffer, 0);

                long max = (1 + (long)uint.MaxValue);
                long remainder = max % diff;
                if (rand < max - remainder)
                {
                    return (int)(minValue + (rand % diff));
                }
            }
        }

        public double NextDouble()
        {
            _rng.GetBytes(_uint32Buffer);
            uint rand = BitConverter.ToUInt32(_uint32Buffer, 0);
            return rand / (1.0 + uint.MaxValue);
        }

        public void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            _rng.GetBytes(buffer);
        }
    }
}
