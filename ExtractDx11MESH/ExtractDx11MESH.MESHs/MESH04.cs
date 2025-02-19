using System;
using System.Collections.Generic;
using ExtractHelper;
using ExtractHelper.VariableTypes;

namespace ExtractDx11MESH.MESHs;

public class MESH04
{
	protected float[] lookUp;
	protected float[] lookUpU;

	public Dictionary<int, VertexList> Vertexlistsdictionary = new Dictionary<int, VertexList>();

	public Dictionary<int, List<int>> Indexlistsdictionary = new Dictionary<int, List<int>>();

	public List<Part> Parts = new List<Part>();

	protected byte[] fileData;

	protected int iPos;

	public int version;

	protected float[] LookUp
	{
		get
		{
			if (lookUp == null)
			{
				double num = 1.0 / 127.0;
				lookUp = new float[256];
				lookUp[0] = -1f;
				for (int i = 1; i < 256; i++)
				{
					lookUp[i] = (float)((double)lookUp[i - 1] + num);
				}
				lookUp[127] = 0f;
				lookUp[255] = 1f;
			}
			return lookUp;
		}
	}

	protected float[] LookUpU
	{
		get
		{
			if (lookUpU == null)
			{
				double num = 1.0 / 255.0;
				lookUpU = new float[256];
				lookUpU[0] = 0f;
				for (int i = 1; i < 256; i++)
				{
					lookUpU[i] = (float)((double)lookUpU[i - 1] + num);
				}
				lookUpU[255] = 1f;
			}
			return lookUpU;
		}
	}

	public MESH04(byte[] fileData, int iPos)
	{
		this.fileData = fileData;
		this.iPos = iPos;
		version = BigEndianBitConverter.ToInt32(fileData, iPos);
		this.iPos += 4;
		ColoredConsole.WriteLineInfo("{0:x8} MESH Version 0x{1:x2}", iPos, version);
	}

