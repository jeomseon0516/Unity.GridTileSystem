using System;
using System.Collections;
using System.Collections.Generic;

namespace Jeomseon.Unity.GridTileSystem.Surface.Core
{
    /// <summary>원소를 저장하지 않고 index 접근 시 계산하는 고정 길이 읽기 전용 목록입니다.</summary>
    internal sealed class ComputedReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly Func<int, T> _valueFactory;

        /// <summary>계산할 원소 수를 가져옵니다.</summary>
        public int Count { get; }
        /// <summary>지정한 index의 원소를 계산해 반환합니다.</summary>
        public T this[int index] => (uint)index < (uint)Count
            ? _valueFactory(index)
            : throw new ArgumentOutOfRangeException(nameof(index));

        /// <summary>고정 길이와 index별 값 계산 함수를 결합합니다.</summary>
        public ComputedReadOnlyList(int count, Func<int, T> valueFactory)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            _valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        }

        /// <summary>index 순서로 계산되는 열거자를 반환합니다.</summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++) yield return _valueFactory(i);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
