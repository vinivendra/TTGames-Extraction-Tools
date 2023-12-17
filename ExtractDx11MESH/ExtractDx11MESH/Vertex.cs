using ExtractHelper.VariableTypes;

namespace ExtractDx11MESH;

public class Byte4
{
	public byte a;
	public byte b;
	public byte c;
	public byte d;
}

public class Vertex
{
	public Vector3 Position;

	public Vector3 Normal;

	public Color4 ColorSet0;

	public Color4 ColorSet1;

	public Vector2 UVSet0;

	public Vector2 UVSet1;

	public Byte4 BlendIndices0;

	public Vector4 BlendWeight0;
}
