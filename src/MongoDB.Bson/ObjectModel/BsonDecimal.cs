using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MongoDB.Bson.ObjectModel
{
    internal class BsonDecimal : BsonValue
    {
        private readonly uint[] _parts;

        /// <summary>
        /// Initializes a new instance of the <see cref="BsonDecimal"/> class.
        /// </summary>
        /// <param name="parts">The parts.</param>
        public BsonDecimal(uint[] parts)
        {
            if (parts == null)
            {
                throw new ArgumentNullException("parts");
            }
            if (parts.Length != 4)
            {
                throw new ArgumentException("Must be of length 4.", "parts");
            }

            _parts = parts.ToArray(); // copy these out...
        }

        /// <inheritdoc />
        public override BsonType BsonType => BsonType.Decimal;

        public override int CompareTo(BsonValue other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            // high bit is 1
            if (_parts[0] >> 31 == 0x1)
            {
                result.Append('-');
            }

            // combination will be the low 5 bits
            var combination = (_parts[0] >> 26) & 0x1F;

            uint biasedExponent;
            uint significandMsb;
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

                biasedExponent = (_parts[0] >> 15) & 0x3FFF;
                significandMsb = 0x8 + ((_parts[0] >> 14) & 0x1);
            }
            else
            {
                biasedExponent = (_parts[0] >> 17) & 0x3FFF;
                significandMsb = (_parts[0] >> 14) & 0x7;
            }

            var exponent = (int)biasedExponent - 6176; // exponent bias

            var sigHigh = (_parts[0] & 0x3FFF) + ((significandMsb & 0xF) << 14);
            var sigHighMid = _parts[1];
            var sigLowMid = _parts[2];
            var sigLow = _parts[3];

            var significand = new uint[36]; // this takes a lot of memory
            bool isZero = false;
            if (sigHigh == 0 && sigHighMid == 0 && sigLowMid == 0 && sigLow == 0)
            {
                isZero = true;
            }
            else
            {
                for (int k = 3; k >= 0; k--)
                {
                    uint remainder;
                    DivideByOneBillion(ref sigHigh, ref sigHighMid, ref sigLowMid, ref sigLow, out remainder);

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

            var scientificExponent = significandDigits - 1 + exponent;

            if (scientificExponent >= 12
                || scientificExponent <= -4
                || exponent > 0
                || (isZero && scientificExponent != 0))
            {
                result.Append(significand[significandRead++]);
                significandDigits--;

                if (significandDigits != 0)
                {
                    result.Append('.');
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
                if (exponent >= 0)
                {
                    for (int i = 0; i < significandDigits; i++)
                    {
                        result.Append(significand[significandRead++]);
                    }
                }
                else
                {
                    int radixPosition = significandDigits + exponent;

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

                    result.Append('.');

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
