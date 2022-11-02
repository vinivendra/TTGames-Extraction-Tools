using System.Collections.Generic;

namespace ExtractDx11MESH;

public class Matrix4x4
{
	public float[][] m;

	public Matrix4x4()
	{
		m = new float[4][];
		for (int i = 0; i < 4; i++)
		{
			m[i] = new float[4];
			for (int j = 0; j < 4; j++)
			{
				m[i][j] = (i == j) ? 1 : 0;
			}
		}
	}

	public Matrix4x4 Multiply(Matrix4x4 other)
	{
		Matrix4x4 result = new Matrix4x4();
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				float number = 0;
				for (int k = 0; k < 4; k++)
				{
					number += this.m[i][k] * other.m[k][j];
				}
				result.m[i][j] = number;
			}
		}

		return result;
	}
}
