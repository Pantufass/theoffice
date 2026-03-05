shader_type spatial;
render_mode blend_add, unshaded;  // Additive blending

uniform vec4 glow_color : source_color = vec4(1.0, 1.0, 0.8, 0.1);
uniform float glow_width : hint_range(0.0, 0.05) = 0.005;

void vertex() {
    // Expand vertices slightly
    VERTEX += NORMAL * glow_width;
}

void fragment() {
    // NO ALBEDO - this preserves the original colors
    // Just output emission/glow
    EMISSION = glow_color.rgb;
    ALPHA = glow_color.a;
}