	public virtual int Read(ref int referencecounter)
	{
		int num = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}   Number of Parts: 0x{1:x8}", iPos, num);
		iPos += 4;
		for (int i = 0; i < num; i++)
		{
			ColoredConsole.WriteLine("{0:x8}   Part 0x{1:x8}", iPos, i);
			Parts.Add(ReadPart(ref referencecounter));
		}
		return iPos;
	}

	protected virtual Part ReadPart(ref int referencecounter)
	{
		Part part = new Part();
		int num = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}     Number of Vertex Lists: 0x{1:x8}", iPos, num);
		iPos += 4;
		for (int i = 0; i < num; i++)
		{
			ColoredConsole.WriteLine("{0:x8}       Vertex List 0x{1:x8}", iPos, i);
			part.VertexListReferences1.Add(GetVertexListReference(ref referencecounter));
		}
		iPos += 4;
		part.IndexListReference1 = GetIndexListReference(ref referencecounter);
		part.OffsetIndices = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}     Offset Indices: 0x{1:x8}", iPos, part.OffsetIndices);
		iPos += 4;
		part.NumberIndices = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}     Number Indices: 0x{1:x8}", iPos, part.NumberIndices);
		iPos += 4;
		part.OffsetVertices = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}     Offset Vertices: 0x{1:x8}", iPos, part.OffsetVertices);
		iPos += 4;
		if (BigEndianBitConverter.ToInt16(fileData, iPos) != 0)
		{
			throw new NotSupportedException("ReadPart Offset Vertices + 4");
		}
		iPos += 2;
		part.NumberVertices = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}     Number Vertices: 0x{1:x8}", iPos, part.NumberVertices);
		iPos += 4;
		referencecounter++;
		iPos += 4;
		int num2 = BigEndianBitConverter.ToInt32(fileData, iPos);
		iPos += 4;
		if (num2 > 0)
		{
			ColoredConsole.Write("{0:x8}     ", iPos);
			for (int i = 0; i < num2; i++)
			{
				ColoredConsole.Write("{0:x2} ", fileData[iPos]);
				iPos++;
			}
			ColoredConsole.WriteLine();
			referencecounter++;
		}
		int num3 = BigEndianBitConverter.ToInt32(fileData, iPos);
		iPos += 4;
		if (num3 != 0)
		{
			int num4 = ReadRelativePositionList();
			referencecounter += num4;
		}
		return part;
	}

	protected virtual int ReadRelativePositionList()
	{
		iPos += 4;
		int num = 1;
		while (BigEndianBitConverter.ToInt32(fileData, iPos) != 0)
		{
			iPos += 8;
			num++;
		}
		ColoredConsole.WriteLine("{0:x8}       Relative Positions: 0x{1:x8}", iPos, num);
		ColoredConsole.WriteLineError("{0:x8} Unknown: {1}", iPos, BigEndianBitConverter.ToInt32(fileData, iPos));
		iPos += 4;
		for (int i = 0; i < num; i++)
		{
			int num2 = BigEndianBitConverter.ToInt32(fileData, iPos);
			iPos += 4;
			if (num2 != 0)
			{
				ColoredConsole.WriteLine("{0:x8}       Relative Positions: 0x{1:x8}", iPos, num2);
				iPos += num2 * 12;
			}
			iPos += 2;
			iPos += 4;
			int num3 = BigEndianBitConverter.ToInt32(fileData, iPos);
			iPos += 4;
			ColoredConsole.WriteLine("{0:x8}       Relative Position Tupels: 0x{1:x8}", iPos, num3);
			iPos += 4 * num3;
		}
		return num * 3;
	}

	protected virtual int GetIndexListReference(ref int referencecounter)
	{
		int num = -1;
		if (fileData[iPos] == 192)
		{
			num = BigEndianBitConverter.ToInt16(fileData, iPos + 2);
			iPos += 4;
			ColoredConsole.WriteLine("{0:x8}     Index List Reference to 0x{1:x4}", iPos, num);
			iPos += 4;
		}
		else
		{
			ColoredConsole.WriteLine("{0:x8}         New Index List 0x{1:x4}", iPos, referencecounter);
			iPos += 4;
			iPos += 4;
			int num2 = BigEndianBitConverter.ToInt32(fileData, iPos);
			ColoredConsole.WriteLine("{0:x8}           Number of Indices: {1:x8}", iPos, num2);
			iPos += 4;
			iPos += 4;
			List<int> list = new List<int>();
			for (int i = 0; i < num2; i++)
			{
				list.Add(BigEndianBitConverter.ToUInt16(fileData, iPos));
				iPos += 2;
			}
			Indexlistsdictionary.Add(referencecounter, list);
			num = referencecounter++;
		}
		return num;
	}

	protected virtual int GetVertexListReference(ref int referencecounter)
	{
		int num = -1;
		if (fileData[iPos] == 192)
		{
			num = BigEndianBitConverter.ToInt16(fileData, iPos + 2);
			iPos += 4;
			ColoredConsole.WriteLineWarn("{0:x8}         Vertex List Reference to 0x{1:x4}", iPos, num);
			iPos += 4;
			iPos += 4;
		}
		else
		{
			ColoredConsole.WriteLineWarn("{0:x8}         New Vertex List 0x{1:x4}", iPos, referencecounter);
			iPos += 4;
			iPos += 4;
			int numberofvertices = BigEndianBitConverter.ToInt32(fileData, iPos);
			iPos += 4;
			VertexList value = ReadVertexList(numberofvertices);
			iPos += 4;
			Vertexlistsdictionary.Add(referencecounter, value);
			num = referencecounter++;
		}
		return num;
	}

	protected virtual VertexList ReadVertexList(int numberofvertices)
	{
		VertexList vertexList = new VertexList();
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.position;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.normal;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.colorSet0;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.tangent;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.colorSet1;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.uvSet01;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.uvSet2;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.blendIndices0;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.blendWeight0;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.lightDirSet;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		if (fileData[iPos] != 0)
		{
			VertexDefinition vertexDefinition = new VertexDefinition();
			vertexDefinition.Variable = VertexDefinition.VariableEnum.lightColSet;
			vertexDefinition.VariableType = (VertexDefinition.VariableTypeEnum)fileData[iPos];
			vertexList.VertexDefinitions.Add(vertexDefinition);
			ColoredConsole.WriteLine("{0:x8}             {1} {2}", iPos, vertexDefinition.VariableType.ToString(), vertexDefinition.Variable.ToString());
		}
		iPos += 2;
		iPos += 6;
		ColoredConsole.WriteLine("{0:x8}           Number of Vertices: {1:x8}", iPos, numberofvertices);
		for (int i = 0; i < numberofvertices; i++)
		{
			vertexList.Vertices.Add(ReadVertex(vertexList.VertexDefinitions));
		}
		return vertexList;
	}

	protected virtual Vertex ReadVertex(List<VertexDefinition> vertexdefinitions)
	{
		Vertex vertex = new Vertex();
		foreach (VertexDefinition vertexdefinition in vertexdefinitions)
		{
			ColoredConsole.WriteLine("{0:x8}           Def {1} {2}", iPos, vertexdefinition.VariableType.ToString(), vertexdefinition.Variable.ToString());

			switch (vertexdefinition.Variable)
			{
			case VertexDefinition.VariableEnum.position:
				vertex.Position = (Vector3)ReadVariableValue(vertexdefinition.VariableType);
				break;
			case VertexDefinition.VariableEnum.normal:
				vertex.Normal = (Vector3)ReadVariableValue(vertexdefinition.VariableType);
				break;
			case VertexDefinition.VariableEnum.colorSet0:
				vertex.ColorSet0 = (Color4)ReadVariableValue(vertexdefinition.VariableType);
				break;
			case VertexDefinition.VariableEnum.colorSet1:
				vertex.ColorSet1 = (Color4)ReadVariableValue(vertexdefinition.VariableType);
				break;
			case VertexDefinition.VariableEnum.uvSet01:
				vertex.UVSet0 = (Vector2)ReadVariableValue(vertexdefinition.VariableType);
				break;
			case VertexDefinition.VariableEnum.blendIndices0:
				vertex.BlendIndices0 = (Byte4)ReadVariableValue(vertexdefinition.VariableType);
				break;
			case VertexDefinition.VariableEnum.blendWeight0:
				vertex.BlendWeight0 = (Vector4)ReadVariableValue(vertexdefinition.VariableType);
				ColoredConsole.WriteLine("{0:x8}           Blend weights {1} {2}", iPos, vertexdefinition.VariableType, vertex.BlendWeight0);
				break;
			case VertexDefinition.VariableEnum.tangent:
			case VertexDefinition.VariableEnum.unknown6:
			case VertexDefinition.VariableEnum.uvSet2:
			case VertexDefinition.VariableEnum.unknown8:
			case VertexDefinition.VariableEnum.unknown11:
			case VertexDefinition.VariableEnum.lightDirSet:
			case VertexDefinition.VariableEnum.lightColSet:
				ReadVariableValue(vertexdefinition.VariableType);
				break;
			default:
				throw new NotSupportedException(vertexdefinition.Variable.ToString());
			}
		}
		return vertex;
	}

	protected virtual object ReadVariableValue(VertexDefinition.VariableTypeEnum variabletype)
	{
		switch (variabletype)
		{
		case VertexDefinition.VariableTypeEnum.vec2float:
		{
			Vector2 vector6 = new Vector2();
			vector6.X = BigEndianBitConverter.ToSingle(fileData, iPos);
			vector6.Y = BigEndianBitConverter.ToSingle(fileData, iPos + 4);
			Vector2 result7 = vector6;
			iPos += 8;
			return result7;
		}
		case VertexDefinition.VariableTypeEnum.vec3float:
		{
			Vector3 vector5 = new Vector3();
			vector5.X = BigEndianBitConverter.ToSingle(fileData, iPos);
			vector5.Y = BigEndianBitConverter.ToSingle(fileData, iPos + 4);
			vector5.Z = BigEndianBitConverter.ToSingle(fileData, iPos + 8);
			Vector3 result6 = vector5;
			iPos += 12;
			return result6;
		}
		case VertexDefinition.VariableTypeEnum.vec4float:
		{
			Vector4 vector4 = new Vector4();
			vector4.X = BigEndianBitConverter.ToSingle(fileData, iPos);
			vector4.Y = BigEndianBitConverter.ToSingle(fileData, iPos + 4);
			vector4.Z = BigEndianBitConverter.ToSingle(fileData, iPos + 8);
			vector4.W = BigEndianBitConverter.ToHalf(fileData, iPos + 12);
			Vector4 result5 = vector4;
			iPos += 16;
			return result5;
		}
		case VertexDefinition.VariableTypeEnum.vec2half:
		{
			Vector2 vector3 = new Vector2();
			vector3.X = BigEndianBitConverter.ToHalf(fileData, iPos);
			vector3.Y = BigEndianBitConverter.ToHalf(fileData, iPos + 2);
			Vector2 result4 = vector3;
			iPos += 4;
			return result4;
		}
		case VertexDefinition.VariableTypeEnum.vec4half:
		{
			Vector4 vector2 = new Vector4();
			vector2.X = BigEndianBitConverter.ToHalf(fileData, iPos);
			vector2.Y = BigEndianBitConverter.ToHalf(fileData, iPos + 2);
			vector2.Z = BigEndianBitConverter.ToHalf(fileData, iPos + 4);
			vector2.W = BigEndianBitConverter.ToHalf(fileData, iPos + 6);
			Vector4 result3 = vector2;
			iPos += 8;
			return result3;
		}
		case VertexDefinition.VariableTypeEnum.vec4char:
			Byte4 bytes = new Byte4();
			bytes.a = fileData[iPos];
			bytes.b = fileData[iPos + 1];
			bytes.c = fileData[iPos + 2];
			bytes.d = fileData[iPos + 3];
			Byte4 result8 = bytes;
			iPos += 4;
			return result8;
		case VertexDefinition.VariableTypeEnum.vec4mini:
		{
			Vector4 vector = new Vector4();
			vector.X = LookUp[fileData[iPos]];
			vector.Y = LookUp[fileData[iPos + 1]];
			vector.Z = LookUp[fileData[iPos + 2]];
			vector.W = LookUp[fileData[iPos + 3]];
			Vector4 result2 = vector;
			iPos += 4;
			return result2;
		}
		case VertexDefinition.VariableTypeEnum.color4char:
		{
			Color4 color = new Color4();
			color.R = fileData[iPos];
			color.G = fileData[iPos + 1];
			color.B = fileData[iPos + 2];
			color.A = fileData[iPos + 3];
			Color4 result = color;
			iPos += 4;
			return result;
		}
		default:
			throw new NotImplementedException(variabletype.ToString());
		}
	}
}
