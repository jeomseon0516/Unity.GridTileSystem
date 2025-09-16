
struct HexOption
{
	float3 Color;
	int IsActive;
};

StructuredBuffer<HexOption> _HexOptions;
int _BufferOn = 0;

void CheckHexOptionBuffer_float(out bool bufferOn)
{
	bufferOn = _BufferOn;
}

void GetHexOption_float(const float2 p, const float limit, out float4 option)
{
	const int q = p.x;
	const int r = p.y;

	const int limitInt = limit;
	const int rMin = max(-limitInt, -q - limitInt);
	
	int index = (2 * limitInt + 1) * (q + limit);
	for (int i = -limitInt; i < q; i++)
	{
		index -= abs(i);
	}

	index += r - rMin;
	HexOption hexOption = _HexOptions[index];
	option = float4(hexOption.Color.x, hexOption.Color.y, hexOption.Color.z, hexOption.IsActive);
}

void DistanceToSegment_float(const float2 p, const float2 a, const float2 b, out float distance)
{
	const float2 ab = b - a;
	const float2 ap = p - a;

	const float abSquared = dot(ab, ab);

	float2 closestPoint;
	if (abSquared == 0.0f)
	{
		closestPoint = a;
	}
	else
	{
		const float t = saturate(dot(ap, ab) / abSquared);
		closestPoint = a + t * ab;
	}

	distance = length(p - closestPoint);
}

void CalculateAlpha_float(
	const float2 p,
	const float2 v0, const float2 v1, const float2 v2,
	const float2 v3, const float2 v4, const float2 v5,
	const float lineWidth,
	out float alpha)
{
	float distances[6];
	DistanceToSegment_float(p, v0, v1, distances[0]);
	DistanceToSegment_float(p, v1, v2, distances[1]);
	DistanceToSegment_float(p, v2, v3, distances[2]);
	DistanceToSegment_float(p, v3, v4, distances[3]);
	DistanceToSegment_float(p, v4, v5, distances[4]);
	DistanceToSegment_float(p, v5, v0, distances[5]);
    
	float minDistance = distances[0];
	for (int i = 1; i < 6; i++)
	{
		minDistance = min(minDistance, distances[i]);
	}

	alpha = saturate(1.0f - minDistance / lineWidth);
}

void CalculateAlpha_float(const float2 center, const float2 p, const float2 vertices[6], const float lineWidthRatio, out float alpha)
{
	float distances[6];
	DistanceToSegment_float(p, vertices[0], vertices[1], distances[0]);
	float minDistance = distances[0];
	for (int i = 1; i < 6; i++)
	{
		DistanceToSegment_float(p, vertices[i], vertices[(i + 1) % 6u], distances[i]);
		minDistance = min(minDistance, distances[i]);
	}

	alpha = saturate(1.0f - minDistance / lineWidthRatio);
}

void GetHexCorner_float(const float2 center, const float radius, const int index, out float2 corner)
{
	const float pi = 3.14159265359f;
	const float angleDeg = 60.0f * index;
	const float angleRad = pi / 180.0f * angleDeg;

	corner = float2(
		center.x + radius * cos(angleRad),
		center.y + radius * sin(angleRad));
}

void CalculateHexOutline_float(const float2 hexPixel, const float2 nowPixel, const float radius, const float lineWidth, out float alpha)
{
	float2 vertices[6];

	for (int i = 0; i < 6; i++)
	{
		GetHexCorner_float(hexPixel, radius, i, vertices[i]);
	}
	
	CalculateAlpha_float(hexPixel, nowPixel, vertices, lineWidth, alpha);
}