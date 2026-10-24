shader_type canvas_item;

// Hit flash: 0 = normal, 1 = solid flash_color.
uniform float flash : hint_range(0.0, 1.0) = 0.0;
uniform vec4 flash_color : source_color = vec4(1.0);
// Outline around the sprite (alpha 0 = no outline). Used for selection / targeting.
uniform vec4 outline_color : source_color = vec4(0.0);

void fragment() {
	vec4 c = COLOR;
	if (c.a < 0.1 && outline_color.a > 0.0) {
		vec2 px = TEXTURE_PIXEL_SIZE;
		float n = texture(TEXTURE, UV + vec2(px.x, 0.0)).a
			+ texture(TEXTURE, UV - vec2(px.x, 0.0)).a
			+ texture(TEXTURE, UV + vec2(0.0, px.y)).a
			+ texture(TEXTURE, UV - vec2(0.0, px.y)).a;
		if (n > 0.0) {
			c = outline_color;
		}
	}
	c.rgb = mix(c.rgb, flash_color.rgb, flash * c.a);
	COLOR = c;
}
