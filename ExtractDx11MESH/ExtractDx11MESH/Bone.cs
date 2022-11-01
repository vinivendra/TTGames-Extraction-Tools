using System.Collections.Generic;

namespace ExtractDx11MESH;

public class Bone
{
	public string name;

	public List<List<float>> transform;

	public int parent;
}
