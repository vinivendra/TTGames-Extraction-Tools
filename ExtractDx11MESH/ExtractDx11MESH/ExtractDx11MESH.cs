using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ExtractDx11MESH.IVL5;
using ExtractDx11MESH.MESHs;
using ExtractDx11MESH.TXGHs;
using ExtractHelper;
using ExtractHelper.VariableTypes;
using Microsoft.SqlServer.Server;

namespace ExtractDx11MESH;

public class ExtractDx11MESH
{
	private static string directoryname;

	private string extention;

	private string filename;

	private static string filenamewithoutextension;

	private string fullPath;

	private int iPos = 0;

	private byte[] fileData;

	private int referencecounter = 5;

	private bool onlyModel = false;

	private MESH04 mesh;

	private TXGH01 txgh;

	private IVL501 ivl5;

	private List<string> boneNames = new List<string>();

	private bool extractMesh = true;

	public void ParseArgs(string[] args)
	{
		if (args.Count() < 1)
		{
			throw new ArgumentException("No argument handed over!");
		}
		if (!File.Exists(args[0]))
		{
			throw new ArgumentException($"File {args[0]} does not exist!");
		}
		directoryname = Path.GetDirectoryName(args[0]);
		extention = Path.GetExtension(args[0]);
		filename = Path.GetFileName(args[0]);
		filenamewithoutextension = Path.GetFileNameWithoutExtension(args[0]);
		fullPath = Path.GetFullPath(args[0]);
		if (extention.ToUpper() == ".MODEL")
		{
			onlyModel = true;
		}
		else if (!(extention.ToUpper() == ".GHG") && !(extention.ToUpper() == ".GSC"))
		{
			throw new ArgumentException("File extention != .ghg and != .gsc");
		}
		for (int i = 1; i < args.Length; i++)
		{
			string text = args[i];
			if (text != null && text == "-x")
			{
				extractMesh = false;
			}
		}
	}

	public void Extract()
	{
		FileInfo fileInfo = new FileInfo(fullPath);
		directoryname = fileInfo.DirectoryName;
		FileStream fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		fileData = new byte[(int)fileInfo.Length];
		fileStream.Read(fileData, 0, (int)fileInfo.Length);
		fileStream.Close();
		if (onlyModel)
		{
			readGSC2();
		}
		else
		{
			readHGOL();
			readNU20();
			readMESH();
		}
		ColoredConsole.WriteLineInfo(fullPath);
	}

	private void readHGOL()
	{
		var pos = iPos;
		// "LOGH" in ASCII ("HGOL" backwards)
		while (pos + 3 < fileData.Length &&
			(fileData[pos] != 76 || fileData[pos + 1] != 79 || fileData[pos + 2] != 71 || fileData[pos + 3] != 72))
		{
			pos++;
		}
		if (fileData[pos] == 76 || fileData[pos + 1] == 79 || fileData[pos + 2] == 71 || fileData[pos + 3] == 72)
		{
			ColoredConsole.WriteLine("{0:x8}   HGOL with bone data", pos);
			// Skip the "LOGH" + a null terminator
			pos += 5;

			// Skip 10 bytes
			pos += 10;

			// Read the number of bones
			int numberOfBones = fileData[pos];
			ColoredConsole.WriteLine($"{pos:x8}   Number of bones: {numberOfBones}");

			// Skip 3 bytes
			pos += 3;

			for (int i = 0; i < numberOfBones; i++)
			{
				int boneStartIndex = pos;
				
				string boneName = readString(ref pos);
				if (boneName == "")
				{
					boneName = $"bone_{i + 1}";
				}

				boneNames.Add(boneName);
				ColoredConsole.WriteLine($"{boneStartIndex:x8}       Bone {i + 1}: {boneName}");

				// Skip a 4x4 matrix + 16 bytes
				pos += 4 * 16 + 16;
			}
		}
		else
		{
			ColoredConsole.WriteLineInfo("Nope");
		}
	}

	private string readString(ref int pos)
	{
		string result = "";
		while(pos < fileData.Length)
		{
			if (fileData[pos] == 0)
			{
				// Skip the null terminator
				pos++;
				return result;
			}
			else
			{
				result += (char)fileData[pos];
				pos++;
			}
		}
		ColoredConsole.WriteLineInfo("Warning: EOF while reading bone name");
		return "";
	}

