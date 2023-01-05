
public static class Shaders{
    public static string DiffuseShader = 
"""
#version 450

layout(set = 0, binding = 0) buffer Vectors
{
    float VectorsBuffer[];
};
layout(set = 0, binding = 1) buffer Indices{
    int IndicesBuffer[];
};

layout(set = 0, binding = 2) uniform DataInformation{
    int MaxVectorsCount;
    int VectorSize;
};

layout(set = 1, binding = 0) buffer Input{
    float InputVector[];
};

layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

void main(){
    uint index = gl_GlobalInvocationID.x;
    InputVector[0] = MaxVectorsCount;
    InputVector[1] = VectorSize;
}
""";
}