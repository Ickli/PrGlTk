#version 330 core
layout (location = 0) in vec3 aPosition;
layout (location = 1) in vec3 aNormal;
out vec3 color;
uniform mat4 view;
uniform mat4 projection;
uniform mat4 model;
uniform vec3 light_dir;

void main() {
    gl_Position = projection * view * model * vec4(aPosition, 1.0f);
    color = vec3(0.7f, 0.7f, 0.7f) * dot(aNormal, light_dir);
}
