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
    public class BsonDecimalTests
    {
        [Test]
        [TestCase("78000000", "0", "0", "0", "Infinity")]
        [TestCase("F8000000", "0", "0", "0", "-Infinity")]
        [TestCase("7C000000", "0", "0", "0", "NaN")]
        [TestCase("FC000000", "0", "0", "0", "NaN")]
        [TestCase("7E000000", "0", "0", "0", "NaN")]
        [TestCase("FE000000", "0", "0", "0", "NaN")]
        [TestCase("7C000000", "0", "0", "C", "NaN")]
        [TestCase("30400000", "0", "0", "1", "1")]
        [TestCase("30400000", "0", "0", "0", "0")]
        [TestCase("30400000", "0", "0", "2", "2")]
        [TestCase("B0400000", "0", "0", "1", "-1")]
        [TestCase("B0400000", "0", "0", "0", "-0")]
        public void ToString(string high, string highMid, string lowMid, string low, string expected)
        {
            var subject = new BsonDecimal(new uint[]
            {
                uint.Parse(high, NumberStyles.HexNumber),
                uint.Parse(highMid, NumberStyles.HexNumber),
                uint.Parse(lowMid, NumberStyles.HexNumber),
                uint.Parse(low, NumberStyles.HexNumber)
            });

            subject.ToString().Should().Be(expected);
        }
    }
}