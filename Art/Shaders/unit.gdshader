shader_type canvas_item;

// Hit flash: 0 = normal, 1 = solid flash_color.
uniform float flash : hint_range(0.0, 1.0) = 0.0;
uniform vec4 flash_color : source_color = vec4(1.0);
// Outline around the sprite (alpha 0 = no outline). Used for selection / targeting.
uniform vec4 outline_color : source_color = vec4(0.0);

// Region lighting (all off by default, so the shader looks unchanged outside battle).
// ambient: multiplies the sprite (cool shadows); rim: light catching the edges that face the torches.
uniform vec4 ambient : source_color = vec4(1.0);
uniform vec4 rim_color : source_color = vec4(1.0, 0.6, 0.3, 1.0);
uniform float rim_strength : hint_range(0.0, 1.0) = 0.0;
// -1 = the light comes from the left of the texture, +1 = from the right.
uniform float rim_side = -1.0;
// Darkens the feet a little, so units sit in the floor's shadow.
uniform float foot_shade : hint_range(0.0, 1.0) = 0.0;

float alpha_at(sampler2D tex, vec2 uv) {
	if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0.0;
	return texture(tex, uv).a;
}

void fragment() {
	vec4 c = COLOR;
	vec2 px = TEXTURE_PIXEL_SIZE;
	if (c.a < 0.1 && outline_color.a > 0.0) {
		float n = alpha_at(TEXTURE, UV + vec2(px.x, 0.0))
			+ alpha_at(TEXTURE, UV - vec2(px.x, 0.0))
			+ alpha_at(TEXTURE, UV + vec2(0.0, px.y))
			+ alpha_at(TEXTURE, UV - vec2(0.0, px.y));
		if (n > 0.0) {
			c = outline_color;
		}
	} else if (c.a >= 0.1) {
		c.rgb *= ambient.rgb;
		if (rim_strength > 0.0) {
			// Edge pixels facing the light (and the top edges) catch it; the next pixel in catches half.
			float side1 = 1.0 - step(0.1, alpha_at(TEXTURE, UV + vec2(rim_side * px.x, 0.0)));
			float side2 = 1.0 - step(0.1, alpha_at(TEXTURE, UV + vec2(rim_side * 2.0 * px.x, 0.0)));
			float top1 = 1.0 - step(0.1, alpha_at(TEXTURE, UV - vec2(0.0, px.y)));
			float rim = max(max(side1, side2 * 0.5), top1 * 0.6);
			c.rgb = mix(c.rgb, rim_color.rgb, rim * rim_strength);
		}
		c.rgb *= 1.0 - foot_shade * clamp((UV.y - 0.75) / 0.25, 0.0, 1.0);
	}
	c.rgb = mix(c.rgb, flash_color.rgb, flash * c.a);
	COLOR = c;
}
