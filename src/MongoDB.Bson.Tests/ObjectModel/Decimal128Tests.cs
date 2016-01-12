using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson.ObjectModel;
using NUnit.Framework;
using FluentAssertions;
using System.Numerics;
using System.Globalization;

namespace MongoDB.Bson.Tests.ObjectModel
{
    public class Decimal128Tests
    {
        [Test]
        public void Default_value()
        {
            var subject = default(Decimal128);

            subject.ToString().Should().Be("0");
            AssertSpecialProperties(subject);
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void GetParts(TestCase tc)
        {
            var subject = new Decimal128(tc.Parts);

            var parts = subject.GetParts();

            parts[0].Should().Be(tc.Parts[0]);
            parts[1].Should().Be(tc.Parts[1]);
            parts[2].Should().Be(tc.Parts[2]);
            parts[3].Should().Be(tc.Parts[3]);
        }

        [Test]
        [TestCaseSource("TestCases")]
        public void ToString(TestCase tc)
        {
            var subject = new Decimal128(tc.Parts);

            subject.ToString().Should().Be(tc.String);
        }

        [Test]
        public void Construction_with_an_integer()
        {
            var subject = new Decimal128(-1);

            subject.ToString().Should().Be("-1");
            AssertSpecialProperties(subject);
        }

        [Test]
        public void NegativeInfinity()
        {
            var subject = Decimal128.NegativeInfinity;

            subject.ToString().Should().Be("-Infinity");
            AssertSpecialProperties(subject, negInfinity: true);
        }

        [Test]
        public void PositiveInfinity()
        {
            var subject = Decimal128.PositiveInfinity;

            subject.ToString().Should().Be("Infinity");
            AssertSpecialProperties(subject, posInfinity: true);
        }

        [Test]
        public void QNaN()
        {
            var subject = Decimal128.QNaN;

            subject.ToString().Should().Be("NaN");
            AssertSpecialProperties(subject, qNaN: true);
        }

        [Test]
        public void SNaN()
        {
            var subject = Decimal128.SNaN;

            subject.ToString().Should().Be("NaN");
            AssertSpecialProperties(subject, sNaN: true);
        }

        private void AssertSpecialProperties(Decimal128 subject, bool qNaN = false, bool sNaN = false, bool posInfinity = false, bool negInfinity = false)
        {
            subject.IsNaN.Should().Be(qNaN || sNaN);
            subject.IsQNaN.Should().Be(qNaN);
            subject.IsSNaN.Should().Be(sNaN);
            subject.IsInfinity.Should().Be(posInfinity || negInfinity);
            subject.IsNegativeInfinity.Should().Be(negInfinity);
            subject.IsPositiveInfinity.Should().Be(posInfinity);
        }

        public class TestCase
        {
            public uint[] Parts;
            public string String;

            public TestCase(uint high, uint highMid, uint lowMid, uint low, string s)
            {
                Parts = new uint[] { high, highMid, lowMid, low };
                String = s;
            }
        }

        private IEnumerable<TestCase> TestCases
        {
            get
            {
                yield return new TestCase(0x78000000, 0, 0, 0, "Infinity");
                yield return new TestCase(0xF8000000, 0, 0, 0, "-Infinity");
                yield return new TestCase(0x7C000000, 0, 0, 0, "NaN");
                yield return new TestCase(0xFC000000, 0, 0, 0, "NaN");
                yield return new TestCase(0x7E000000, 0, 0, 0, "NaN");
                yield return new TestCase(0xFE000000, 0, 0, 0, "NaN");
                yield return new TestCase(0x7C000000, 0, 0, 0, "NaN");
                yield return new TestCase(0x7C000000, 0, 0, 12, "NaN");
                yield return new TestCase(0x30400000, 0, 0, 1, "1");
                yield return new TestCase(0x30400000, 0, 0, 0, "0");
                yield return new TestCase(0x30400000, 0, 0, 2, "2");
                yield return new TestCase(0xB0400000, 0, 0, 1, "-1");
                yield return new TestCase(0xB0400000, 0, 0, 0, "-0");
                yield return new TestCase(0x303e0000, 0, 0, 1, "0.1");
                yield return new TestCase(0x30340000, 0, 0, 0x4D2, "0.001234");
                yield return new TestCase(0x30400000, 0, 0x1C, 0xBE991A14, "123456789012");
                yield return new TestCase(0x302A0000, 0, 0, 0x75AEF40, "0.00123400000");
                yield return new TestCase(0x2FFC3CDE, 0x6FFF9732, 0xDE825CD0, 0x7E96AFF2, "0.1234567890123456789012345678901234");
            }
        }
    }
}