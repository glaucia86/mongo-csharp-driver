using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.ObjectModel
{
    /// <summary>
    /// Represents a Decimal128 value.
    /// </summary>
    [Serializable]
    internal struct Decimal128
    {
        private const int __exponentBias = 6176;

        public static Decimal128 NegativeInfinity =>
            new Decimal128(new uint[] { 0xF8000000, 0, 0, 0 });

        public static Decimal128 PositiveInfinity =>
            new Decimal128(new uint[] { 0x78000000, 0, 0, 0 });

        public static Decimal128 QNaN =>
            new Decimal128(new uint[] { 0x7C000000, 0, 0, 0 });

        public static Decimal128 SNaN =>
            new Decimal128(new uint[] { 0x7E000000, 0, 0, 0 });

        private readonly byte _flags;
        private readonly int _exponent;

        // below only represent the significand
        private readonly uint _high;
        private readonly uint _highMid;
        private readonly uint _lowMid;
        private readonly uint _low;

        public Decimal128(int value)
        {
            _flags = 0;
            if (value < 0)
            {
                // sign bit
                _flags = 0x80;
                value = -value;
            }

            _exponent = 0;
            _high = 0;
            _highMid = 0;
            _lowMid = 0;
            _low = (uint)value;
        }

        public Decimal128(uint[] parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException("parts");
            }
            if (parts.Length != 4)
            {
                throw new ArgumentException("Must be of length 4.", "parts");
            }

            _flags = (byte)(parts[0] >> 24);

            // combination will be the low 5 bits
            var combination = (_flags >> 2) & 0x1F;
            uint biasedExponent;
            uint significandMsb;
            // 2 high combination bits are set
            if ((combination >> 3) == 0x3)
            {
                biasedExponent = (parts[0] >> 15) & 0x3FFF;
                significandMsb = 0x8 + ((parts[0] >> 14) & 0x1);
            }
            else
            {
                biasedExponent = (parts[0] >> 17) & 0x3FFF;
                significandMsb = (parts[0] >> 14) & 0x7;
            }

            _exponent = (int)biasedExponent - __exponentBias;

            _high = (parts[0] & 0x3FFF) + ((significandMsb & 0xF) << 14);
            _highMid = parts[1];
            _lowMid = parts[2];
            _low = parts[3];
        }

        public bool IsInfinity => IsPositiveInfinity || IsNegativeInfinity;

        public bool IsNegative => (_flags & 0x80) != 0;

        public bool IsNegativeInfinity => (_flags & 0xF8) == 0xF8;

        public bool IsPositiveInfinity => (_flags & 0x78) == 0x78 && (_flags & 0x80) == 0 && (_flags & 0x7C) != 0x7C;

        public bool IsNaN => IsQNaN || IsSNaN;

        public bool IsQNaN => (_flags & 0x7C) == 0x7C && (_flags & 0x7E) != 0x7E;

        public bool IsSNaN => (_flags & 0x7E) == 0x7E;

        public uint[] GetParts()
        {
            var parts = new uint[4];
            parts[0] = (uint)_flags << 24;

            // combination will be the low 5 bits
            var combination = (_flags >> 2) & 0x1F;
            uint biasedExponent = (uint)(_exponent + __exponentBias);
            uint significandMsb;
            // 2 high combination bits are set
            if ((combination >> 3) == 0x3)
            {
                parts[0] |= (biasedExponent << 15);
                //biasedExponent = (parts[0] >> 15) & 0x3FFF;



                significandMsb = 0x8 + ((parts[0] >> 14) & 0x1);
            }
            else
            {
                parts[0] |= (biasedExponent << 17);
                //biasedExponent = (parts[0] >> 17) & 0x3FFF;
                significandMsb = (parts[0] >> 14) & 0x7;
            }

            //_high = (parts[0] & 0x3FFF) + ((significandMsb & 0xF) << 14);

            parts[0] |= _high;
            parts[1] = _highMid;
            parts[2] = _lowMid;
            parts[3] = _low;

            return parts;
        }

        public override string ToString()
        {
            return ToString(NumberFormatInfo.InvariantInfo);
        }

        private string ToString(NumberFormatInfo formatInfo)
        {
            var result = new StringBuilder();

            // high bit is 1
            if ((_flags & 0x80) != 0)
            {
                result.Append('-');
            }

            // combination will be the low 5 bits
            var combination = (_flags >> 2) & 0x1F;

            // 2 high combination bits are set
            if ((combination >> 3) == 0x3)
            {
                if (combination == 0x1E)
                {
                    result.Append("Infinity");
                    return result.ToString();
                }
                else if (combination == 0x1F)
                {
                    result.Clear(); // erase the sign
                    result.Append("NaN");
                    // TODO: should put an S in front when SNaN...
                    return result.ToString();
                }
            }

            var significand = new uint[36]; // this takes a lot of memory
            bool isZero = false;
            var high = _high;
            var highMid = _highMid;
            var lowMid = _lowMid;
            var low = _low;
            if (high == 0 && highMid == 0 && lowMid == 0 && low == 0)
            {
                isZero = true;
            }
            else
            {
                for (int k = 3; k >= 0; k--)
                {
                    uint remainder;
                    DivideByOneBillion(ref high, ref highMid, ref lowMid, ref low, out remainder);

                    // we now have the 9 least significant digits (in base 2).
                    if (remainder == 0)
                    {
                        break;
                    }

                    for (int j = 8; j >= 0; j--)
                    {
                        significand[k * 9 + j] = remainder % 10;
                        remainder /= 10;
                    }
                }
            }

            int significandDigits = 0;
            int significandRead = 0;
            if (isZero)
            {
                significandDigits = 1;
                significandRead = 0;
            }
            else
            {
                significandDigits = 36;
                while (significand[significandRead] == 0)
                {
                    significandDigits--;
                    significandRead++;
                }
            }

            var scientificExponent = significandDigits - 1 + _exponent;

            if (scientificExponent >= 12
                || scientificExponent <= -4
                || _exponent > 0
                || (isZero && scientificExponent != 0))
            {
                result.Append(significand[significandRead++]);
                significandDigits--;

                if (significandDigits != 0)
                {
                    result.Append(formatInfo.NumberDecimalSeparator);
                }

                for (int i = 0; i < significandDigits; i++)
                {
                    result.Append(significand[significandRead++]);
                }

                result.Append('E');
                result.Append(scientificExponent);
            }
            else
            {
                if (_exponent >= 0)
                {
                    for (int i = 0; i < significandDigits; i++)
                    {
                        result.Append(significand[significandRead++]);
                    }
                }
                else
                {
                    int radixPosition = significandDigits + _exponent;

                    if (radixPosition > 0) // non-zero digits before radix
                    {
                        for (int i = 0; i < radixPosition; i++)
                        {
                            result.Append(significand[significandRead++]);
                        }
                    }
                    else
                    {
                        result.Append('0');
                    }

                    result.Append(formatInfo.NumberDecimalSeparator);

                    while (radixPosition++ < 0)
                    {
                        result.Append('0');
                    }

                    for (int i = 0; i < significandDigits - (int)Math.Max(radixPosition - 1, 0); i++)
                    {
                        result.Append(significand[significandRead++]);
                    }
                }
            }

            return result.ToString();
        }

        private static void DivideByOneBillion(ref uint high, ref uint highMid, ref uint lowMid, ref uint low, out uint remainder)
        {
            const uint divisor = 1000 * 1000 * 1000;

            ulong tempRemainder = 0;

            if (high == 0 && highMid == 0 && lowMid == 0 && low == 0)
            {
                remainder = 0;
                return;
            }

            tempRemainder <<= 32;
            tempRemainder += high;
            high = (uint)(tempRemainder / divisor);
            tempRemainder %= divisor;

            tempRemainder <<= 32;
            tempRemainder += highMid;
            highMid = (uint)(tempRemainder / divisor);
            tempRemainder %= divisor;

            tempRemainder <<= 32;
            tempRemainder += lowMid;
            lowMid = (uint)(tempRemainder / divisor);
            tempRemainder %= divisor;

            tempRemainder <<= 32;
            tempRemainder += low;
            low = (uint)(tempRemainder / divisor);
            tempRemainder %= divisor;

            remainder = (uint)tempRemainder;
        }
    }
}

