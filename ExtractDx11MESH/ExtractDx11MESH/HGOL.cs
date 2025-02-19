﻿using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
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

		iPos += 6; // Skip 2 bytes + an Int32 which is the number of bones again

		foreach (Bone bone in Bones)
		{
			bone.transform2 = ReadTransform();
		}

		iPos += 8; // Skip 4 bytes + an Int32 which is the number of bones again

		foreach (Bone bone in Bones)
		{
			bone.transform3 = ReadTransform();
		}

		return iPos;
	}

	protected virtual Matrix4x4 ReadTransform()
	{
		Matrix4x4 matrix = new Matrix4x4();

		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				float readValue = BigEndianBitConverter.ToSingle(fileData, iPos);
				matrix.m[i][j] = readValue;
				iPos += 4; // Skip the single
			}
		}

		return matrix;
	}

	protected virtual Bone ReadBone()
	{
		Bone bone = new Bone();
		var name = ReadString(fileData, iPos);
		iPos += name.Length + 1; // Skip the string and the null terminator
		bone.name = name;

		Matrix4x4 matrix = new Matrix4x4();

		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				float readValue = BigEndianBitConverter.ToSingle(fileData, iPos);
				matrix.m[i][j] = readValue;
				iPos += 4; // Skip the single
			}
		}

		bone.transform1 = matrix;

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