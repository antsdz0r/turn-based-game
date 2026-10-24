shader_type canvas_item;

// Battle background colour grade: shadows lean toward a region tint, highlights warm up, a touch more contrast.
uniform vec3 shadow_tint = vec3(0.05, 0.0, 0.12);
uniform vec3 highlight_tint = vec3(0.10, 0.04, -0.02);
uniform float contrast : hint_range(0.5, 1.5) = 1.12;

void fragment() {
	vec4 c = texture(TEXTURE, UV) * COLOR;
	float lum = dot(c.rgb, vec3(0.299, 0.587, 0.114));
	c.rgb = (c.rgb - 0.5) * contrast + 0.5;
	c.rgb += (1.0 - lum) * shadow_tint * 0.6 + lum * lum * lum * highlight_tint;
	COLOR = vec4(clamp(c.rgb, 0.0, 1.0), c.a);
}
