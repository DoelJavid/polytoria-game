shader_type spatial;
render_mode cull_back, diffuse_burley, specular_schlick_ggx;

instance uniform vec4 color : source_color = vec4(1.0);
instance uniform float emissive_strength : hint_range(0.0, 1.0) = 0.0;

uniform sampler2D albedo : source_color;
uniform bool use_albedo_texture = true;

uniform sampler2D orm;
uniform bool use_orm_texture = true;

uniform float specular_value : hint_range(0.0, 1.0) = 0.2;

uniform float studs_per_tile = 2.0;

varying vec3 v_local_pos;
varying vec3 v_local_normal;
varying vec3 v_object_scale;

vec2 calculate_uv(vec3 pos, vec3 normal)
{
	vec3 an = abs(normal);
	vec2 uv;

	if (an.x >= an.y && an.x >= an.z) {
		uv = pos.zy;
		if (normal.x < 0.0) {
			uv.x = -uv.x;
		}
	} else if (an.y >= an.z) {
		uv = pos.xz;
		if (normal.y < 0.0) {
			uv.y = -uv.y;
		}
	} else {
		uv = pos.xy;
		if (normal.z > 0.0) {
			uv.x = -uv.x;
		}
	}

	return uv;
}

void vertex() {
	v_local_pos = VERTEX;
	v_local_normal = normalize(NORMAL);
	v_object_scale = vec3(
		length(MODEL_MATRIX[0].xyz),
		length(MODEL_MATRIX[1].xyz),
		length(MODEL_MATRIX[2].xyz)
	);
}

void fragment() {
	vec3 n = normalize(v_local_normal);
	vec3 part_space = v_local_pos * v_object_scale;
	vec2 uv = calculate_uv(part_space, n) / studs_per_tile;
	vec4 albedo_color = color;
	
	float occlusion_value = 1.0;
	float roughness_value = 0.7;
	float metallic_value = 0.0;

	if (use_albedo_texture) {
		albedo_color *= texture(albedo, uv);
	}

	if (use_orm_texture) {
		vec3 orm_values = texture(orm, uv).rgb;
		occlusion_value = orm_values.r;
		roughness_value = orm_values.g;
		metallic_value = orm_values.b;
	}

	ALBEDO = albedo_color.rgb * COLOR.rgb;
	ROUGHNESS = roughness_value;
	SPECULAR = specular_value;
	METALLIC = metallic_value;
	// AO = occlusion_value;

	if (emissive_strength > 0.0) {
		EMISSION = ALBEDO * emissive_strength;
	}
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
