using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Jeomseon.Unity.GridTileSystem;

namespace Jeomseon.HexGrid.Tests
{
    public sealed class HexGridIndexTests
    {
        [Test]
        public void GetHexIndex_ForFullHexagonalGrid_ProducesContiguousUniqueIndices(
            [Values(0, 1, 2, 3, 5)] int limit)
        {
            var indices = new HashSet<int>();
            int count = 0;

            for (int q = -limit; q <= limit; q++)
            {
                int rMin = Mathf.Max(-limit, -q - limit);
                int rMax = Mathf.Min(limit, -q + limit);
                for (int r = rMin; r <= rMax; r++)
                {
                    global::Jeomseon.Unity.GridTileSystem.HexGrid hex = new(q, r);
                    int index = hex.GetHexIndex(limit);

                    Assert.That(indices.Add(index), Is.True, $"Duplicate index {index} at ({q},{r})");
                    count++;
                }
            }

            Assert.That(indices, Is.EquivalentTo(Enumerable.Range(0, count)));
        }
    }
}