	private void readNU20()
	{
		// "02UN" in ASCII ("NU20" backwards)
		while (fileData[iPos] != 48 || fileData[iPos + 1] != 50 || fileData[iPos + 2] != 85 || fileData[iPos + 3] != 78)
		{
			iPos++;
		}
		if (fileData[iPos] == 48 || fileData[iPos + 1] == 50 || fileData[iPos + 2] == 85 || fileData[iPos + 3] == 78)
		{
			iPos += 4;
			switch (BigEndianBitConverter.ToInt32(fileData, iPos))
			{
			case 67:
				readTXGH();
				referencecounter++;
				break;
			case 78:
				readTXGH();
				referencecounter++;
				break;
			case 79:
			case 83:
			case 86:
			case 88:
			case 92:
				break;
			default:
				throw new NotSupportedException($"NU20 Version {BigEndianBitConverter.ToInt32(fileData, iPos):x2}");
			}
		}
	}

	private void readIVL5()
	{
		while (fileData[iPos] != 53 || fileData[iPos + 1] != 76 || fileData[iPos + 2] != 86 || fileData[iPos + 3] != 73)
		{
			iPos++;
		}
		if (fileData[iPos] != 53 || fileData[iPos + 1] != 76 || fileData[iPos + 2] != 86 || fileData[iPos + 3] != 73)
		{
			iPos += 4;
			int num = BigEndianBitConverter.ToInt32(fileData, iPos);
			if (num != 1)
			{
				throw new NotSupportedException($"IVL5 Version {BigEndianBitConverter.ToInt32(fileData, iPos):x2}");
			}
			ivl5 = new IVL501(fileData, iPos);
		}
	}

	private void readGSC2()
	{
		while (fileData[iPos] != 50 || fileData[iPos + 1] != 67 || fileData[iPos + 2] != 83 || fileData[iPos + 3] != 71)
		{
			iPos++;
		}
		if (fileData[iPos] == 50 || fileData[iPos + 1] == 67 || fileData[iPos + 2] == 83 || fileData[iPos + 3] == 71)
		{
			iPos += 4;
			GSC2EB gSC2EB = new GSC2EB(fileData, iPos);
			iPos = gSC2EB.Read(ref referencecounter, directoryname, filenamewithoutextension);
		}
	}

	private void readMESH()
	{
		while (fileData[iPos] != 72 || fileData[iPos + 1] != 83 || fileData[iPos + 2] != 69 || fileData[iPos + 3] != 77)
		{
			iPos++;
		}
		if (fileData[iPos] == 72 && fileData[iPos + 1] == 83 && fileData[iPos + 2] == 69 && fileData[iPos + 3] == 77)
		{
			iPos += 4;
			switch (BigEndianBitConverter.ToInt32(fileData, iPos))
			{
			case 4:
				mesh = new MESH04(fileData, iPos);
				break;
			case 5:
				mesh = new MESH05(fileData, iPos);
				break;
			case 46:
				mesh = new MESH2E(fileData, iPos);
				break;
			case 47:
				mesh = new MESH2F(fileData, iPos);
				break;
			case 48:
				mesh = new MESH30(fileData, iPos);
				break;
			case 169:
				mesh = new MESHA9(fileData, iPos);
				break;
			case 170:
				mesh = new MESHAA(fileData, iPos);
				break;
			case 175:
				mesh = new MESHAF(fileData, iPos);
				break;
			case 200:
				mesh = new MESHC8(fileData, iPos);
				break;
			case 201:
				referencecounter = 4;
				mesh = new MESHC9(fileData, iPos);
				break;
			default:
				throw new NotSupportedException($"MESH Version {BigEndianBitConverter.ToInt32(fileData, iPos):x2}");
			}
			iPos = mesh.Read(ref referencecounter);
			int num = 0;
			bool flag = true;
			{
                ColoredConsole.WriteLine("Exporting Collada file...");

                ColladaExporter colladaExporter = new ColladaExporter();
				colladaExporter.boneNames = boneNames;

				string path = directoryname + "\\" + filenamewithoutextension + ".dae";
                colladaExporter.StartFile(path);

				ColoredConsole.WriteLine($"	Output file path: {path}");

				num = 0;
				foreach (Part part in mesh.Parts)
				{
					flag = false;
					num++;
					colladaExporter.AddMesh(mesh, part, num);
				}

				colladaExporter.EndGeometriesAndStartSkin();

				num = 0;
				foreach (Part part in mesh.Parts)
				{
					flag = false;
					num++;
					colladaExporter.AddSkins(mesh, part, num);
				}

				colladaExporter.EndSkinAndStartSceneForArmatures();

				num = 0;
				foreach (Part part in mesh.Parts)
				{
					flag = false;
					num++;
					colladaExporter.AddScenesForArmatures(mesh, part, num);
				}

				colladaExporter.EndArmaturesAndStartSceneForObjects();
				num = 0;
				foreach (Part part in mesh.Parts)
				{
					flag = false;
					num++;
					colladaExporter.AddScenesForObjects(mesh, part, num);
				}

				colladaExporter.EndFile(mesh.Parts.Count);
				
                return;
			}
		}
		ColoredConsole.WriteLine("No MESH");
	}

