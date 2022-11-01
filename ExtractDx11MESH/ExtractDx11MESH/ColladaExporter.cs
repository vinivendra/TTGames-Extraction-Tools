using ExtractDx11MESH.MESHs;
using ExtractHelper;
using ExtractHelper.VariableTypes;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ExtractDx11MESH;
public class ColladaExporter
{
	private StreamWriter streamWriter;

	public MESH04 mesh;
	public List<HGOL> hgols;

	public void WriteFile(string path)
	{
		streamWriter = new StreamWriter(path);
		OpenFileAndStartGeometry();

		ColoredConsole.WriteLine($"    Writing information on {mesh.Parts.Count} meshes...");
		int num = 0;
		foreach (Part part in mesh.Parts)
		{
			num++;
			AddMeshToGeometry(mesh, part, num);
		}
		
		EndGeometryAndStartScene();

		ColoredConsole.WriteLine($"    Placing {mesh.Parts.Count} meshes on the scene...");
		AddMeshesToScene();

		ColoredConsole.WriteLine($"    Placing {hgols.Count} armatures on the scene...");
		AddArmaturesToScene();

		EndSceneAndCloseFile();
	}

	private void OpenFileAndStartGeometry()
	{
		string fileContents = @"<?xml version=""1.0"" encoding=""utf-8""?>
<COLLADA xmlns=""http://www.collada.org/2005/11/COLLADASchema"" version=""1.4.1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <asset>
	<contributor>
	  <author>ExtractDx11Mesh</author>
	  <authoring_tool>ExtractDx11Mesh</authoring_tool>
	</contributor>
	<unit name=""meter"" meter=""1""/>
	<up_axis>Y_UP</up_axis>
  </asset>
  <library_effects>
	<effect id=""Material-effect"">
	  <profile_COMMON>
		<technique sid=""common"">
		  <lambert>
			<emission>
			  <color sid=""emission"">0 0 0 1</color>
			</emission>
			<diffuse>
			  <color sid=""diffuse"">0.8 0.8 0.8 1</color>
			</diffuse>
			<index_of_refraction>
			  <float sid=""ior"">1.45</float>
			</index_of_refraction>
		  </lambert>
		</technique>
	  </profile_COMMON>
	</effect>
  </library_effects>
  <library_images/>
  <library_materials>
	<material id=""Material-material"" name=""Material"">
	  <instance_effect url=""#Material-effect""/>
	</material>
  </library_materials>
  <library_geometries>";

		streamWriter.Write(fileContents);
	}

	private void EndGeometryAndStartScene()
	{
		string fileContents = $@"
  </library_geometries>
  <library_visual_scenes>
	<visual_scene id=""Scene"" name=""Scene"">";

		streamWriter.Write(fileContents);
	}

	private void EndSceneAndCloseFile() {
		string fileContents = $@"
	</visual_scene>
  </library_visual_scenes>
  <scene>
	<instance_visual_scene url=""#Scene""/>
  </scene>
</COLLADA>";

		streamWriter.Write(fileContents);

		streamWriter.Close();
	}

	private void AddArmaturesToScene()
	{
		int i = 0;
		foreach (HGOL hgol in hgols)
		{
			i++;
			ColoredConsole.WriteLine($"        Armature #{i}...");
			// TODO: start HGOL

			// Bones without parent have parent = -1. All other bones are added recursively as children.
			AddBonesWithParent(hgol, -1, "");
			
			// TODO: end HGOL
		}
	}
	private void AddBonesWithParent(HGOL hgol, int parent, string indentation)
	{
		// TODO: print start

		// Print children
		for (int i = 0; i < hgol.Bones.Count; i++)
		{
			Bone bone = hgol.Bones[i];
			if (bone.parent == parent)
			{
				ColoredConsole.WriteLine($"            {indentation}Bone #{i}, parent {bone.parent}");
				AddBonesWithParent(hgol, i, indentation + "  ");
			}
		}

		// TODO: Print end
	}

	private void AddMeshesToScene()
	{

		int numberOfMeshes = mesh.Parts.Count;

		for (int i = 1; i <= numberOfMeshes; i += 1)
		{
			string meshNumber = $"{i:0000}";
			string fileContents = $@"
	  <node id=""Object_{meshNumber}"" name=""Object.{meshNumber}"" type=""NODE"">
		<matrix sid=""transform"">1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1</matrix>
		<instance_geometry url=""#Object_{meshNumber}-mesh"" name=""Object.{meshNumber}"">
		  <bind_material>
			<technique_common>
			  <instance_material symbol=""Material-material"" target=""#Material-material"">
				<bind_vertex_input semantic=""UVMap"" input_semantic=""TEXCOORD"" input_set=""0""/>
			  </instance_material>
			</technique_common>
		  </bind_material>
		</instance_geometry>
	  </node>";
			streamWriter.Write(fileContents);
		}
	}

