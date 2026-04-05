shader_type spatial;
render_mode blend_mix, unshaded;

uniform vec4 outline_color : source_color = vec4(1.0, 1.0, 0.5, 0.4);
uniform float outline_width : hint_range(0.0, 0.02) = 0.002;

void vertex() {
    // Expand vertices very slightly
    VERTEX += NORMAL * outline_width;
}

void fragment() {
    // DON'T set ALBEDO - this preserves the original texture/colors
    // Just add a subtle emission glow
    EMISSION = outline_color.rgb * 1.5;
    ALPHA = outline_color.a;
}