	private void readTXGH()
	{
		while (fileData[iPos] != 72 || fileData[iPos + 1] != 71 || fileData[iPos + 2] != 88 || fileData[iPos + 3] != 84)
		{
			iPos++;
		}
		if (fileData[iPos] == 72 && fileData[iPos + 1] == 71 && fileData[iPos + 2] == 88 && fileData[iPos + 3] == 84)
		{
			iPos += 4;
			switch (BigEndianBitConverter.ToInt32(fileData, iPos))
			{
			case 1:
				referencecounter = 9;
				txgh = new TXGH01(fileData, iPos);
				break;
			case 3:
				referencecounter = 9;
				txgh = new TXGH03(fileData, iPos);
				break;
			case 4:
				referencecounter = 9;
				txgh = new TXGH04(fileData, iPos);
				break;
			case 5:
				referencecounter = 9;
				txgh = new TXGH05(fileData, iPos);
				break;
			case 6:
				referencecounter = 9;
				txgh = new TXGH06(fileData, iPos);
				break;
			case 7:
				referencecounter = 9;
				txgh = new TXGH07(fileData, iPos);
				break;
			case 8:
				referencecounter = 7;
				txgh = new TXGH08(fileData, iPos);
				break;
			case 9:
				referencecounter = 7;
				txgh = new TXGH09(fileData, iPos);
				break;
			case 10:
				referencecounter = 6;
				txgh = new TXGH0A(fileData, iPos);
				break;
			case 12:
				txgh = new TXGH0C(fileData, iPos);
				break;
			default:
				throw new NotSupportedException($"TXGH Version {BigEndianBitConverter.ToInt32(fileData, iPos):x2}");
			}
			iPos = txgh.Read(ref referencecounter);
		}
		else
		{
			ColoredConsole.WriteLine("No TXGH");
		}
	}

	private void CheckData(Part part)
	{
		bool flag = false;
		bool flag2 = false;
		List<Vertex> list = null;
		VertexList vertexList = mesh.Vertexlistsdictionary[part.VertexListReferences1[0]];
		VertexList vertexList2 = null;
		if (part.VertexListReferences1.Count > 1)
		{
			vertexList2 = mesh.Vertexlistsdictionary[part.VertexListReferences1[1]];
		}
		List<int> list2 = mesh.Indexlistsdictionary[part.IndexListReference1];
		if (vertexList.Vertices[0].Position != null)
		{
			for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
			{
				Vector3 position = vertexList.Vertices[i].Position;
			}
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].Position != null)
		{
			for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
			{
				Vector3 position = vertexList2.Vertices[i].Position;
			}
		}
		if (vertexList.Vertices[0].UVSet0 != null)
		{
			flag2 = true;
			for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
			{
				Vector2 uVSet = vertexList.Vertices[i].UVSet0;
			}
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].UVSet0 != null)
		{
			flag2 = true;
			for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
			{
				Vector2 uVSet = vertexList2.Vertices[i].UVSet0;
			}
		}
		if (vertexList.Vertices[0].Normal != null)
		{
			flag = true;
			for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
			{
				Vector3 normal = vertexList.Vertices[i].Normal;
			}
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].Normal != null)
		{
			flag = true;
			for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
			{
				Vector3 normal = vertexList2.Vertices[i].Normal;
			}
		}
		if (vertexList.Vertices[0].ColorSet0 != null)
		{
			list = vertexList.Vertices;
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].ColorSet0 != null)
		{
			list = vertexList2.Vertices;
		}
	}
}
