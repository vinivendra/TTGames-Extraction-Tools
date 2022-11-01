using System;
using System.Collections.Generic;
using ExtractHelper;
using ExtractHelper.VariableTypes;

namespace ExtractDx11MESH;

public class HGOL
{
	protected byte[] fileData;

	protected int iPos;

	public List<Bone> Bones = new List<Bone>();
	public HGOL(byte[] fileData, int iPos)
	{
		this.fileData = fileData;
		this.iPos = iPos;
		ColoredConsole.WriteLineInfo("{0:x8} HGOL", iPos);
	}
	public virtual int Read()
	{
		iPos += 8; // Skip 8 bytes
		int numberOfBones = BigEndianBitConverter.ToInt32(fileData, iPos);
		ColoredConsole.WriteLine("{0:x8}   Number of Bones: {1:D}", iPos, numberOfBones);
		iPos += 4; // Read the Int32 above

		iPos += 2; // Skip 2 bytes

		for (int i = 0; i < numberOfBones; i++)
		{
			Bones.Add(ReadBone());
		}
		return iPos;
	}

	protected virtual Bone ReadBone()
	{
		Bone bone = new Bone();
		var name = ReadString(fileData, iPos);
		iPos += name.Length + 1; // Skip the string and the null terminator
		bone.name = name;

		List<List<float>> matrix = new List<List<float>>();

		for (int i = 0; i < 4; i++)
		{
			matrix.Add(new List<float>());
			for (int j = 0; j < 4; j++)
			{
				float readValue = BigEndianBitConverter.ToSingle(fileData, iPos);
				matrix[i].Add(readValue);
				iPos += 4; // Skip the single
			}
		}

		bone.transform = matrix;

		iPos += 12; // Skip unknown 12 bytes

		bone.parent = ReadSignedByte(fileData, iPos);
		iPos += 4; // Skip the byte above and 3 other unknowns

		return bone;
	}

	protected string ReadString(byte[] data, int i)
	{
		string result = "";
		while (data[i] != 0)
		{
			char character = (char)data[i];
			result += character;
			i++;
		}
		return result;
	}
	protected sbyte ReadSignedByte(byte[] data, int i)
	{
		byte readByte = data[i];
		return (sbyte)readByte;
	}
}