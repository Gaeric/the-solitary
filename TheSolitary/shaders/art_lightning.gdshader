shader_type canvas_item;

uniform vec4 tint : source_color = vec4(1.0);

void fragment() {
	vec4 sample_color = texture(TEXTURE, UV);
	// 先消除原闪电的黄色，避免直接 modulate 导致蓝/紫色发暗或偏色。
	float intensity = max(sample_color.r, max(sample_color.g, sample_color.b));
	COLOR = vec4(tint.rgb * intensity, sample_color.a * tint.a);
}