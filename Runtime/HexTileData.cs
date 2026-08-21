using System.Collections.Generic;
using Jeomseon.Unity.Attributes;
using UnityEngine;

namespace Jeomseon.Unity.GridTileSystem
{
    /// <summary>UnityEvent나 Renderer 참조 없이 저장·복제·동기화할 수 있는 순수 Hex Tile 데이터입니다.</summary>
    [System.Serializable]
    public sealed class HexTileData
    {
        /// <summary>사용자가 붙인 문자열 속성 목록입니다.</summary>
        [SerializeField] private List<string> properties = new();
        /// <summary>타일의 불변 논리 Cube 좌표입니다.</summary>
        [SerializeField, ReadOnly] private HexCoordinates coordinates;
        /// <summary>Surface Region 중심의 표시용 월드 위치입니다.</summary>
        [SerializeField, ReadOnly] private Vector3 tilePosition;
        /// <summary>intrinsic chart의 타일 중심 좌표입니다.</summary>
        [SerializeField, ReadOnly] private Vector2 intrinsicPosition;
        /// <summary>Grid ring 순회 규칙으로 계산한 직렬 인덱스입니다.</summary>
        [SerializeField, ReadOnly] private int index;
        /// <summary>선택과 시각화에 사용할 활성 상태입니다.</summary>
        [SerializeField] private bool isActive = true;
        /// <summary>타일 시각화 색상입니다.</summary>
        [SerializeField] private Color color = Color.cyan;

        /// <summary>사용자 문자열 속성의 읽기 전용 목록을 가져옵니다.</summary>
        public IReadOnlyList<string> Properties => properties;
        /// <summary>타일의 논리 Cube 좌표를 가져옵니다.</summary>
        public HexCoordinates Coordinates => coordinates;
        /// <summary>표시용 월드 위치를 가져옵니다.</summary>
        public Vector3 TilePosition => tilePosition;
        /// <summary>intrinsic chart 중심 좌표를 가져옵니다.</summary>
        public Vector2 IntrinsicPosition => intrinsicPosition;
        /// <summary>Grid ring 순회 기반 직렬 인덱스를 가져옵니다.</summary>
        public int Index => index;
        /// <summary>활성 상태를 가져오거나 설정합니다.</summary>
        public bool IsActive { get => isActive; set => isActive = value; }
        /// <summary>시각화 색상을 가져오거나 설정합니다.</summary>
        public Color Color { get => color; set => color = value; }

        /// <summary>모든 위치와 직렬 인덱스를 포함한 타일 데이터를 생성합니다.</summary>
        public HexTileData(int q, int r, in Vector3 tilePosition, in Vector2 intrinsicPosition, int gridRadius)
        {
            coordinates = new HexCoordinates(q, r);
            this.tilePosition = tilePosition;
            this.intrinsicPosition = intrinsicPosition;
            index = CalculateIndex(coordinates, gridRadius);
        }

        /// <summary>Axial 좌표만 포함한 기본 타일 데이터를 생성합니다.</summary>
        public HexTileData(in AxialCoordinates coordinates)
            : this(coordinates.Q, coordinates.R, Vector3.zero, Vector2.zero, 0) { }

        /// <summary>사용자 문자열 속성을 추가합니다. 중복 값도 보존합니다.</summary>
        public void AddProperty(string property) => properties.Add(property);
        /// <summary>같은 값을 가진 문자열 속성을 모두 제거하고 제거 여부를 반환합니다.</summary>
        public bool RemoveProperty(string property) => properties.RemoveAll(value => value == property) > 0;
        /// <summary>표시용 월드 위치를 갱신합니다. 논리 identity에는 영향을 주지 않습니다.</summary>
        public void SetTilePosition(in Vector3 position) => tilePosition = position;

        /// <summary>같은 좌표의 이전 데이터에서 사용자가 수정할 수 있는 상태만 복사합니다.</summary>
        internal void CopyUserStateFrom(HexTileData source)
        {
            properties.Clear();
            properties.AddRange(source.properties);
            isActive = source.isActive;
            color = source.color;
        }

        /// <summary>반경 R인 Hex 영역의 q-major 열 순회에서 좌표의 0 기반 인덱스를 계산합니다.</summary>
        public static int CalculateIndex(in HexCoordinates coordinates, int gridRadius)
        {
            int q = coordinates.Q;
            int r = coordinates.R;
            // Cube 제약 |q|, |r|, |s| <= R을 동시에 만족하는 현재 q 열의 첫 r입니다.
            int rMin = Mathf.Max(-gridRadius, -q - gridRadius);
            int index = (2 * gridRadius + 1) * (q + gridRadius);
            // 직사각형 열 누적값에서 육각 경계 밖의 위·아래 삼각형 셀 수를 차례로 제거합니다.
            for (int previousQ = -gridRadius; previousQ < q; previousQ++) index -= Mathf.Abs(previousQ);
            return index + r - rMin;
        }
    }
}
