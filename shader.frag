#version 333 core
out vec4 FragColor;

void main()
{
    int i;
    float delta;
    float h = 0.1f;
    for(delta = .0; delta <= 1.0; delta = delta + h)
    {
        FragColor = vec4(delta, delta, delta, 1.0f);
        i++;
    }
}