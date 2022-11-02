using System.Collections.Generic;

namespace ExtractDx11MESH;

public class Bone
{
	public string name;
	public Matrix4x4 transform1;
	public Matrix4x4 transform2;
	public Matrix4x4 transform3;
	public int parent;
}
