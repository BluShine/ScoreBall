Shader "Unlit/Color Transparent"
{
	Properties
	{
		_Color ("Color", Color) = (1.0, 1.0, 1.0, 1.0)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
		Blend SrcAlpha OneMinusSrcAlpha
		Color [_Color]
		LOD 0

		Pass {}
	}
}
