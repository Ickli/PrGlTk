#version 330 core

out vec4 outputColor;
in vec3 color;

void main() {
    // outputColor = vec4(color,1.0f);
    outputColor = vec4((color + vec3(1,1,1)) / 2, 1.0f);// * vec4(0.0f, 0.3f, 0.0f, 1.0f);
//    outputColor = vec4(color, 1.0f);
}