	private void AddMeshToGeometry(MESH04 mesh, Part part, int partnumber)
	{
		VertexList vertexList = mesh.Vertexlistsdictionary[part.VertexListReferences1[0]];
		VertexList vertexList2 = null;
		if (part.VertexListReferences1.Count > 1)
		{
			vertexList2 = mesh.Vertexlistsdictionary[part.VertexListReferences1[1]];
		}
		List<int> list2 = null;
		try
		{
			list2 = mesh.Indexlistsdictionary[part.IndexListReference1];
		}
		catch (Exception ex)
		{
			ColoredConsole.WriteError("{0} @ Part {1:x4} Index {2:x4}", ex.Message, partnumber, part.IndexListReference1);
		}

		// Add vertices
		string meshNumber = $"{partnumber:0000}";
		if (vertexList.Vertices[0].Position != null)
		{
			AddVertices(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].Position != null)
		{
			AddVertices(meshNumber, part, vertexList2);
		}

		// Add UVs
		if (vertexList.Vertices[0].UVSet0 != null)
		{
			AddUVs(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].UVSet0 != null)
		{
			AddUVs(meshNumber, part, vertexList2);
		}

		// Add normals
		if (vertexList.Vertices[0].Normal != null)
		{
			AddNormals(meshNumber, part, vertexList);
		}
		else if (vertexList2 != null && vertexList2.Vertices[0].Normal != null)
		{
			AddNormals(meshNumber, part, vertexList2);
		}

		AddFaces(meshNumber, part, list2);
	}

	private void AddFaces(string meshNumber, Part part, List<int> indices)
	{
		int numberOfFaces = part.NumberIndices / 3;

		string fileContents = $@"
		<vertices id=""Object_{meshNumber}-mesh-vertices"">
		  <input semantic=""POSITION"" source=""#Object_{meshNumber}-mesh-positions""/>
		</vertices>
		<triangles material=""Material-material"" count=""{numberOfFaces}"">
		  <input semantic=""VERTEX"" source=""#Object_{meshNumber}-mesh-vertices"" offset=""0""/>
		  <input semantic=""NORMAL"" source=""#Object_{meshNumber}-mesh-normals"" offset=""1""/>
		  <input semantic=""TEXCOORD"" source=""#Object_{meshNumber}-mesh-map-0"" offset=""2"" set=""0""/>
		  <p> ";

		streamWriter.Write(fileContents);

		for (int i = part.OffsetIndices; i < part.OffsetIndices + part.NumberIndices; i += 3)
		{
			string format = "{0} {0} {0} {1} {1} {1} {2} {2} {2} ";

			int index1 = indices[i];
			int index2 = indices[i + 1];
			int index3 = indices[i + 2];

			fileContents = string.Format(format, index1, index2, index3);
			streamWriter.Write(fileContents);
		}

		fileContents = $@"</p>
		</triangles>
	  </mesh>
	</geometry>";
		streamWriter.Write(fileContents);
	}

	private void AddNormals(string meshNumber, Part part, VertexList vertexList)
	{
		string fileContents = @$"
		<source id=""Object_{meshNumber}-mesh-normals"">
		  <float_array id=""Object_{meshNumber}-mesh-normals-array"" count=""{part.NumberVertices * 3}""> ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Vector3 normal = vertexList.Vertices[i].Normal;
			fileContents = $"{normal.X:0.000000} {normal.Y:0.000000} {normal.Z:0.000000} ".Replace(',', '.');
			streamWriter.Write(fileContents);
		}

		fileContents = $@"</float_array>
		  <technique_common>
			<accessor source=""#Object_{meshNumber}-mesh-normals-array"" count=""{part.NumberVertices}"" stride=""3"">
			  <param name=""X"" type=""float""/>
			  <param name=""Y"" type=""float""/>
			  <param name=""Z"" type=""float""/>
			</accessor>
		  </technique_common>
		</source>";
		streamWriter.Write(fileContents);
	}

	private void AddUVs(string meshNumber, Part part, VertexList vertexList)
	{
		string fileContents = @$"
		<source id=""Object_{meshNumber}-mesh-map-0"">
		  <float_array id=""Object_001-mesh-map-0-array"" count=""{part.NumberVertices * 2}""> ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Vector2 uVSet = vertexList.Vertices[i].UVSet0;
			fileContents = $"{uVSet.X:0.000000} {uVSet.Y:0.000000}\t".Replace(',', '.');
			streamWriter.Write(fileContents);
		}

		fileContents = $@"</float_array>
		  <technique_common>
			<accessor source=""#Object_{meshNumber}-mesh-map-0-array"" count=""{part.NumberVertices}"" stride=""2"">
			  <param name=""S"" type=""float""/>
			  <param name=""T"" type=""float""/>
			</accessor>
		  </technique_common>
		</source>";
		streamWriter.Write(fileContents);
	}


	private void AddVertices(string meshNumber, Part part, VertexList vertexList)
	{
		string fileContents = @$"
	<geometry id=""Object_{meshNumber}-mesh"" name=""Object.{meshNumber}"">
	  <mesh>
		<source id=""Object_{meshNumber}-mesh-positions"">
		  <float_array id=""Object_{meshNumber}-mesh-positions-array"" count=""{part.NumberVertices * 3}""> ";
		streamWriter.Write(fileContents);

		for (int i = part.OffsetVertices; i < part.OffsetVertices + part.NumberVertices; i++)
		{
			Vector3 position = vertexList.Vertices[i].Position;
			fileContents = $"{position.X:0.000000} {position.Y:0.000000} {position.Z:0.000000} ".Replace(',', '.');
			streamWriter.Write(fileContents);
		}

		fileContents = @$"</float_array>
		  <technique_common>
			<accessor source=""#Object_{meshNumber}-mesh-positions-array"" count=""{part.NumberVertices}"" stride=""3"">
			  <param name=""X"" type=""float""/>
			  <param name=""Y"" type=""float""/>
			  <param name=""Z"" type=""float""/>
			</accessor>
		  </technique_common>
		</source>";
		streamWriter.Write(fileContents);
	}

}

